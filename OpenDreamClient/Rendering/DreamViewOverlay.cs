using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using OpenDreamShared.Dream;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace OpenDreamClient.Rendering;

/// <summary>
/// Overlay for rendering world atoms
/// </summary>
sealed class DreamViewOverlay : Overlay {
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    private ShaderInstance _blockColorInstance;

    private readonly Dictionary<Vector2i, List<IRenderTexture>> _renderTargetCache = new();
    private readonly RenderOrderComparer _renderOrderComparer = new();
    private EntityLookupSystem _lookupSystem;
    private ClientAppearanceSystem _appearanceSystem;
    private ClientScreenOverlaySystem _screenOverlaySystem;
    private SharedTransformSystem _transformSystem;
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowWorld;
    public bool ScreenOverlayEnabled = true;
    private IRenderTexture mouseMapRenderTarget;
    public Texture MouseMap;
    public Dictionary<Color, EntityUid> MouseMapLookup = new();


    public DreamViewOverlay() {
        IoCManager.InjectDependencies(this);
        var protoManager = IoCManager.Resolve<IPrototypeManager>();
        _blockColorInstance = protoManager.Index<ShaderPrototype>("blockcolor").InstanceUnique();
    }

    protected override void Draw(in OverlayDrawArgs args) {
        EntityUid? eye = _playerManager.LocalPlayer?.Session.AttachedEntity;
        if (eye == null) return;

        //because we render everything in render targets, and then render those to the world, we've got to apply some transformations to all world draws
        //in order to correct for different coordinate systems and general weirdness
        args.WorldHandle.SetTransform(new Vector2(0,args.WorldAABB.Size.Y), Angle.FromDegrees(180), new Vector2(-1,1));
        mouseMapRenderTarget = RentPingPongRenderTarget((Vector2i) args.WorldAABB.Size*EyeManager.PixelsPerMeter);
        ClearRenderTarget(mouseMapRenderTarget, args.WorldHandle);
        MouseMapLookup.Clear();
        DrawAll(args, eye.Value);
        MouseMap = mouseMapRenderTarget.Texture;
        ReturnPingPongRenderTarget(mouseMapRenderTarget);
        _appearanceSystem.CleanUpUnusedFilters();
        _appearanceSystem.ResetFilterUsageFlags();
    }

