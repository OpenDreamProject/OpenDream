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
        public int SeeInvisibility = 127; //most things don't have this, only mobs, so default to full visibility

        public DreamClientAppearanceComponent() {
        }

        public override ComponentState GetComponentState() {
            return new DreamClientAppearanceComponentState(SeeInvisibility);
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
