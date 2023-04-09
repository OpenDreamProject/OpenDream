using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;
using Robust.Shared.GameStates;
using OpenDreamShared.Dream;
using System.Collections.Generic;

namespace OpenDreamShared.Rendering {
    [RegisterComponent]
    [NetworkedComponent]
    public class DreamMobSightComponent : Component {
        //this would be a good place for:
        //see_in_dark
        //see_infrared
        //sight

        public sbyte SeeInvisibility;

        public DreamMobSightComponent() {
        }

        public override ComponentState GetComponentState() {
            return new DreamMobSightComponentState(SeeInvisibility);
        }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState) {
            if (curState == null)
                return;

            DreamMobSightComponentState state = (DreamMobSightComponentState)curState;

            this.SeeInvisibility = state.SeeInvisibility;
        }

        [Serializable, NetSerializable]
        protected sealed class DreamMobSightComponentState : ComponentState {
            public readonly sbyte SeeInvisibility;

            public DreamMobSightComponentState(sbyte SeeInvisibility) {
                this.SeeInvisibility = SeeInvisibility;

            }
        }
    }




}
