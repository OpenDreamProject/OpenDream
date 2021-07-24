using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System;
using Robust.Shared.GameStates;

namespace Content.Shared {
    [NetworkedComponent]
    public class SharedDMISpriteComponent : Component {
        public override string Name => "DMISprite";

        [Serializable, NetSerializable]
        protected class DMISpriteComponentState : ComponentState {
            public readonly ResourcePath Icon;
            public readonly string IconState;

            public DMISpriteComponentState(ResourcePath icon, string iconState) {
                Icon = icon;
                IconState = iconState;
            }
        }
    }
}
