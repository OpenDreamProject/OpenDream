using Content.Shared;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace OpenDreamClient.Rendering {
    [RegisterComponent]
    [ComponentReference(typeof(SharedDMISpriteComponent))]
    class DMISpriteComponent : SharedDMISpriteComponent {
        public ResourcePath Icon { get; set; }
        public string IconState { get; set; }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState) {
            if (curState == null)
                return;

            DMISpriteComponentState state = (DMISpriteComponentState)curState;
            Icon = state.Icon;
            IconState = state.IconState;
        }
    }
}
