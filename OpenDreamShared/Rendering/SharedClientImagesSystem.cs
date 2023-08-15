using OpenDreamShared.Dream;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;

namespace OpenDreamShared.Rendering;
[Virtual]
public class SharedClientImagesSystem : EntitySystem {
    [Serializable, NetSerializable]
    public sealed class AddClientImageEvent : EntityEventArgs {
        public uint AttachedAppearance;
        public EntityUid AttachedEntity; //if this is EntityUid.Invalid (ie, a turf) use the AttachedAppearance instead
        public uint ImageAppearance;

        public AddClientImageEvent(EntityUid attachedEntity, uint attachedAppearance, uint imageAppearance) {
            AttachedAppearance = attachedAppearance;
            ImageAppearance = imageAppearance;
            AttachedEntity = attachedEntity;
        }
    }

    [Serializable, NetSerializable]
    public sealed class RemoveClientImageEvent : EntityEventArgs {
        public uint AttachedAppearance;
        public EntityUid AttachedEntity; //if this is EntityUid.Invalid (ie, a turf) use the AttachedAppearance instead
        public uint ImageAppearance;

        public RemoveClientImageEvent(EntityUid attachedEntity, uint attachedAppearance, uint imageAppearance) {
            AttachedAppearance = attachedAppearance;
            ImageAppearance = imageAppearance;
            AttachedEntity = attachedEntity;
        }
    }
}
