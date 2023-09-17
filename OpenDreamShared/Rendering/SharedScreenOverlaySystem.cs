using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;

namespace OpenDreamShared.Rendering {
    [Virtual]
    public class SharedScreenOverlaySystem : EntitySystem {
        [Serializable, NetSerializable]
        public sealed class AddScreenObjectEvent : EntityEventArgs {
            public NetEntity ScreenObject;

            public AddScreenObjectEvent(NetEntity screenObject) {
                ScreenObject = screenObject;
            }
        }

        [Serializable, NetSerializable]
        public sealed class RemoveScreenObjectEvent : EntityEventArgs {
            public NetEntity ScreenObject;

            public RemoveScreenObjectEvent(NetEntity screenObject) {
                ScreenObject = screenObject;
            }
        }
    }
}
