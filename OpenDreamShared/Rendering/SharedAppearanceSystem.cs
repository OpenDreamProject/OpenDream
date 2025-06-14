using OpenDreamShared.Dream;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;

namespace OpenDreamShared.Rendering;

public abstract class SharedAppearanceSystem : EntitySystem {
    public abstract ImmutableAppearance MustGetAppearanceById(uint appearanceId);
    public abstract void RemoveAppearance(ImmutableAppearance appearance);

    [Serializable, NetSerializable]
    public sealed class NewAppearanceEvent(ImmutableAppearance appearance) : EntityEventArgs {
        public ImmutableAppearance Appearance { get; } = appearance;
    }

    [Serializable, NetSerializable]
    public sealed class RemoveAppearanceEvent(uint appearanceId) : EntityEventArgs {
        public uint AppearanceId { get; } = appearanceId;
    }

    [Serializable, NetSerializable]
    public sealed class AnimationEvent(NetEntity entity, uint targetAppearanceId, TimeSpan duration, AnimationEasing easing, int loop, AnimationFlags flags, int delay, bool chainAnim, uint? turfId)
        : EntityEventArgs {
        public NetEntity Entity = entity;
        public uint TargetAppearanceId = targetAppearanceId;
        public TimeSpan Duration = duration;
        public AnimationEasing Easing = easing;
        public int Loop = loop;
        public AnimationFlags Flags = flags;
        public int Delay = delay;
        public bool ChainAnim = chainAnim;
        public uint? TurfId = turfId;
    }

    [Serializable, NetSerializable]
    public sealed class FlickEvent(ClientObjectReference clientRef, int iconId, string? iconState) : EntityEventArgs {
        public ClientObjectReference ClientRef = clientRef;
        public int IconId = iconId;
        public string? IconState = iconState;
    }
}
