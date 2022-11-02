using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;

namespace OpenDreamClient.Rendering;

/// <summary>
/// Overlay for rendering world atoms
/// </summary>
sealed class DreamViewOverlay : Overlay {
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

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

            sprite.Icon.Draw(args.WorldHandle,
                _transformSystem.GetWorldPosition(spriteTransform.Owner, xformQuery) - 0.5f);
        }
    }

    private void DrawTiles(OverlayDrawArgs args, TransformComponent eyeTransform) {
        if (!_mapManager.TryFindGridAt(eyeTransform.MapPosition, out var grid))
            return;

        foreach (TileRef tileRef in grid.GetTilesIntersecting(Box2.CenteredAround(eyeTransform.WorldPosition, (17, 17)))) {
            MapCoordinates pos = grid.GridTileToWorld(tileRef.GridIndices);
            DreamIcon icon = _appearanceSystem.GetTurfIcon(tileRef.Tile.TypeId);

            icon.Draw(args.WorldHandle, pos.Position - 1);
        }
    }
}
