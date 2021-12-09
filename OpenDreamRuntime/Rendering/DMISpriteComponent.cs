using OpenDreamShared.Dream;
using OpenDreamShared.Rendering;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;
using Robust.Shared.ViewVariables;

namespace OpenDreamRuntime.Rendering {
    [RegisterComponent]
    class DMISpriteComponent : SharedDMISpriteComponent {
        [ViewVariables]
        public uint? AppearanceId {
            get => _appearanceId;
            set {
                _appearanceId = value;
                Dirty();
            }
        }
        private uint? _appearanceId;

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
            get => (AppearanceId != null) ? EntitySystem.Get<ServerAppearanceSystem>().GetAppearance(AppearanceId.Value) : null;
            set => AppearanceId = (value != null) ? EntitySystem.Get<ServerAppearanceSystem>().AddAppearance(value) : null;
        }

        public override ComponentState GetComponentState() {
            return new DMISpriteComponentState(AppearanceId, ScreenLocation);
        }
    }
}
