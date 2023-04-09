using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;
using Robust.Shared.GameStates;
using OpenDreamShared.Dream;

namespace OpenDreamShared.Rendering {
    [RegisterComponent]
    [NetworkedComponent]
    public class DreamClientAppearanceComponent : Component {
        //hey this'd probably be a good place for client images
        ///The current attached mob's see_invisible value

        public int SeeInvisibility;

        public DreamClientAppearanceComponent() {
        }

        public override ComponentState GetComponentState() {
            return new DreamClientAppearanceComponentState(SeeInvisibility);
        }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState) {
            if (curState == null)
                return;

            DreamClientAppearanceComponentState state = (DreamClientAppearanceComponentState)curState;

            this.SeeInvisibility = state.SeeInvisibility;
        }

        [Serializable, NetSerializable]
        protected sealed class DreamClientAppearanceComponentState : ComponentState {
            public readonly int SeeInvisibility;

            public DreamClientAppearanceComponentState(int SeeInvisibility) {
                this.SeeInvisibility = SeeInvisibility;
            }
        }
    }




}
