using OpenDreamShared.Dream;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;

namespace OpenDreamShared.Rendering {
    public abstract class SharedAppearanceSystem : EntitySystem {
        [Serializable, NetSerializable]
        public sealed class AllAppearancesEvent : EntityEventArgs {
            public Dictionary<int, IconAppearance> Appearances = new();

            public AllAppearancesEvent(Dictionary<int, IconAppearance> appearances) {
              Appearances = appearances;
            }
        }

        [Serializable, NetSerializable]
        public sealed class NewAppearanceEvent : EntityEventArgs {
            public int AppearanceId { get; }
            public IconAppearance Appearance { get; }

            public NewAppearanceEvent(int appearanceID, IconAppearance appearance) {
                AppearanceId = appearanceID;
                Appearance = appearance;
            }
        }

        [Serializable, NetSerializable]
        public sealed class AnimationEvent : EntityEventArgs {
            public NetEntity Entity;
            public int TargetAppearanceId;
            public TimeSpan Duration;

            public AnimationEvent(NetEntity entity, int targetAppearanceId, TimeSpan duration) {
                Entity = entity;
                TargetAppearanceId = targetAppearanceId;
                Duration = duration;
            }
        }
    }
}
