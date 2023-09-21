using OpenDreamShared.Dream;
using OpenDreamShared.Rendering;

namespace OpenDreamRuntime.Rendering {
    [RegisterComponent]
    public sealed partial class DMISpriteComponent : SharedDMISpriteComponent {
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

        public void SetAppearance(IconAppearance? appearance, bool dirty = true) {
            Appearance = appearance;

            if (dirty) {
                Dirty();
            }
        }
    }
}
