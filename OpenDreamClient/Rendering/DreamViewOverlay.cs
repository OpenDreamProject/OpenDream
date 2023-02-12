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
    private Dictionary<int, ShaderInstance> _blendmodeInstances;

    private readonly Dictionary<Vector2i, List<IRenderTexture>> _renderTargetCache = new();
    private EntityLookupSystem _lookupSystem;
    private ClientAppearanceSystem _appearanceSystem;
    private ClientScreenOverlaySystem _screenOverlaySystem;
    private SharedTransformSystem _transformSystem;
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowWorld;
    public bool ScreenOverlayEnabled = true;
    public bool MouseMapRenderEnabled = false;
    private IRenderTexture mouseMapRenderTarget;
    public Texture MouseMap;
    public Dictionary<Color, EntityUid> MouseMapLookup = new();
    private Dictionary<String, IRenderTexture> _renderSourceLookup = new();
    private List<IRenderTexture> _renderTargetsToReturn = new();


    public DreamViewOverlay() {
        IoCManager.InjectDependencies(this);
        Logger.Debug("Loading shaders...");
        var protoManager = IoCManager.Resolve<IPrototypeManager>();
        _blockColorInstance = protoManager.Index<ShaderPrototype>("blockcolor").InstanceUnique();

        _blendmodeInstances = new(4);
        //_blendmodeInstances.Add(0, protoManager.Index<ShaderPrototype>("empty").InstanceUnique()); //BLEND_DEFAULT, BLEND_OVERLAY - null shaders, because overlay is default behaviour
        _blendmodeInstances.Add(2, protoManager.Index<ShaderPrototype>("blend_add").InstanceUnique()); //BLEND_ADD
        _blendmodeInstances.Add(3, protoManager.Index<ShaderPrototype>("blend_subtract").InstanceUnique()); //BLEND_SUBTRACT
        _blendmodeInstances.Add(4, protoManager.Index<ShaderPrototype>("blend_multiply").InstanceUnique()); //BLEND_MULTIPLY
        _blendmodeInstances.Add(5, protoManager.Index<ShaderPrototype>("blend_inset_overlay").InstanceUnique()); //BLEND_INSET_OVERLAY //TODO
    }

    protected override void Draw(in OverlayDrawArgs args) {
        EntityUid? eye = _playerManager.LocalPlayer?.Session.AttachedEntity;
        if (eye == null) return;

        //because we render everything in render targets, and then render those to the world, we've got to apply some transformations to all world draws
        //in order to correct for different coordinate systems and general weirdness
        args.WorldHandle.SetTransform(new Vector2(0,args.WorldAABB.Size.Y), Angle.FromDegrees(180), new Vector2(-1,1));
        mouseMapRenderTarget = RentPingPongRenderTarget((Vector2i) args.WorldAABB.Size*EyeManager.PixelsPerMeter);
        ClearRenderTarget(mouseMapRenderTarget, args.WorldHandle, Color.Transparent);
        MouseMapLookup.Clear();
        DrawAll(args, eye.Value);
        MouseMap = mouseMapRenderTarget.Texture;
        ReturnPingPongRenderTarget(mouseMapRenderTarget);
        _appearanceSystem.CleanUpUnusedFilters();
        _appearanceSystem.ResetFilterUsageFlags();
        //some render targets need to be kept until the end of the render cycle, so return them here.
        foreach(IRenderTexture RT in _renderTargetsToReturn)
            ReturnPingPongRenderTarget(RT);
        _renderTargetsToReturn.Clear();
        _renderSourceLookup.Clear();
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
            if(entity == eye)
                continue; //don't render the player twice
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
                sprites.AddRange(ProcessIconComponents(_appearanceSystem.GetTurfIcon(tileRef.Tile.TypeId), pos.Position - 1, EntityUid.Invalid, false));
            }

        //screen objects
        if(ScreenOverlayEnabled){
            foreach (DMISpriteComponent sprite in _screenOverlaySystem.EnumerateScreenObjects()) {
                if (!sprite.IsVisible(checkWorld: false, mapManager: _mapManager))
                    continue;
                if (sprite.ScreenLocation.MapControl != null) // Don't render screen objects meant for other map controls
                    continue;
                Vector2 position = sprite.ScreenLocation.GetViewPosition(screenArea.BottomLeft, EyeManager.PixelsPerMeter);
                Vector2 iconSize = sprite.Icon.DMI == null ? Vector2.Zero : sprite.Icon.DMI.IconSize / (float)EyeManager.PixelsPerMeter;
                for (int x = 0; x < sprite.ScreenLocation.RepeatX; x++) {
                    for (int y = 0; y < sprite.ScreenLocation.RepeatY; y++) {
                        sprites.AddRange(ProcessIconComponents(sprite.Icon, position + iconSize * (x, y), sprite.Owner, true));
                    }
                }
            }
        }

        //early return if there's nothing to do
        if(sprites.Count == 0)
            return;

        sprites.Sort();

        //After sort, group by plane and render together
        float lastPlane = sprites[0].MainIcon.Appearance.Plane;
        IRenderTexture baseTarget = RentPingPongRenderTarget((Vector2i) args.WorldAABB.Size*EyeManager.PixelsPerMeter);
        IRenderTexture planeTarget = baseTarget;

        bool PlaneMasterActive = false;
        RendererMetaData PlaneMaster = null;

        ClearRenderTarget(planeTarget, args.WorldHandle, Color.Transparent);
        for(var i = 0; i < sprites.Count; i++){
            RendererMetaData sprite = sprites[i];
            //plane masters don't get rendered, but their properties get applied to the overall rendertarget
            if(((int)sprite.MainIcon.Appearance.AppearanceFlags & 128) == 128){ //appearance_flags & PLANE_MASTER
                PlaneMasterActive = true;
                PlaneMaster = sprite;
                planeTarget = RentPingPongRenderTarget((Vector2i) args.WorldAABB.Size*EyeManager.PixelsPerMeter);
                ClearRenderTarget(planeTarget, args.WorldHandle, Color.White.WithAlpha(0));
            } else {
                byte[] rgba = BitConverter.GetBytes(sprite.GetHashCode()); //.ClickUID
                Color targetColor = new Color(rgba[0],rgba[1],rgba[2],255);
                MouseMapLookup[targetColor] = sprite.ClickUID;
                _blockColorInstance.SetParameter("targetColor", targetColor); //Set shader instance's block colour for the mouse map
                //if we were drawing on a plane_master group, and plane changed, draw the plane master, then draw the sprite normally
                if(lastPlane != sprite.Plane && PlaneMasterActive){
                    DrawIcon(args.WorldHandle, baseTarget, PlaneMaster, -screenArea.BottomLeft, planeTarget.Texture);
                    PlaneMaster = null;
                    PlaneMasterActive = false;
                    ReturnPingPongRenderTarget(planeTarget);
                    planeTarget = baseTarget;
                }

                if(sprite.RenderTarget.Length > 0){
                    //if this sprite has a render target, draw it to a slate instead. If it needs to be drawn on the map, a second sprite instance will already have been created for that purpose
                    IRenderTexture tmpRenderTarget;
                    if(!_renderSourceLookup.TryGetValue(sprite.RenderTarget, out tmpRenderTarget)){

                        tmpRenderTarget = RentPingPongRenderTarget((Vector2i) args.WorldAABB.Size*EyeManager.PixelsPerMeter);
                        ClearRenderTarget(tmpRenderTarget, args.WorldHandle, Color.White.WithAlpha(0));
                        _renderSourceLookup.Add(sprite.RenderTarget, tmpRenderTarget);
                        _renderTargetsToReturn.Add(tmpRenderTarget);
                    }
                    DrawIcon(args.WorldHandle, tmpRenderTarget, sprite, (args.WorldAABB.Size/2)-sprite.Position); //draw the sprite centered on the RenderTarget
                }
                else {
                    //we draw the icon on the render plane, which is then drawn with the screen offset, so we correct for that in the draw positioning with offset
                    //if it's a render source though, we draw with a texture override with a center screen offset instead
                    if(sprite.RenderSource.Length > 0 && _renderSourceLookup.TryGetValue(sprite.RenderSource, out var renderSourceTexture)){
                        DrawIcon(args.WorldHandle, planeTarget, sprite, (-screenArea.BottomLeft)-(args.WorldAABB.Size/2), renderSourceTexture.Texture);
                    }
                    else{
                        DrawIcon(args.WorldHandle, planeTarget, sprite, -screenArea.BottomLeft);
                    }
                }
            }
            lastPlane = sprite.Plane;
        }
        //if a plane_master was active on the final draw, draw that first
         if(PlaneMasterActive){
            DrawIcon(args.WorldHandle, baseTarget, PlaneMaster, -screenArea.BottomLeft, planeTarget.Texture);
            ReturnPingPongRenderTarget(planeTarget);
         }

        //final draw
        if(MouseMapRenderEnabled)
            args.WorldHandle.DrawTexture(mouseMapRenderTarget.Texture, new Vector2(screenArea.Left, screenArea.Bottom*-1), null);
        else
            args.WorldHandle.DrawTexture(baseTarget.Texture, new Vector2(screenArea.Left, screenArea.Bottom*-1), null);
        ReturnPingPongRenderTarget(baseTarget);
    }

    //handles underlays, overlays, appearance flags, images. Returns a list of icons and metadata for them to be sorted, so they can be drawn with DrawIcon()
    private List<RendererMetaData> ProcessIconComponents(DreamIcon icon, Vector2 position, EntityUid uid, Boolean isScreen, RendererMetaData? parentIcon = null, bool keepTogether = false, int tieBreaker = 0)
    {
        List<RendererMetaData> result = new(icon.Underlays.Count + icon.Overlays.Count + 1);
        RendererMetaData current = new();
        current.MainIcon = icon;
        current.Position = position + (icon.Appearance.PixelOffset / (float)EyeManager.PixelsPerMeter);
        current.UID = uid;
        current.ClickUID = uid;
        current.IsScreen = isScreen;
        current.TieBreaker = tieBreaker;
        current.RenderSource = icon.Appearance.RenderSource;
        current.RenderTarget = icon.Appearance.RenderTarget;


        Matrix3 iconAppearanceTransformMatrix = new(new float[] { //reverse rotation transforms because of 180 flip from rendertarget->world transform
                icon.Appearance.Transform[0], -icon.Appearance.Transform[1], 0,
                -icon.Appearance.Transform[2], icon.Appearance.Transform[3], 0,
                icon.Appearance.Transform[4], icon.Appearance.Transform[5], 1
            });

        if(parentIcon != null){
            current.ClickUID = parentIcon.ClickUID;
            if((icon.Appearance.AppearanceFlags & 2) == 2 || keepTogether) //RESET_COLOR
                current.ColorToApply = icon.Appearance.Color;
            else
                current.ColorToApply = parentIcon.ColorToApply;

            if((icon.Appearance.AppearanceFlags & 4) == 4 || keepTogether) //RESET_ALPHA
                current.AlphaToApply = icon.Appearance.Alpha/255.0f;
            else
                current.AlphaToApply = parentIcon.AlphaToApply;

            if((icon.Appearance.AppearanceFlags & 8) == 8 || keepTogether) //RESET_TRANSFORM
                current.TransformToApply = iconAppearanceTransformMatrix;
            else
                current.TransformToApply = parentIcon.TransformToApply;

            if(((int)icon.Appearance.Plane & -32767) == -32767) //FLOAT_PLANE
                current.Plane = parentIcon.Plane + ((int)icon.Appearance.Plane & ~(-32767));
            else
                current.Plane = icon.Appearance.Plane;

            if(icon.Appearance.Layer == -1) //FLOAT_LAYER
                current.Layer = parentIcon.Layer;
            else
                current.Layer = icon.Appearance.Layer;
        } else {
            current.ColorToApply = icon.Appearance.Color;
            current.AlphaToApply = icon.Appearance.Alpha/255.0f;
            current.TransformToApply = iconAppearanceTransformMatrix;
            current.Plane = icon.Appearance.Plane;
            current.Layer = icon.Appearance.Layer;
        }

        keepTogether = keepTogether || ((icon.Appearance.AppearanceFlags & 32) == 32); //KEEP_TOGETHER

        if(current.RenderTarget.Length > 0 && current.RenderTarget[0]!='*'){ //if the rendertarget starts with *, we don't render it. If it doesn't we create a placeholder rendermetadata to position it correctly
            RendererMetaData renderTargetPlaceholder = new();
            //transform, color, alpha, filters - they should all already have been applied, so we leave them null in the placeholder
            renderTargetPlaceholder.MainIcon = current.MainIcon; //placeholder - TODO this might have unintended effects
            renderTargetPlaceholder.Position = current.Position;
            renderTargetPlaceholder.UID = current.UID;
            renderTargetPlaceholder.ClickUID = current.UID;
            renderTargetPlaceholder.IsScreen = current.IsScreen;
            renderTargetPlaceholder.TieBreaker = current.TieBreaker;
            renderTargetPlaceholder.Plane = current.Plane;
            renderTargetPlaceholder.Layer = current.Layer;
            renderTargetPlaceholder.RenderSource = current.RenderTarget;
            result.Add(renderTargetPlaceholder);
        } else if(current.RenderTarget.Length > 0) {
            current.RenderTarget = current.RenderTarget.Substring(1); //cut the * off, we're done with it
        }

        //TODO check for images with override here
        /*foreach(image in client.images){
            if(image.override && image.location == icon.owner)
                current.MainIcon = image
            else
                add like overlays?
        }*/

        //TODO vis_contents
        //click uid should be set to current.uid again
        //dont forget the vis_flags

        //underlays - colour, alpha, and transform are inherited, but filters aren't
        foreach (DreamIcon underlay in icon.Underlays) {
            if(!keepTogether || (underlay.Appearance.AppearanceFlags & 64) == 64) //KEEP_TOGETHER wasn't set on our parent, or KEEP_APART
                result.AddRange(ProcessIconComponents(underlay, current.Position, uid, isScreen, current, false, -1));
            else
                current.KeepTogetherGroup.AddRange(ProcessIconComponents(underlay, current.Position, uid, isScreen, current, keepTogether, -1));
        }

        //overlays - colour, alpha, and transform are inherited, but filters aren't
        foreach (DreamIcon overlay in icon.Overlays) {
            if(!keepTogether || (overlay.Appearance.AppearanceFlags & 64) == 64) //KEEP_TOGETHER wasn't set on our parent, or KEEP_APART
                result.AddRange(ProcessIconComponents(overlay, current.Position, uid, isScreen, current, false, 1));
            else
                current.KeepTogetherGroup.AddRange(ProcessIconComponents(overlay, current.Position, uid, isScreen, current, keepTogether, 1));
        }

        //TODO maptext - note colour + transform apply

        //TODO particles - colour and transform don't apply?

        //flatten keeptogethergroup. Done here so we get implicit recursive iteration down the tree.
        if(current.KeepTogetherGroup.Count > 0){
            List<RendererMetaData> flatKTGroup = new List<RendererMetaData>(current.KeepTogetherGroup.Count);
            foreach(RendererMetaData KTItem in current.KeepTogetherGroup){
                flatKTGroup.AddRange(KTItem.KeepTogetherGroup);
                flatKTGroup.Add(KTItem);
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

    private void ClearRenderTarget(IRenderTexture target, DrawingHandleWorld handle, Color clearColor)
    {
        handle.RenderInRenderTarget(target, () => {}, clearColor);
    }

    private void DrawIcon(DrawingHandleWorld handle, IRenderTarget renderTarget, RendererMetaData iconMetaData, Vector2 positionOffset, Texture textureOverride = null) {
        DreamIcon icon = iconMetaData.MainIcon;
        if(icon.Appearance == null && iconMetaData.RenderSource == "")
            return;

        Vector2 position = iconMetaData.Position + positionOffset;
        Vector2 pixelPosition = position*EyeManager.PixelsPerMeter;

        Texture frame;
        if(textureOverride != null) {
            frame = textureOverride;
            //TODO figure out why this is necessary and delete it from existence.
            IRenderTexture TempTexture = RentPingPongRenderTarget(frame.Size);
            ClearRenderTarget(TempTexture, handle, Color.White.WithAlpha(0));
            handle.RenderInRenderTarget(TempTexture , () => {
                    handle.DrawRect(new Box2(Vector2.Zero, TempTexture.Size), new Color());
                    handle.DrawTextureRect(frame, new Box2(Vector2.Zero, TempTexture.Size));
                }, Color.Transparent);
            frame = TempTexture.Texture;
            ReturnPingPongRenderTarget(TempTexture);
        }
        else
            frame = icon.CurrentFrame;



        if(iconMetaData.KeepTogetherGroup.Count > 0)
        {
            //store the parent's transform, color, blend, and alpha - then clear them for drawing to the render target
            Matrix3 KTParentTransform = iconMetaData.TransformToApply;
            iconMetaData.TransformToApply = Matrix3.Identity;
            Color KTParentColor = iconMetaData.ColorToApply;
            iconMetaData.ColorToApply = Color.White;
            float KTParentAlpha = iconMetaData.AlphaToApply;
            iconMetaData.AlphaToApply = 1f;
            float KTParentBlendMode = iconMetaData.MainIcon.Appearance.BlendMode;
            iconMetaData.MainIcon.Appearance.BlendMode = 0; //BLEND_DEFAULT

            List<RendererMetaData> KTItems = new List<RendererMetaData>(iconMetaData.KeepTogetherGroup.Count+1);
            KTItems.Add(iconMetaData);
            KTItems.AddRange(iconMetaData.KeepTogetherGroup);
            iconMetaData.KeepTogetherGroup.Clear();

            KTItems.Sort();
            //draw it onto an additional render target that we can return immediately for correction of transform
            IRenderTexture TempTexture = RentPingPongRenderTarget(frame.Size);
            ClearRenderTarget(TempTexture, handle, Color.Transparent);

            foreach(RendererMetaData KTItem in KTItems){
                DrawIcon(handle, TempTexture, KTItem, -KTItem.Position);
            }
            //but keep the handle to the final KT group's render target so we don't override it later in the render cycle
            IRenderTexture KTTexture = RentPingPongRenderTarget(TempTexture.Size);
            handle.RenderInRenderTarget(KTTexture , () => {
                    handle.DrawRect(new Box2(Vector2.Zero, TempTexture.Size), new Color());
                    handle.DrawTextureRect(TempTexture.Texture, new Box2(Vector2.Zero, TempTexture.Size));
                }, Color.Transparent);
            frame = KTTexture.Texture;
            ReturnPingPongRenderTarget(TempTexture);
            //now restore the original color, alpha, blend, and transform so they can be applied to the render target as a whole
            iconMetaData.TransformToApply = KTParentTransform;
            iconMetaData.ColorToApply = KTParentColor;
            iconMetaData.AlphaToApply = KTParentAlpha;
            iconMetaData.MainIcon.Appearance.BlendMode = KTParentBlendMode;

            _renderTargetsToReturn.Add(KTTexture);
        }

        if(frame != null && icon.Appearance.Filters.Count == 0) {
            //faster path for rendering unfiltered sprites
            handle.RenderInRenderTarget(renderTarget, () => {
                    handle.UseShader(_blendmodeInstances.TryGetValue((int) icon.Appearance.BlendMode, out var value) ? value : null);
                    handle.SetTransform(Matrix3.CreateTranslation(-(pixelPosition.X+frame.Size.X/2), -(pixelPosition.Y+frame.Size.Y/2)) * //translate, apply transformation, untranslate
                                        iconMetaData.TransformToApply *
                                        Matrix3.CreateTranslation((pixelPosition.X+frame.Size.X/2), (pixelPosition.Y+frame.Size.Y/2)));
                    handle.DrawTextureRect(frame,
                        new Box2(pixelPosition, pixelPosition+frame.Size),
                        iconMetaData.ColorToApply.WithAlpha(iconMetaData.AlphaToApply));
                    handle.UseShader(null);
                }, null);
            if(icon.Appearance.MouseOpacity != MouseOpacity.Transparent)
                handle.RenderInRenderTarget(mouseMapRenderTarget, () => {
                        handle.UseShader(_blockColorInstance);
                        handle.SetTransform(Matrix3.CreateTranslation(-(pixelPosition.X+frame.Size.X/2), -(pixelPosition.Y+frame.Size.Y/2)) * //translate, apply transformation, untranslate
                                        iconMetaData.TransformToApply *
                                        Matrix3.CreateTranslation((pixelPosition.X+frame.Size.X/2), (pixelPosition.Y+frame.Size.Y/2)));
                        handle.DrawTextureRect(frame,
                            new Box2(pixelPosition, pixelPosition+frame.Size),
                            Color.White);
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
                }, Color.Black.WithAlpha(0));

            foreach (DreamFilter filterId in icon.Appearance.Filters) {
                ShaderInstance s = _appearanceSystem.GetFilterShader(filterId, _renderSourceLookup);

                handle.RenderInRenderTarget(ping, () => {
                    handle.DrawRect(new Box2(Vector2.Zero, frame.Size * 2), new Color());
                    handle.UseShader(s);
                    handle.DrawTextureRect(pong.Texture, new Box2(Vector2.Zero, frame.Size * 2));
                    handle.UseShader(null);
                }, Color.Black.WithAlpha(0));

                tmpHolder = ping;
                ping = pong;
                pong = tmpHolder;
            }

            handle.RenderInRenderTarget(renderTarget, () => {
                    handle.UseShader(_blendmodeInstances.TryGetValue((int) icon.Appearance.BlendMode, out var value) ? value : null);
                    handle.SetTransform(Matrix3.CreateTranslation(-(pixelPosition.X+frame.Size.X/2), -(pixelPosition.Y+frame.Size.Y/2)) * //translate, apply transformation, untranslate
                                        iconMetaData.TransformToApply *
                                        Matrix3.CreateTranslation((pixelPosition.X+frame.Size.X/2), (pixelPosition.Y+frame.Size.Y/2)));
                    handle.DrawTextureRect(pong.Texture,
                        new Box2(pixelPosition-(frame.Size/2), pixelPosition+frame.Size+(frame.Size/2)),
                        null);
                    handle.UseShader(null);
                }, null);
            if(icon.Appearance.MouseOpacity != MouseOpacity.Transparent)
                handle.RenderInRenderTarget(mouseMapRenderTarget, () => {
                        handle.UseShader(_blockColorInstance);
                        handle.SetTransform(Matrix3.CreateTranslation(-(pixelPosition.X+frame.Size.X/2), -(pixelPosition.Y+frame.Size.Y/2)) * //translate, apply transformation, untranslate
                                        iconMetaData.TransformToApply *
                                        Matrix3.CreateTranslation((pixelPosition.X+frame.Size.X/2), (pixelPosition.Y+frame.Size.Y/2)));
                        handle.DrawTextureRect(pong.Texture,
                            new Box2(pixelPosition-(frame.Size/2), pixelPosition+frame.Size+(frame.Size/2)),
                            Color.White);
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

public sealed class ToggleMouseOverlayCommand : IConsoleCommand {
    public string Command => "togglemouseoverlay";
    public string Description => "Toggle rendering of mouse click area for screen objects";
    public string Help => "";

    public void Execute(IConsoleShell shell, string argStr, string[] args) {
        if (args.Length != 0) {
            shell.WriteError("This command does not take any arguments!");
            return;
        }

        IOverlayManager overlayManager = IoCManager.Resolve<IOverlayManager>();
        if (overlayManager.TryGetOverlay(typeof(DreamViewOverlay), out var overlay) &&
            overlay is DreamViewOverlay screenOverlay) {
            screenOverlay.MouseMapRenderEnabled = !screenOverlay.MouseMapRenderEnabled;
        }
    }
}

internal sealed class RendererMetaData : IComparable<RendererMetaData> {
    public DreamIcon MainIcon;
    public Vector2 Position = Vector2.Zero;
    public float Plane = 0; //true plane value may be different from appearance plane value, due to special flags
    public float Layer = 0; //ditto for layer
    public EntityUid UID = EntityUid.Invalid;
    public EntityUid ClickUID = EntityUid.Invalid; //the UID of the object clicks on this should be passed to (ie, for overlays)
    public Boolean IsScreen = false;
    public int TieBreaker = 0;
    public Color ColorToApply = Color.White;
    public float AlphaToApply = 1.0f;
    public Matrix3 TransformToApply = Matrix3.Identity;
    public String RenderSource = "";
    public String RenderTarget = "";
    public List<RendererMetaData> KeepTogetherGroup = new();

    public int CompareTo(RendererMetaData other) {
        int val = 0;

        val = (this.RenderTarget.Length > 0).CompareTo(other.RenderTarget.Length > 0);
        if (val != 0) {
            return -val;
        }
        //Plane
        val =  this.Plane.CompareTo(other.Plane);
        if (val != 0) {
            return val;
        }
        val = (((int)this.MainIcon.Appearance.AppearanceFlags & 128) == 128).CompareTo(((int)other.MainIcon.Appearance.AppearanceFlags & 128) == 128); //appearance_flags & PLANE_MASTER
        //PLANE_MASTER objects go first for any given plane
        if (val != 0) {
            return -val; //sign flip because we want 1 < -1
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
            int effects_layer = (((int)this.Layer & 5000) == 5000).CompareTo(((int)other.Layer & 5000) == 5000);
            if(effects_layer != 0)
                return effects_layer;
            int background_layer = (((int)this.Layer & 20000) == 20000).CompareTo(((int)other.Layer & 20000) == 20000);
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

