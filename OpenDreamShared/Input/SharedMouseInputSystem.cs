﻿using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;

namespace OpenDreamShared.Input {
    public class SharedMouseInputSystem : EntitySystem {
        [Serializable, NetSerializable]
        public class EntityClickedEvent : EntityEventArgs {
            public EntityUid EntityUid { get; }

            public EntityClickedEvent(EntityUid entityUid) {
                EntityUid = entityUid;
            }
        }
    }
}
