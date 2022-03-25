using OpenDreamShared.Dream;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;

namespace OpenDreamShared.Rendering {
    public abstract class SharedAppearanceSystem : EntitySystem {
        [Serializable, NetSerializable]
        public sealed class AllAppearancesEvent : EntityEventArgs {
            public Dictionary<uint, IconAppearance> Appearances = new();

            public AllAppearancesEvent(Dictionary<uint, IconAppearance> appearances) {
              Appearances = appearances;
            }
        }

        [Serializable, NetSerializable]
        public sealed class NewAppearanceEvent : EntityEventArgs {
            public uint AppearanceId { get; }
            public IconAppearance Appearance { get; }

            public NewAppearanceEvent(uint appearanceID, IconAppearance appearance) {
                AppearanceId = appearanceID;
                Appearance = appearance;
            }
        }

        [Serializable, NetSerializable]
        public sealed class AnimationEvent : EntityEventArgs {
            public EntityUid Entity;
            public uint TargetAppearanceId;
            public TimeSpan Duration;

            public AnimationEvent(EntityUid entity, uint targetAppearanceId, TimeSpan duration) {
                Entity = entity;
                TargetAppearanceId = targetAppearanceId;
                Duration = duration;
            }
        }
    }
}
