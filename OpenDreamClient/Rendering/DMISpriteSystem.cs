using OpenDreamShared.Rendering;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace OpenDreamClient.Rendering;

internal sealed class DMISpriteSystem : EntitySystem {
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ClientAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    public RenderTargetPool RenderTargetPool = default!;

    private EntityQuery<DMISpriteComponent> _spriteQuery;
    private DreamViewOverlay _mapOverlay = default!;

    public override void Initialize() {
        SubscribeLocalEvent<DMISpriteComponent, ComponentAdd>(HandleComponentAdd);
        SubscribeLocalEvent<DMISpriteComponent, ComponentHandleState>(HandleComponentState);
        SubscribeLocalEvent<DMISpriteComponent, ComponentRemove>(HandleComponentRemove);
        SubscribeLocalEvent<TransformComponent, MoveEvent>(HandleTransformMove);
        SubscribeLocalEvent<TileChangedEvent>(HandleTileChanged);

        RenderTargetPool = new(_clyde);
        _spriteQuery = _entityManager.GetEntityQuery<DMISpriteComponent>();
        _mapOverlay = new DreamViewOverlay(RenderTargetPool);
        _overlayManager.AddOverlay(_mapOverlay);
    }

    public override void Shutdown() {
        RenderTargetPool = default!;
        _overlayManager.RemoveOverlay<DreamViewOverlay>();
        _mapOverlay = default!;
    }

    /// <summary>
    /// Checks if a sprite should be visible to the player<br/>
    /// Checks the appearance's invisibility, if it's inside the given AABB, and whether it's parented to another entity
    /// </summary>
    /// <param name="sprite">The sprite to check</param>
    /// <param name="transform">The entity's transform, the parent check is skipped if this is null</param>
    /// <param name="seeInvisibility">The eye's see_invisibility var</param>
    /// <param name="worldAABB">The box visible to the viewport</param>
    public bool IsVisible(DMISpriteComponent sprite, TransformComponent? transform, int? seeInvisibility, Box2? worldAABB) {
        var icon = sprite.Icon;
        if (icon.Appearance?.Invisibility > seeInvisibility)
            return false;

        if (transform != null) {
            if (worldAABB != null) {
                Box2? aabb = null;
                icon.GetWorldAABB(_transformSystem.GetWorldPosition(transform), ref aabb);
                if (aabb.HasValue && !worldAABB.Value.Intersects(aabb.Value))
                    return false;
            }

            //Only render movables not inside another movable's contents (parented to the grid)
            //TODO: Use RobustToolbox's container system/components?
            if (transform.ParentUid != transform.GridUid)
                return false;
        }

        return true;
    }

    private void OnIconSizeChanged(EntityUid uid) {
        if (!_entityManager.TryGetComponent<TransformComponent>(uid, out var transform))
            return;

        _lookupSystem.FindAndAddToEntityTree(uid, xform: transform);
    }

    private void HandleComponentAdd(EntityUid uid, DMISpriteComponent component, ref ComponentAdd args) {
        component.Icon = new DreamIcon(RenderTargetPool, _gameTiming, _clyde, _appearanceSystem);
        component.Icon.SizeChanged += () => OnIconSizeChanged(uid);
    }

    private void HandleComponentState(EntityUid uid, DMISpriteComponent component, ref ComponentHandleState args) {
        SharedDMISpriteComponent.DMISpriteComponentState? state = (SharedDMISpriteComponent.DMISpriteComponentState?)args.Current;
        if (state == null)
            return;

        _mapOverlay.DirtyTileVisibility(); // Our icon's opacity may have changed
        component.ScreenLocation = state.ScreenLocation;
        component.Icon.SetAppearance(state.AppearanceId);
    }

    private void HandleTransformMove(EntityUid uid, TransformComponent component, ref MoveEvent args) {
        if (!_spriteQuery.TryGetComponent(uid, out var sprite))
            return;

        if (sprite.Icon.Appearance?.Opacity is true || uid == _playerManager.LocalSession?.AttachedEntity)
            _mapOverlay.DirtyTileVisibility(); // A movable with opacity=TRUE, or our eye, has moved
    }

    private void HandleTileChanged(ref TileChangedEvent ev) {
        _mapOverlay.DirtyTileVisibility();
    }

    private static void HandleComponentRemove(EntityUid uid, DMISpriteComponent component, ref ComponentRemove args) {
        component.Icon.Dispose();
    }
}
