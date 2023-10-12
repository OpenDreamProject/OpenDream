using OpenDreamShared.Rendering;
using Robust.Shared.GameStates;

namespace OpenDreamRuntime.Rendering;

public sealed class DMISpriteSystem : EntitySystem {
    [Dependency] private readonly ServerAppearanceSystem _appearance = default!;

    public override void Initialize() {
        SubscribeLocalEvent<DMISpriteComponent, ComponentGetState>(GetComponentState);
    }

    private void GetComponentState(EntityUid uid, DMISpriteComponent component, ref ComponentGetState args) {
        int? appearanceId = null;
        if (component.Appearance != null) {
            appearanceId = _appearance.AddAppearance(component.Appearance);
        }

        args.State = new SharedDMISpriteComponent.DMISpriteComponentState(appearanceId, component.ScreenLocation);
    }
}
