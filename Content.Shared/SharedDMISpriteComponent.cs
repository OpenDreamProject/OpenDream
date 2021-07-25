using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Content.Shared.Dream;

namespace Content.Shared {
    [NetworkedComponent]
    public class SharedDMISpriteComponent : Component {
        public override string Name => "DMISprite";

        [Serializable, NetSerializable]
        protected class DMISpriteComponentState : ComponentState {
            public readonly ResourcePath Icon;
            public readonly string IconState;
            public readonly Color Color;
            public readonly Vector2i PixelOffset;
            public readonly AtomDirection Direction;
            public readonly float Layer;

            public DMISpriteComponentState(ResourcePath icon, string iconState, AtomDirection direction, Vector2i pixelOfffset, Color color, float layer) {
                Icon = icon;
                IconState = iconState;
                Direction = direction;
                PixelOffset = pixelOfffset;
                Color = color;
                Layer = layer;
            }
        }
    }
}
