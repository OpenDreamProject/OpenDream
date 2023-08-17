using OpenDreamShared.Dream;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using System;


namespace OpenDreamShared.Rendering;
[Virtual]
public class SharedClientImagesSystem : EntitySystem {
    [Serializable, NetSerializable]
    public sealed class AddClientImageEvent : EntityEventArgs {
        public Vector3 TurfCoords;
        public EntityUid AttachedEntity; //if this is EntityUid.Invalid (ie, a turf) use the TurfCoords instead
        public uint ImageAppearance;

        public AddClientImageEvent(EntityUid attachedEntity, Vector3 turfCoords, uint imageAppearance) {
            TurfCoords = turfCoords;
            ImageAppearance = imageAppearance;
            AttachedEntity = attachedEntity;
        }
    }

    [Serializable, NetSerializable]
    public sealed class RemoveClientImageEvent : EntityEventArgs {
        public Vector3 TurfCoords;
        public EntityUid AttachedEntity; //if this is EntityUid.Invalid (ie, a turf) use the TurfCoords instead
        public uint ImageAppearance;

        public RemoveClientImageEvent(EntityUid attachedEntity, Vector3 turfCoords, uint imageAppearance) {
            TurfCoords = turfCoords;
            ImageAppearance = imageAppearance;
            AttachedEntity = attachedEntity;
        }
    }
}