    private void DrawAll(OverlayDrawArgs args, EntityUid eye) {
        _transformSystem ??= _entitySystem.GetEntitySystem<SharedTransformSystem>();
        _lookupSystem ??= _entitySystem.GetEntitySystem<EntityLookupSystem>();
        _appearanceSystem ??= _entitySystem.GetEntitySystem<ClientAppearanceSystem>();
        _screenOverlaySystem ??= _entitySystem.GetEntitySystem<ClientScreenOverlaySystem>();
        var spriteQuery = _entityManager.GetEntityQuery<DMISpriteComponent>();
        var xformQuery = _entityManager.GetEntityQuery<TransformComponent>();

        if (!xformQuery.TryGetComponent(eye, out var eyeTransform))
            return;
        Box2 screenArea = Box2.CenteredAround(eyeTransform.WorldPosition, args.WorldAABB.Size);

        var entities = _lookupSystem.GetEntitiesIntersecting(args.MapId, screenArea.Scale(1.2f)); //the scaling is to attempt to prevent pop-in, by rendering sprites that are *just* offscreen
        List<RendererMetaData> sprites = new(entities.Count + 1);

        //self icon
        if (spriteQuery.TryGetComponent(eye, out var player) && player.IsVisible(mapManager: _mapManager) && xformQuery.TryGetComponent(player.Owner, out var playerTransform))
            sprites.AddRange(ProcessIconComponents(player.Icon, _transformSystem.GetWorldPosition(playerTransform.Owner, xformQuery) - 0.5f, player.Owner, false));

        //visible entities
        foreach (EntityUid entity in entities) {
            if (!spriteQuery.TryGetComponent(entity, out var sprite))
                continue;
            if (!sprite.IsVisible(mapManager: _mapManager))
                continue;
            if(!xformQuery.TryGetComponent(sprite.Owner, out var spriteTransform))
                continue;
            sprites.AddRange(ProcessIconComponents(sprite.Icon, _transformSystem.GetWorldPosition(spriteTransform.Owner, xformQuery) - 0.5f, sprite.Owner, false));
        }

        //visible turfs
        if (_mapManager.TryFindGridAt(eyeTransform.MapPosition, out var grid))
            foreach (TileRef tileRef in grid.GetTilesIntersecting(screenArea.Scale(1.2f))) {
                MapCoordinates pos = grid.GridTileToWorld(tileRef.GridIndices);
                sprites.AddRange(ProcessIconComponents(_appearanceSystem.GetTurfIcon(tileRef.Tile.TypeId), pos.Position - 1, tileRef.GridUid, false));
            }

        //screen objects
        foreach (DMISpriteComponent sprite in _screenOverlaySystem.EnumerateScreenObjects()) {
            if (!sprite.IsVisible(checkWorld: false, mapManager: _mapManager))
                continue;
            if (sprite.ScreenLocation.MapControl != null) // Don't render screen objects meant for other map controls
                continue;
            Vector2 position = sprite.ScreenLocation.GetViewPosition(screenArea.BottomLeft, EyeManager.PixelsPerMeter);
            Vector2 iconSize = sprite.Icon.DMI.IconSize / (float)EyeManager.PixelsPerMeter;
            for (int x = 0; x < sprite.ScreenLocation.RepeatX; x++) {
                for (int y = 0; y < sprite.ScreenLocation.RepeatY; y++) {
                    sprites.AddRange(ProcessIconComponents(sprite.Icon, position + iconSize * (x, y), sprite.Owner, true));
                }
            }
        }

        //early return if there's nothing to do
        if(sprites.Count == 0)
            return;

        sprites.Sort(_renderOrderComparer);
        //After sort, group by plane and render together
        float lastPlane = sprites[0].MainIcon.Appearance.Plane;
        IRenderTexture planeTarget = RentPingPongRenderTarget((Vector2i) args.WorldAABB.Size*EyeManager.PixelsPerMeter);
        Color? PlaneMasterColor = null;
        float[]? PlaneMasterTransform = null;
        float? PlaneMasterBlendmode = null;

        ClearRenderTarget(planeTarget, args.WorldHandle);
        foreach (RendererMetaData sprite in sprites) {
            if(lastPlane != sprite.MainIcon.Appearance.Plane){
                args.WorldHandle.DrawTexture(planeTarget.Texture, new Vector2(screenArea.Left, screenArea.Bottom*-1), PlaneMasterColor);
                lastPlane = sprite.MainIcon.Appearance.Plane;
                //refresh planemaster values
                PlaneMasterColor = null;
                PlaneMasterTransform = null;
                PlaneMasterBlendmode = null;
                ClearRenderTarget(planeTarget, args.WorldHandle);
            }
            //plane masters don't get rendered, but their properties get applied to the overall rendertarget
            if(((int)sprite.MainIcon.Appearance.AppearanceFlags & 128) != 0){ //appearance_flags & PLANE_MASTER
                PlaneMasterColor = sprite.ColorToApply;
                PlaneMasterTransform = sprite.TransformToApply;
                PlaneMasterBlendmode = 0; //TODO
            } else {
                byte[] rgba = BitConverter.GetBytes(sprite.GetHashCode());
                Color targetColor = new Color(rgba[0], rgba[1], rgba[2], 255);
                MouseMapLookup.Add(targetColor, sprite.UID);
                _blockColorInstance.SetParameter("targetColor", targetColor); //Set shader instance's block colour for the mouse map
                //we draw the icon on the render plane, which is then drawn with the screen offset, so we correct for that in the draw positioning with offset
                DrawIcon(args.WorldHandle, planeTarget, sprite, -screenArea.BottomLeft);
            }
        }
        //final draw
        args.WorldHandle.DrawTexture(planeTarget.Texture, new Vector2(screenArea.Left, screenArea.Bottom*-1), PlaneMasterColor);
        ReturnPingPongRenderTarget(planeTarget);
    }

