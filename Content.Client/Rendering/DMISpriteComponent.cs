using Content.Shared;
using Content.Shared.Dream;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace OpenDreamClient.Rendering {
    [RegisterComponent]
    [ComponentReference(typeof(SharedDMISpriteComponent))]
    class DMISpriteComponent : SharedDMISpriteComponent {
        public ResourcePath Icon { get; set; }
        public string IconState { get; set; }
        public AtomDirection Direction { get; set; }
        public Vector2i PixelOffset { get; set; }
        public Color Color { get; set; }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState) {
            if (curState == null)
                return;

            DMISpriteComponentState state = (DMISpriteComponentState)curState;
            Icon = state.Icon;
            IconState = state.IconState;
            Direction = state.Direction;
            PixelOffset = state.PixelOffset;
            Color = state.Color;
        }
    }
}
