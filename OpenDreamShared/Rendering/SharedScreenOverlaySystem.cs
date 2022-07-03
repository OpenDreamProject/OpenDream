using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;

namespace OpenDreamShared.Rendering {
    [Virtual]
    public class SharedScreenOverlaySystem : EntitySystem {
        [Serializable, NetSerializable]
        public sealed class AddScreenObjectEvent : EntityEventArgs {
            public EntityUid ScreenObject;

            public AddScreenObjectEvent(EntityUid screenObject) {
                ScreenObject = screenObject;
            }
        }

        [Serializable, NetSerializable]
        public sealed class RemoveScreenObjectEvent : EntityEventArgs {
            public EntityUid ScreenObject;

            public RemoveScreenObjectEvent(EntityUid screenObject) {
                ScreenObject = screenObject;
            }
        }
    }
}
