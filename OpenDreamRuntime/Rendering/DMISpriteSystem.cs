using OpenDreamShared.Dream;
using OpenDreamShared.Rendering;
using Robust.Shared.GameStates;

namespace OpenDreamRuntime.Rendering;

public sealed class DMISpriteSystem : EntitySystem {
    [Dependency] private readonly ServerAppearanceSystem _appearance = default!;

    public override void Initialize() {
        SubscribeLocalEvent<DMISpriteComponent, ComponentGetState>(GetComponentState);
    }

    private void GetComponentState(EntityUid uid, DMISpriteComponent component, ref ComponentGetState args) {
        uint? appearanceId = (component.Appearance != null)
            ? _appearance.AddAppearance(component.Appearance).MustGetId()
            : null;

        args.State = new SharedDMISpriteComponent.DMISpriteComponentState(appearanceId, component.ScreenLocation);
    }

    public void SetSpriteAppearance(Entity<DMISpriteComponent> ent, MutableAppearance appearance, bool dirty = true) {
        DMISpriteComponent component = ent.Comp;
        component.Appearance = new ImmutableAppearance(appearance, _appearance);
        if(dirty)
            Dirty(ent, component);
    }

    public void SetSpriteScreenLocation(Entity<DMISpriteComponent> ent, ScreenLocation screenLocation) {
        DMISpriteComponent component = ent.Comp;
        component.ScreenLocation = screenLocation;
        Dirty(ent, component);
    }
}
