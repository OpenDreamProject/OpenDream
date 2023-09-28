using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;
using Robust.Shared.GameStates;

namespace OpenDreamShared.Rendering {
    [RegisterComponent]
    [NetworkedComponent]
    public sealed partial class DreamMobSightComponent : Component {
        //this would be a good place for:
        //see_in_dark
        //see_infrared

        public sbyte SeeInvisibility;
        public SightFlags Sight;
    }

    [Serializable, NetSerializable]
    internal sealed class DreamMobSightComponentState : ComponentState {
        public readonly sbyte SeeInvisibility;
        public readonly SightFlags Sight;

        public DreamMobSightComponentState(sbyte seeInvisibility, SightFlags sight) {
            SeeInvisibility = seeInvisibility;
            Sight = sight;
        }
    }
}
