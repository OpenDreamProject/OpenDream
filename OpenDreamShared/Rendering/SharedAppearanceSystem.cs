using OpenDreamShared.Dream;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;

namespace OpenDreamShared.Rendering;

public abstract class SharedAppearanceSystem : EntitySystem {
    [Serializable, NetSerializable]
    public sealed class AllAppearancesEvent(Dictionary<int, IconAppearance> appearances) : EntityEventArgs {
        public Dictionary<int, IconAppearance> Appearances = appearances;
    }

    [Serializable, NetSerializable]
    public sealed class NewAppearanceEvent(int appearanceId, IconAppearance appearance) : EntityEventArgs {
        public int AppearanceId { get; } = appearanceId;
        public IconAppearance Appearance { get; } = appearance;
    }

    [Serializable, NetSerializable]
    public sealed class AnimationEvent(NetEntity entity, int targetAppearanceId, TimeSpan duration)
        : EntityEventArgs {
        public NetEntity Entity = entity;
        public int TargetAppearanceId = targetAppearanceId;
        public TimeSpan Duration = duration;
    }
}
