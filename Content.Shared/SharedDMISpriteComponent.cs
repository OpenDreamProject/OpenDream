using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System;

namespace Content.Shared {
    public class SharedDMISpriteComponent : Component {
        public override string Name => "DMISprite";
        public override uint? NetID => ContentNetIDs.DMI_SPRITE;

        [Serializable, NetSerializable]
        protected class DMISpriteComponentState : ComponentState {
            public readonly ResourcePath Icon;
            public readonly string IconState;

            public DMISpriteComponentState(ResourcePath icon, string iconState) : base(ContentNetIDs.DMI_SPRITE) {
                Icon = icon;
                IconState = iconState;
            }
        }
    }
}
