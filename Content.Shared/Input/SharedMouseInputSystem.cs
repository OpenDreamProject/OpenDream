using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;

namespace Content.Shared.Input {
    public class SharedMouseInputSystem : EntitySystem {
        [Serializable, NetSerializable]
        public class EntityClickedEvent : EntityEventArgs {
            public EntityUid EntityUid { get; set; }

            public EntityClickedEvent(EntityUid entityUid) {
                EntityUid = entityUid;
            }
        }
    }
}
