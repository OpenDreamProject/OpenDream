using OpenDreamShared.Dream;
using OpenDreamShared.Rendering;

namespace OpenDreamRuntime.Rendering {
    [RegisterComponent]
    public sealed class DMISpriteComponent : SharedDMISpriteComponent {
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

        [ViewVariables]
        public IconAppearance? Appearance => (_appearanceId != null) ? EntitySystem.Get<ServerAppearanceSystem>().MustGetAppearance(_appearanceId.Value) : null;

        private uint? _appearanceId;

        public override ComponentState GetComponentState() {
            return new DMISpriteComponentState(_appearanceId, ScreenLocation);
        }

        public void SetAppearance(IconAppearance? appearance, bool dirty = true) {
            if (appearance == null) {
                _appearanceId = null;
            } else {
                if (_appearanceSystem is null) {
                    EntitySystem.TryGet<ServerAppearanceSystem>(out _appearanceSystem);
                }

                _appearanceId = _appearanceSystem?.AddAppearance(appearance);
            }

            if (dirty) {
                Dirty();
            }
        }
    }
}
