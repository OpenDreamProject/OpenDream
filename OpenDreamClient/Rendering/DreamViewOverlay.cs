using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using OpenDreamShared.Dream;

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
    private SharedTransformSystem _transformSystem;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowWorld;

    public DreamViewOverlay() {
        IoCManager.InjectDependencies(this);
    }

    protected override void Draw(in OverlayDrawArgs args) {
        EntityUid? eye = _playerManager.LocalPlayer?.Session.AttachedEntity;
        if (eye == null) return;

        DrawMap(args, eye.Value);
        _appearanceSystem.CleanUpUnusedFilters();
        _appearanceSystem.ResetFilterUsageFlags();
    }

    private void DrawMap(OverlayDrawArgs args, EntityUid eye) {
        _transformSystem ??= _entitySystem.GetEntitySystem<SharedTransformSystem>();
        _lookupSystem ??= _entitySystem.GetEntitySystem<EntityLookupSystem>();
        _appearanceSystem ??= _entitySystem.GetEntitySystem<ClientAppearanceSystem>();
        var spriteQuery = _entityManager.GetEntityQuery<DMISpriteComponent>();
        var xformQuery = _entityManager.GetEntityQuery<TransformComponent>();

        if (!xformQuery.TryGetComponent(eye, out var eyeTransform))
            return;

        DrawTiles(args, eyeTransform);

        var entities = _lookupSystem.GetEntitiesIntersecting(args.MapId, args.WorldAABB);
        List<DMISpriteComponent> sprites = new(entities.Count + 1);

        if (spriteQuery.TryGetComponent(eye, out var player) && player.IsVisible(mapManager: _mapManager))
            sprites.Add(player);

        foreach (EntityUid entity in entities) {
            if (!spriteQuery.TryGetComponent(entity, out var sprite))
                continue;
            if (!sprite.IsVisible(mapManager: _mapManager))
                continue;

            sprites.Add(sprite);
        }

        //early return if there's nothing to do
        if(sprites.Count == 0)
            return;

        sprites.Sort(_renderOrderComparer);
        //After sort, group by plane and render together
        float lastPlane = sprites[0].Icon.Appearance.Plane;
        IRenderTexture planeTarget = RentPingPongRenderTarget((Vector2i) args.WorldAABB.Size*EyeManager.PixelsPerMeter);
        foreach (DMISpriteComponent sprite in sprites) {
            if (!xformQuery.TryGetComponent(sprite.Owner, out var spriteTransform))
                continue;

            if(lastPlane != sprite.Icon.Appearance.Plane){
                args.WorldHandle.DrawTexture(planeTarget.Texture, Vector2i.Zero);
                args.WorldHandle.DrawRect(new Box2(Vector2.Zero, planeTarget.Size), new Color());
                lastPlane = sprite.Icon.Appearance.Plane;
            }
            DrawIcon(args.WorldHandle, planeTarget, sprite.Icon,
                _transformSystem.GetWorldPosition(spriteTransform.Owner, xformQuery) - 0.5f);
        }
        //final draw
        args.WorldHandle.DrawTexture(planeTarget.Texture, Vector2i.Zero);
        ReturnPingPongRenderTarget(planeTarget);
    }

    private void DrawTiles(OverlayDrawArgs args, TransformComponent eyeTransform) {
        if (!_mapManager.TryFindGridAt(eyeTransform.MapPosition, out var grid))
            return;
        IRenderTexture planeTarget = RentPingPongRenderTarget((Vector2i) args.WorldAABB.Size*EyeManager.PixelsPerMeter);
        //args.WorldHandle.SetTransform(eyeTransform.LocalMatrix);
        foreach (TileRef tileRef in grid.GetTilesIntersecting(Box2.CenteredAround(eyeTransform.WorldPosition, (17, 17)))) {
            MapCoordinates pos = grid.GridTileToWorld(tileRef.GridIndices);
            DreamIcon icon = _appearanceSystem.GetTurfIcon(tileRef.Tile.TypeId);

            DrawIcon(args.WorldHandle, planeTarget, icon, pos.Position - 1);
        }
        //args.WorldHandle.SetTransform(eyeTransform.WorldMatrix);
        args.WorldHandle.DrawTexture(planeTarget.Texture, eyeTransform.WorldPosition-planeTarget.Size, Color.Transparent);
        //args.WorldHandle.DrawTextureRect(planeTarget.Texture, new Box2Rotated(Box2.CenteredAround(eyeTransform.WorldPosition, (17, 17)), Angle.FromDegrees(180)), Color.Transparent);
        //args.WorldHandle.DrawTextureRect(planeTarget.Texture, Box2.CenteredAround(Vector2i.Zero, (17, 17)));
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

    // TODO: Move this to DreamIcon.Draw() so screen objects can have filters
    private void DrawIcon(DrawingHandleWorld handle, IRenderTarget renderTarget, DreamIcon icon, Vector2 position) {
        if (icon.Appearance == null)
            return;

        position += icon.Appearance.PixelOffset / (float)EyeManager.PixelsPerMeter;

        //TODO appearance_flags - notably KEEP_TOGETHER and KEEP_APART
        //keep together can probably just be a subcall to DrawIcon()?

        //TODO check for images with override here

        //TODO vis_contents

        //underlays - inherit colour and transform ?
        foreach (DreamIcon underlay in icon.Underlays) {
            DrawIcon(handle, renderTarget, underlay, position);
        }

        //main icon - TODO transform
        AtlasTexture frame = icon.CurrentFrame;
        if(frame != null && icon.Appearance.Filters.Count == 0) {
            //faster path for rendering unfiltered sprites
            handle.RenderInRenderTarget(renderTarget, () => {
                    handle.SetTransform(Vector2.Zero, Angle.Zero, Vector2.One);
                    handle.DrawTextureRect(frame,
                        new Box2(position*EyeManager.PixelsPerMeter, position*EyeManager.PixelsPerMeter+frame.Size),
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

            //this is so dumb
            if (rotate) {
                handle.RenderInRenderTarget(ping, () => {
                    handle.DrawRect(new Box2(Vector2.Zero, frame.Size * 2), new Color());
                    handle.DrawTextureRect(pong.Texture, new Box2(Vector2.Zero, frame.Size * 2));
                }, Color.Transparent);

                tmpHolder = ping;
                ping = pong;
                pong = tmpHolder;
            }

            handle.RenderInRenderTarget(renderTarget, () => {
                    handle.DrawTextureRect(pong.Texture,
                        new Box2((position*EyeManager.PixelsPerMeter) + (frame.Size / 2), frame.Size + (frame.Size / 2)),
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

