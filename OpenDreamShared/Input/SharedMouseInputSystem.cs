using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;

namespace OpenDreamShared.Input {
    [Virtual]
    public class SharedMouseInputSystem : EntitySystem {
        [Serializable, NetSerializable]
        public sealed class EntityClickedEvent : EntityEventArgs {
            public EntityUid EntityUid { get; }
            public bool Shift { get; }
            public bool Ctrl { get; }
            public bool Alt { get; }

            public EntityClickedEvent(EntityUid entityUid, bool shift, bool ctrl, bool alt) {
                EntityUid = entityUid;
                Shift = shift;
                Ctrl = ctrl;
                Alt = alt;
            }
        }
    }
}
