using OpenDreamShared.Dream;
using OpenDreamShared.Rendering;

namespace OpenDreamRuntime.Rendering {
    [RegisterComponent]
    sealed class DMISpriteComponent : SharedDMISpriteComponent {
        [ViewVariables]
        public ScreenLocation? ScreenLocation {
            get => _screenLocation;
            set {
                _screenLocation = value;
                Dirty();
            }
        }
        private ScreenLocation? _screenLocation;

        [ViewVariables]
        public IconAppearance? Appearance {
            get => (_appearanceId != null) ? EntitySystem.Get<ServerAppearanceSystem>().GetAppearance(_appearanceId.Value) : null;
        }

        private uint? _appearanceId;

        public override ComponentState GetComponentState() {
            return new DMISpriteComponentState(_appearanceId, ScreenLocation);
        }

        public void SetAppearance(IconAppearance? appearance, bool dirty = true) {
            if (appearance == null) {
                _appearanceId = null;
            } else {
                _appearanceId = EntitySystem.Get<ServerAppearanceSystem>().AddAppearance(appearance);
            }

            if (dirty) {
                Dirty();
            }
        }
    }
}