    //handles underlays, overlays, appearance flags, images. Returns a list of icons and metadata for them to be sorted, so they can be drawn with DrawIcon()
    private List<RendererMetaData> ProcessIconComponents(DreamIcon icon, Vector2 position, EntityUid uid, Boolean isScreen, RendererMetaData? parentIcon = null, bool keepTogether = false, int tieBreaker = 0)
    {
        List<RendererMetaData> result = new(icon.Underlays.Count + icon.Overlays.Count + 1);
        RendererMetaData current = new();
        current.MainIcon = icon;
        current.Position = position + (icon.Appearance.PixelOffset / (float)EyeManager.PixelsPerMeter);
        current.UID = uid;
        current.IsScreen = isScreen;
        current.TieBreaker = tieBreaker;

//TODO render source and target (jesus christ)

        if(parentIcon != null){
            if((icon.Appearance.AppearanceFlags & 1) != 0) //RESET_COLOR
                current.ColorToApply = icon.Appearance.Color;
            else
                current.ColorToApply = parentIcon.ColorToApply;

            if((icon.Appearance.AppearanceFlags & 2) != 0) //RESET_ALPHA
                current.AlphaToApply = icon.Appearance.Alpha/255.0f;
            else
                current.AlphaToApply = parentIcon.AlphaToApply;

            if((icon.Appearance.AppearanceFlags & 4) != 0) //RESET_TRANSFORM
                current.TransformToApply = icon.Appearance.Transform;
            else
                current.TransformToApply = parentIcon.TransformToApply;

            if(((int)icon.Appearance.Plane & -32767) != 0) //FLOAT_PLANE
                current.Plane = parentIcon.Plane + ((int)icon.Appearance.Plane ^ -32767);
            else
                current.Plane = icon.Appearance.Plane;

            if(icon.Appearance.Layer == -1) //FLOAT_LAYER
                current.Layer = parentIcon.Layer;
            else
                current.Layer = icon.Appearance.Plane;
        } else {
            current.ColorToApply = icon.Appearance.Color;
            current.AlphaToApply = icon.Appearance.Alpha/255.0f;
            current.TransformToApply = icon.Appearance.Transform;
            current.Plane = icon.Appearance.Plane;
            current.Layer = icon.Appearance.Layer;
        }

        keepTogether = keepTogether || ((icon.Appearance.AppearanceFlags & 5) != 0); //KEEP_TOGETHER

        //TODO check for images with override here
        /*foreach(image in icon.images){
            if(image.override)
                current.MainIcon = image
            else
                add like overlays?
        }*/

        //TODO vis_contents

        //underlays - colour, alpha, and transform are inherited, but filters aren't
        foreach (DreamIcon underlay in icon.Underlays) {
            if(!keepTogether || (icon.Appearance.AppearanceFlags & 5) != 0) //KEEP_APART
                result.AddRange(ProcessIconComponents(underlay, current.Position, uid, isScreen, current, false, -1));
            else
                parentIcon.KeepTogetherGroup.AddRange(ProcessIconComponents(underlay, current.Position, uid, isScreen, current, keepTogether, -1));
        }

        //overlays - colour, alpha, and transform are inherited, but filters aren't
        foreach (DreamIcon overlay in icon.Overlays) {
            if(!keepTogether || (icon.Appearance.AppearanceFlags & 5) != 0) //KEEP_APART
                result.AddRange(ProcessIconComponents(overlay, current.Position, uid, isScreen, current, false, 1));
            else
                parentIcon.KeepTogetherGroup.AddRange(ProcessIconComponents(overlay, current.Position, uid, isScreen, current, keepTogether, 1));
        }

        //TODO maptext - note colour + transform apply

        //TODO particles - colour and transform don't apply?

        //flatten keeptogethergroup. Done here so we get implicit recursive iteration down the tree.
        if(current.KeepTogetherGroup.Count > 0){
            List<RendererMetaData> flatKTGroup = current.KeepTogetherGroup;
            foreach(RendererMetaData KTItem in current.KeepTogetherGroup){
                flatKTGroup.AddRange(KTItem.KeepTogetherGroup);
                KTItem.KeepTogetherGroup.Clear();
            }
            current.KeepTogetherGroup = flatKTGroup;
        }
        result.Add(current);
        return result;
    }

