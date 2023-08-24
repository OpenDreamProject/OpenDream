using OpenDreamShared.Rendering;
using Robust.Shared.GameStates;

namespace OpenDreamClient.Rendering;

public sealed class DMISpriteSystem : EntitySystem {
    public override void Initialize() {
        SubscribeLocalEvent<DMISpriteComponent, ComponentHandleState>(HandleComponentState);
    }

    private static void HandleComponentState(EntityUid uid, DMISpriteComponent component, ref ComponentHandleState args) {
        SharedDMISpriteComponent.DMISpriteComponentState? state = (SharedDMISpriteComponent.DMISpriteComponentState?)args.Current;
        if (state == null)
            return;

        component.ScreenLocation = state.ScreenLocation;
        component.Icon.SetAppearance(state.AppearanceId);
    }
}
