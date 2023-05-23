using OpenDreamShared.Dream;
using OpenDreamShared.Rendering;

namespace OpenDreamRuntime.Rendering {
    [RegisterComponent]
    public sealed class DMISpriteComponent : SharedDMISpriteComponent {
        private IEntitySystemManager? _entitySystemManager;
        private ServerAppearanceSystem? _appearanceSystem;

        [ViewVariables]
        public ScreenLocation? ScreenLocation {
            get => _screenLocation;
            set {
                _screenLocation = value;
                Dirty();
            }
        }
        private ScreenLocation? _screenLocation;

        [ViewVariables] public IconAppearance? Appearance { get; private set; }

        public override ComponentState GetComponentState() {
            uint? appearanceId = null;
            if (Appearance != null) {
                IoCManager.Resolve(ref _entitySystemManager);
                _appearanceSystem ??= _entitySystemManager.GetEntitySystem<ServerAppearanceSystem>();

                appearanceId = _appearanceSystem.AddAppearance(Appearance);
            }

            return new DMISpriteComponentState(appearanceId, ScreenLocation);
        }

        public void SetAppearance(IconAppearance? appearance, bool dirty = true) {
            Appearance = appearance;

            if (dirty) {
                Dirty();
            }
        }
    }
}
