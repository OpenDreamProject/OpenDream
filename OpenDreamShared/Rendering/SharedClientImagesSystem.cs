using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;
using System.Numerics;

namespace OpenDreamShared.Rendering;

[Virtual]
public class SharedClientImagesSystem : EntitySystem {
    [Serializable, NetSerializable]
    public sealed class AddClientImageEvent(NetEntity attachedEntity, Vector3 turfCoords, NetEntity imageEntity)
        : EntityEventArgs {
        public Vector3 TurfCoords = turfCoords;
        public NetEntity AttachedEntity = attachedEntity; //if this is NetEntity.Invalid (ie, a turf) use the TurfCoords instead
        public NetEntity ImageEntity = imageEntity;
    }

    [Serializable, NetSerializable]
    public sealed class RemoveClientImageEvent(NetEntity attachedEntity, Vector3 turfCoords, NetEntity imageEntity)
        : EntityEventArgs {
        public Vector3 TurfCoords = turfCoords;
        public NetEntity AttachedEntity = attachedEntity; //if this is NetEntity.Invalid (ie, a turf) use the TurfCoords instead
        public NetEntity ImageEntity = imageEntity;
    }
}
