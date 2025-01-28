using OpenDreamShared.Rendering;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace OpenDreamClient.Rendering;

public sealed class DMISpriteSystem : EntitySystem {
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly ClientAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly ClientScreenOverlaySystem _screenOverlaySystem = default!;
    [Dependency] private readonly ClientImagesSystem _clientImagesSystem = default!;

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
        _mapOverlay = new DreamViewOverlay(RenderTargetPool, _transformSystem, _mapSystem, _lookupSystem, _appearanceSystem, _screenOverlaySystem, _clientImagesSystem);
        _overlayManager.AddOverlay(_mapOverlay);
    }

    public override void Shutdown() {
        RenderTargetPool = default!;
        _overlayManager.RemoveOverlay<DreamViewOverlay>();
        _mapOverlay = default!;
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

        if (sprite.Icon.Appearance?.Opacity is true)
            _mapOverlay.DirtyTileVisibility(); // A movable with opacity=TRUE has moved
    }

    private void HandleTileChanged(ref TileChangedEvent ev) {
        _mapOverlay.DirtyTileVisibility();
    }

    private static void HandleComponentRemove(EntityUid uid, DMISpriteComponent component, ref ComponentRemove args) {
        component.Icon.Dispose();
    }
}
