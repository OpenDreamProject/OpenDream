using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;

namespace OpenDreamShared.Rendering {
    public class SharedScreenOverlaySystem : EntitySystem {
        [Serializable, NetSerializable]
        public class AddScreenObjectEvent : EntityEventArgs {
            public EntityUid ScreenObject;

            public AddScreenObjectEvent(EntityUid screenObject) {
                ScreenObject = screenObject;
            }
        }

        [Serializable, NetSerializable]
        public class RemoveScreenObjectEvent : EntityEventArgs {
            public EntityUid ScreenObject;

            public RemoveScreenObjectEvent(EntityUid screenObject) {
                ScreenObject = screenObject;
            }
        }
    }
}
