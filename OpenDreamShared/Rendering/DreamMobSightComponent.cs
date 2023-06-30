using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;
using Robust.Shared.GameStates;

namespace OpenDreamShared.Rendering {
    [RegisterComponent]
    [NetworkedComponent]
    public sealed class DreamMobSightComponent : Component {
        //this would be a good place for:
        //see_in_dark
        //see_infrared

        public sbyte SeeInvisibility;
        public SightFlags Sight;

        public override ComponentState GetComponentState() {
            return new DreamMobSightComponentState(SeeInvisibility, Sight);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState) {
            if (curState == null)
                return;

            DreamMobSightComponentState state = (DreamMobSightComponentState)curState;

            SeeInvisibility = state.SeeInvisibility;
            Sight = state.Sight;
        }

        [Serializable, NetSerializable]
        private sealed class DreamMobSightComponentState : ComponentState {
            public readonly sbyte SeeInvisibility;
            public readonly SightFlags Sight;

            public DreamMobSightComponentState(sbyte seeInvisibility, SightFlags sight) {
                SeeInvisibility = seeInvisibility;
                Sight = sight;
            }
        }
    }
}
