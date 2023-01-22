using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using OpenDreamShared.Dream;
using Robust.Shared.Console;

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

    private readonly Dictionary<Vector2i, List<IRenderTexture>> _renderTargetCache = new();
    private readonly RenderOrderComparer _renderOrderComparer = new();
    private EntityLookupSystem _lookupSystem;
    private ClientAppearanceSystem _appearanceSystem;
    private ClientScreenOverlaySystem _screenOverlaySystem;
    private SharedTransformSystem _transformSystem;
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowWorld;
    public bool ScreenOverlayEnabled = true;


    public DreamViewOverlay() {
        IoCManager.InjectDependencies(this);
    }

    protected override void Draw(in OverlayDrawArgs args) {
        EntityUid? eye = _playerManager.LocalPlayer?.Session.AttachedEntity;
        if (eye == null) return;

        //because we render everything in render targets, and then render those to the world, we've got to apply some transformations to all world draws
        //in order to correct for different coordinate systems and general weirdness
        args.WorldHandle.SetTransform(new Vector2(0,args.WorldAABB.Size.Y), Angle.FromDegrees(180), new Vector2(-1,1));
        DrawMap(args, eye.Value);
        _appearanceSystem.CleanUpUnusedFilters();
        _appearanceSystem.ResetFilterUsageFlags();
    }

    private void DrawMap(OverlayDrawArgs args, EntityUid eye) {
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
        List<(DreamIcon, Vector2, EntityUid, Boolean)> sprites = new(entities.Count + 1);

        //self icon
        if (spriteQuery.TryGetComponent(eye, out var player) && player.IsVisible(mapManager: _mapManager) && xformQuery.TryGetComponent(player.Owner, out var playerTransform))
            sprites.Add((player.Icon, _transformSystem.GetWorldPosition(playerTransform.Owner, xformQuery) - 0.5f, player.Owner, false));

        //visible entities
        foreach (EntityUid entity in entities) {
            if (!spriteQuery.TryGetComponent(entity, out var sprite))
                continue;
            if (!sprite.IsVisible(mapManager: _mapManager))
                continue;
            if(!xformQuery.TryGetComponent(sprite.Owner, out var spriteTransform))
                continue;
            sprites.Add((sprite.Icon, _transformSystem.GetWorldPosition(spriteTransform.Owner, xformQuery) - 0.5f, sprite.Owner, false));
        }

        //visible turfs
        if (_mapManager.TryFindGridAt(eyeTransform.MapPosition, out var grid))
            foreach (TileRef tileRef in grid.GetTilesIntersecting(screenArea.Scale(1.2f))) {
                MapCoordinates pos = grid.GridTileToWorld(tileRef.GridIndices);
                sprites.Add((_appearanceSystem.GetTurfIcon(tileRef.Tile.TypeId), pos.Position - 1, tileRef.GridUid, false));
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
                    sprites.Add((sprite.Icon, position + iconSize * (x, y), sprite.Owner, true));
                }
            }
        }

        //early return if there's nothing to do
        if(sprites.Count == 0)
            return;

        sprites.Sort(_renderOrderComparer);
        //After sort, group by plane and render together
        float lastPlane = sprites[0].Item1.Appearance.Plane;
        IRenderTexture planeTarget = RentPingPongRenderTarget((Vector2i) args.WorldAABB.Size*EyeManager.PixelsPerMeter);
        ClearRenderTarget(planeTarget, args.WorldHandle);
        foreach ((DreamIcon, Vector2, EntityUid, Boolean) sprite in sprites) {
            if(lastPlane != sprite.Item1.Appearance.Plane){
                args.WorldHandle.DrawTexture(planeTarget.Texture, new Vector2(screenArea.Left, screenArea.Bottom*-1));
                lastPlane = sprite.Item1.Appearance.Plane;
                ClearRenderTarget(planeTarget, args.WorldHandle);
            }
            //we draw the icon on the render plane, which is then drawn with the screen offset, so we correct for that in the draw positioning
            DrawIcon(args.WorldHandle, planeTarget, sprite.Item1, sprite.Item2 - screenArea.BottomLeft);
        }
        //final draw
        args.WorldHandle.DrawTexture(planeTarget.Texture, new Vector2(screenArea.Left, screenArea.Bottom*-1));
        ReturnPingPongRenderTarget(planeTarget);
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

    // TODO: Move this to DreamIcon.Draw() so screen objects can have filters
    private void DrawIcon(DrawingHandleWorld handle, IRenderTarget renderTarget, DreamIcon icon, Vector2 position) {
        if (icon.Appearance == null)
            return;

        position += icon.Appearance.PixelOffset / (float)EyeManager.PixelsPerMeter;
        Vector2 pixelPosition = position*EyeManager.PixelsPerMeter;
        //TODO appearance_flags - notably KEEP_TOGETHER and KEEP_APART
        //keep together can probably just be a subcall to DrawIcon()?

        //TODO check for images with override here

        //TODO vis_contents

        //underlays - inherit colour and transform ?
        //FUCK - this only makes sense if each underlay has FLOAT_PLANE as its layer TODO - move them to the sort I guess
        foreach (DreamIcon underlay in icon.Underlays) {
            DrawIcon(handle, renderTarget, underlay, position);
        }

        //main icon - TODO transform
        AtlasTexture frame = icon.CurrentFrame;
        if(frame != null && icon.Appearance.Filters.Count == 0) {
            //faster path for rendering unfiltered sprites
            handle.RenderInRenderTarget(renderTarget, () => {
                    handle.DrawTextureRect(frame,
                        new Box2(pixelPosition, pixelPosition+frame.Size),
                        icon.Appearance.Color);
                }, null);
        } else if (frame != null) {
            IRenderTexture ping = RentPingPongRenderTarget(frame.Size * 2);
            IRenderTexture pong = RentPingPongRenderTarget(frame.Size * 2);
            IRenderTexture tmpHolder;

            handle.RenderInRenderTarget(pong,
                () => {
                    handle.DrawTextureRect(frame,
                        new Box2(Vector2.Zero + (frame.Size / 2), frame.Size + (frame.Size / 2)),
                        icon.Appearance.Color);
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
                        icon.Appearance.Color);
                }, null);
            ReturnPingPongRenderTarget(ping);
            ReturnPingPongRenderTarget(pong);
        }

        //TODO maptext - note colour + transform apply

        //TODO particles - colour and transform don't apply?

        //overlays - colour and transform are inherited, but filters aren't
        foreach (DreamIcon overlay in icon.Overlays) {
            DrawIcon(handle, renderTarget, overlay, position);
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
