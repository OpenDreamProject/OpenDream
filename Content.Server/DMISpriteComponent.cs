using Content.Shared;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;
using Robust.Shared.Utility;

namespace Content.Server {
    [RegisterComponent]
    class DMISpriteComponent : SharedDMISpriteComponent {
        private static readonly ResourcePath GamePath = new ResourcePath("/Game");

        private ResourcePath _icon = null;
        private string _iconState = null;

        public ResourcePath Icon {
            get => _icon;
            set {
                _icon = GamePath / value;
                Dirty();
            }
        }

        public string IconState {
            get => _iconState;
            set {
                _iconState = value;
                Dirty();
            }
        }

        public override ComponentState GetComponentState(ICommonSession player) {
            return new DMISpriteComponentState(Icon, IconState);
        }
    }
}