    private IRenderTexture RentPingPongRenderTarget(Vector2i size) {
        IRenderTexture result;

        if (!_renderTargetCache.TryGetValue(size, out var listResult)) {
            result = _clyde.CreateRenderTarget(size, new(RenderTargetColorFormat.Rgba8Srgb));
        } else {
            if (listResult.Count > 0) {
                result = listResult[0]; //pop a value
                listResult.Remove(result);
            } else {
                result = _clyde.CreateRenderTarget(size, new(RenderTargetColorFormat.Rgba8Srgb));
            }

            _renderTargetCache[size] = listResult; //put the shorter list back
        }
        return result;
    }

    private void ReturnPingPongRenderTarget(IRenderTexture rental) {
        if (!_renderTargetCache.TryGetValue(rental.Size, out var storeList))
            storeList = new List<IRenderTexture>(4);

        storeList.Add(rental);
        _renderTargetCache[rental.Size] = storeList;
    }

    private void ClearRenderTarget(IRenderTexture target, DrawingHandleWorld handle)
    {
        handle.RenderInRenderTarget(target, () => {}, Color.Transparent);
    }

    private void DrawIcon(DrawingHandleWorld handle, IRenderTarget renderTarget, RendererMetaData iconMetaData, Vector2 positionOffset) {
        DreamIcon icon = iconMetaData.MainIcon;
        Vector2 position = iconMetaData.Position + positionOffset;
        if (icon.Appearance == null)
            return;

        Vector2 pixelPosition = position*EyeManager.PixelsPerMeter;

        //main icon - TODO transform

        Texture frame = icon.CurrentFrame;
        if(iconMetaData.KeepTogetherGroup.Count > 0)
        {
            iconMetaData.KeepTogetherGroup.Add(iconMetaData);
            iconMetaData.KeepTogetherGroup.Sort(_renderOrderComparer);
            IRenderTexture KTTexture = RentPingPongRenderTarget(frame.Size * 2);
            foreach(RendererMetaData KTItem in iconMetaData.KeepTogetherGroup){
                DrawIcon(handle, KTTexture, KTItem, (frame.Size / 2)/EyeManager.PixelsPerMeter);
            }
            frame = KTTexture.Texture;
            ReturnPingPongRenderTarget(KTTexture);
        }
        if(frame != null && icon.Appearance.Filters.Count == 0) {
            //faster path for rendering unfiltered sprites
            handle.RenderInRenderTarget(renderTarget, () => {
                    handle.DrawTextureRect(frame,
                        new Box2(pixelPosition, pixelPosition+frame.Size),
                        iconMetaData.ColorToApply.WithAlpha(iconMetaData.AlphaToApply));
                }, null);
            if(icon.Appearance.MouseOpacity != MouseOpacity.Transparent)
                handle.RenderInRenderTarget(mouseMapRenderTarget, () => {
                        handle.UseShader(_blockColorInstance);
                        handle.DrawTextureRect(frame,
                            new Box2(pixelPosition, pixelPosition+frame.Size),
                            iconMetaData.ColorToApply.WithAlpha(iconMetaData.AlphaToApply));
                        handle.UseShader(null);
                    }, null);

        } else if (frame != null) {
            IRenderTexture ping = RentPingPongRenderTarget(frame.Size * 2);
            IRenderTexture pong = RentPingPongRenderTarget(frame.Size * 2);
            IRenderTexture tmpHolder;

            handle.RenderInRenderTarget(pong,
                () => {
                    handle.DrawTextureRect(frame,
                        new Box2(Vector2.Zero + (frame.Size / 2), frame.Size + (frame.Size / 2)),
                        iconMetaData.ColorToApply.WithAlpha(iconMetaData.AlphaToApply));
                }, Color.Transparent);

            bool rotate = true;
            foreach (DreamFilter filterId in icon.Appearance.Filters) {
                ShaderInstance s = _appearanceSystem.GetFilterShader(filterId);

                handle.RenderInRenderTarget(ping, () => {
                    handle.DrawRect(new Box2(Vector2.Zero, frame.Size * 2), new Color());
                    handle.UseShader(s);
                    handle.DrawTextureRect(pong.Texture, new Box2(Vector2.Zero, frame.Size * 2));
                    handle.UseShader(null);
                }, Color.Transparent);

                tmpHolder = ping;
                ping = pong;
                pong = tmpHolder;
                rotate = !rotate;
            }

            handle.RenderInRenderTarget(renderTarget, () => {
                    handle.DrawTextureRect(pong.Texture,
                        new Box2(pixelPosition-(frame.Size/2), pixelPosition+frame.Size+(frame.Size/2)),
                        null);
                }, null);
            if(icon.Appearance.MouseOpacity != MouseOpacity.Transparent)
                handle.RenderInRenderTarget(mouseMapRenderTarget, () => {
                        handle.UseShader(_blockColorInstance);
                        handle.DrawTextureRect(pong.Texture,
                            new Box2(pixelPosition-(frame.Size/2), pixelPosition+frame.Size+(frame.Size/2)),
                            null);
                        handle.UseShader(null);
                    }, null);
            ReturnPingPongRenderTarget(ping);
            ReturnPingPongRenderTarget(pong);
        }
    }
}

