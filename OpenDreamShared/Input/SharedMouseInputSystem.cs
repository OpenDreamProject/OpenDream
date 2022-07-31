using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;
using Robust.Shared.Maths;

namespace OpenDreamShared.Input {
    [Virtual]
    public class SharedMouseInputSystem : EntitySystem {
        protected interface IAtomClickedEvent {
            public bool Shift { get; }
            public bool Ctrl { get; }
            public bool Alt { get; }
        }

        [Serializable, NetSerializable]
        public sealed class EntityClickedEvent : EntityEventArgs, IAtomClickedEvent {
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

        [Serializable, NetSerializable]
        public sealed class TurfClickedEvent : EntityEventArgs, IAtomClickedEvent {
            public Vector2i Position;
            public int Z;
            public bool Shift { get; }
            public bool Ctrl { get; }
            public bool Alt { get; }

            public TurfClickedEvent(Vector2i position, int z, bool shift, bool ctrl, bool alt) {
                Position = position;
                Z = z;
                Shift = shift;
                Ctrl = ctrl;
                Alt = alt;
            }
        }
    }
}
