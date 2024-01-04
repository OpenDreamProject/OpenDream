﻿using Robust.Shared.Analyzers;
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
        public NetEntity AttachedEntity; //if this is NetEntity.Invalid (ie, a turf) use the TurfCoords instead
        public NetEntity ImageEntity;

        public AddClientImageEvent(NetEntity attachedEntity, Vector3 turfCoords, NetEntity imageEntity) {
            TurfCoords = turfCoords;
            ImageEntity = imageEntity;
            AttachedEntity = attachedEntity;
        }
    }

    [Serializable, NetSerializable]
    public sealed class RemoveClientImageEvent : EntityEventArgs {
        public Vector3 TurfCoords;
        public NetEntity AttachedEntity; //if this is NetEntity.Invalid (ie, a turf) use the TurfCoords instead
        public NetEntity ImageEntity;

        public RemoveClientImageEvent(NetEntity attachedEntity, Vector3 turfCoords, NetEntity imageEntity) {
            TurfCoords = turfCoords;
            ImageEntity = imageEntity;
            AttachedEntity = attachedEntity;
        }
    }
}