public sealed class ToggleScreenOverlayCommand : IConsoleCommand {
    public string Command => "togglescreenoverlay";
    public string Description => "Toggle rendering of screen objects";
    public string Help => "";

    public void Execute(IConsoleShell shell, string argStr, string[] args) {
        if (args.Length != 0) {
            shell.WriteError("This command does not take any arguments!");
            return;
        }

        IOverlayManager overlayManager = IoCManager.Resolve<IOverlayManager>();
        if (overlayManager.TryGetOverlay(typeof(DreamViewOverlay), out var overlay) &&
            overlay is DreamViewOverlay screenOverlay) {
            screenOverlay.ScreenOverlayEnabled = !screenOverlay.ScreenOverlayEnabled;
        }
    }
}

internal sealed class RendererMetaData : IComparable<RendererMetaData> {
    public DreamIcon MainIcon;
    public Vector2 Position;
    public float Plane; //true plane value may be different from appearance plane value, due to special flags
    public float Layer; //ditto for layer
    public EntityUid UID;
    public Boolean IsScreen = false;
    public int TieBreaker = 0;
    public Color ColorToApply = Color.White;
    public float AlphaToApply = 1.0f;
    public float[] TransformToApply;
    public List<RendererMetaData> KeepTogetherGroup = new();

    public int CompareTo(RendererMetaData other) {
        int val = 0;
        //Plane
        val =  this.Plane.CompareTo(other.Plane);
        if (val != 0) {
            return val;
        }
        //subplane (ie, HUD vs not HUD)
        val = this.IsScreen.CompareTo(other.IsScreen);
        if (val != 0) {
            return val;
        }
        //depending on world.map_format, either layer or physical position
        //TODO
        val = this.Layer.CompareTo(other.Layer);
        if (val != 0) {
            //special handling for EFFECTS_LAYER and BACKGROUND_LAYER
            int effects_layer = ((int)this.Layer & 20000).CompareTo((int)other.Layer & 20000);
            if(effects_layer != 0)
                return effects_layer;
            int background_layer = ((int)this.Layer & 20000).CompareTo((int)other.Layer & 20000);
            if(background_layer != 0)
                return -background_layer; //flipped because background_layer flag forces it to the back
            return val;
        }

        //Finally, tie-breaker - in BYOND, this is order of creation of the sprites
        //for us, we use EntityUID, with a tie-breaker (for underlays/overlays)
        val = this.UID.CompareTo(other.UID);
        if (val != 0) {
            return val;
        }
        val = this.TieBreaker.CompareTo(other.TieBreaker);
        if (val != 0) {
            return val;
        }

        return 0;
    }
}

