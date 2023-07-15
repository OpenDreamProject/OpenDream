using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace OpenDreamShared.Rendering;

public sealed class DreamMobSightSystem : EntitySystem {
    public override void Initialize() {
        SubscribeLocalEvent<DreamMobSightComponent, ComponentGetState>(GetComponentState);
        SubscribeLocalEvent<DreamMobSightComponent, ComponentHandleState>(HandleComponentState);
    }

    private static void GetComponentState(EntityUid uid, DreamMobSightComponent component, ref ComponentGetState args) {
        args.State = new DreamMobSightComponentState(
            component.SeeInvisibility, component.Sight);
    }

    private static void HandleComponentState(EntityUid uid, DreamMobSightComponent component, ref ComponentHandleState args) {
        DreamMobSightComponentState? state = (DreamMobSightComponentState?)args.Current;
        if (state == null)
            return;

        component.SeeInvisibility = state.SeeInvisibility;
        component.Sight = state.Sight;
    }
}
