using OpenDreamShared.Dream;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;

namespace OpenDreamShared {
    public abstract class SharedAppearanceSystem : EntitySystem {
        [Serializable, NetSerializable]
        public class AllAppearancesEvent : EntityEventArgs {
            public Dictionary<uint, IconAppearance> Appearances = new();

            public AllAppearancesEvent(Dictionary<uint, IconAppearance> appearances) {
              Appearances = appearances;
            }
        }

        [Serializable, NetSerializable]
        public class NewAppearanceEvent : EntityEventArgs {
            public uint AppearanceId { get; }
            public IconAppearance Appearance { get; }

            public NewAppearanceEvent(uint appearanceID, IconAppearance appearance) {
                AppearanceId = appearanceID;
                Appearance = appearance;
            }
        }
    }
}
