using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;
using Robust.Shared.GameStates;

namespace OpenDreamShared {
    [NetworkedComponent]
    public class SharedDMISpriteComponent : Component {
        public override string Name => "DMISprite";

        [Serializable, NetSerializable]
        protected class DMISpriteComponentState : ComponentState {
            public readonly uint? AppearanceId;

            public DMISpriteComponentState(uint? appearanceId) {
                AppearanceId = appearanceId;
            }
        }
    }
}
