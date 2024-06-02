using OpenDreamShared.Rendering;
using Robust.Client.Graphics;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace OpenDreamClient.Rendering;

public sealed class DMISpriteSystem : EntitySystem {
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly ClientAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly IClyde _clyde = default!;

    public override void Initialize() {
        SubscribeLocalEvent<DMISpriteComponent, ComponentAdd>(HandleComponentAdd);
        SubscribeLocalEvent<DMISpriteComponent, ComponentHandleState>(HandleComponentState);
    }

    private void OnIconSizeChanged(EntityUid uid) {
        _entityManager.TryGetComponent<TransformComponent>(uid, out var transform);
        _lookupSystem.FindAndAddToEntityTree(uid, xform: transform);
    }

    private void HandleComponentAdd(EntityUid uid, DMISpriteComponent component, ref ComponentAdd args) {
        component.Icon = new DreamIcon(_gameTiming, _clyde, _appearanceSystem);
        component.Icon.SizeChanged += () => OnIconSizeChanged(uid);
    }

    private static void HandleComponentState(EntityUid uid, DMISpriteComponent component, ref ComponentHandleState args) {
        SharedDMISpriteComponent.DMISpriteComponentState? state = (SharedDMISpriteComponent.DMISpriteComponentState?)args.Current;
        if (state == null)
            return;

        component.ScreenLocation = state.ScreenLocation;
        component.Icon.SetAppearance(state.AppearanceId);
    }
}
