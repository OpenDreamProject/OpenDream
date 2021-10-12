using OpenDreamShared;
using OpenDreamShared.Dream;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;
using Robust.Shared.ViewVariables;

namespace OpenDreamRuntime {
    [RegisterComponent]
    class DMISpriteComponent : SharedDMISpriteComponent {
        private uint? _appearanceId;

        [ViewVariables]
        public uint? AppearanceId {
            get => _appearanceId;
            set {
                _appearanceId = value;
                Dirty();
            }
        }

        [ViewVariables]
        public IconAppearance? Appearance {
            get => (AppearanceId != null) ? EntitySystem.Get<ServerAppearanceSystem>().GetAppearance(AppearanceId.Value) : null;
            set => AppearanceId = (value != null) ? EntitySystem.Get<ServerAppearanceSystem>().AddAppearance(value) : null;
        }

        public override ComponentState GetComponentState(ICommonSession player) {
            return new DMISpriteComponentState(AppearanceId);
        }
    }
}
