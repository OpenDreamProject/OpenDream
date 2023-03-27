using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using OpenDreamShared.Dream;
using Robust.Shared.Player;
using System.Net.NetworkInformation;

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

        sprites.Sort(_renderOrderComparer);
        foreach (DMISpriteComponent sprite in sprites) {
            if (!xformQuery.TryGetComponent(sprite.Owner, out var spriteTransform))
                continue;

            DrawIcon(args.WorldHandle, sprite.Icon,
                _transformSystem.GetWorldPosition(spriteTransform.Owner, xformQuery) - 0.5f);
        }
    }

    private void DrawTiles(OverlayDrawArgs args, TransformComponent eyeTransform) {
        if (!_mapManager.TryFindGridAt(eyeTransform.MapPosition, out var grid))
            return;

        foreach (TileRef tileRef in grid.GetTilesIntersecting(Box2.CenteredAround(eyeTransform.WorldPosition, (17, 17)))) {
            MapCoordinates pos = grid.GridTileToWorld(tileRef.GridIndices);
            DreamIcon icon = _appearanceSystem.GetTurfIcon(tileRef.Tile.TypeId);

            DrawIcon(args.WorldHandle, icon, pos.Position - 1);
        }
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
    private void DrawIcon(DrawingHandleWorld handle, DreamIcon icon, Vector2 position) {
        if (icon.Appearance == null)
            return;

        position += icon.Appearance.PixelOffset / (float)EyeManager.PixelsPerMeter;

        foreach (DreamIcon underlay in icon.Underlays) {
            DrawIcon(handle, underlay, position);
        }

        AtlasTexture frame = icon.CurrentFrame;
        if(frame is not null && icon.Appearance.Filters.Count == 0 && icon.Appearance.SillyColorFilter is null) {
            //faster path for rendering unshaded sprites
            handle.DrawTexture(frame, position, icon.Appearance.Color);
        } else if (frame is not null) {
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
            foreach (DreamFilter filterId in icon.Appearance.GetAllFilters()) {
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

            //FIXME: this is so dumb, make it stop rotating
            if (rotate) {
                handle.RenderInRenderTarget(ping, () => {
                    handle.DrawRect(new Box2(Vector2.Zero, frame.Size * 2), new Color());
                    handle.DrawTextureRect(pong.Texture, new Box2(Vector2.Zero, frame.Size * 2));
                }, Color.Transparent);

                tmpHolder = ping;
                ping = pong;
                pong = tmpHolder;
            }

            handle.DrawTexture(pong.Texture, position - ((frame.Size / 2) / (float) EyeManager.PixelsPerMeter),
                icon.Appearance.Color);
            ReturnPingPongRenderTarget(ping);
            ReturnPingPongRenderTarget(pong);
        }

        foreach (DreamIcon overlay in icon.Overlays) {
            DrawIcon(handle, overlay, position);
        }
    }
}

