using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace OpenDreamShared.Dream;

/// <summary>
/// Used by the client to refer to something that could be either a turf or an entity
/// </summary>
/// <remarks>This should only be used on the client or when communicating with the client</remarks>
[Serializable, NetSerializable]
public struct AtomReference {
    public enum RefType {
        Turf,
        Entity
    }

    public RefType AtomType;
    public NetEntity Entity;
    public int TurfX, TurfY, TurfZ;

    public AtomReference(Vector2i turfPos, int z) {
        AtomType = RefType.Turf;
        (TurfX, TurfY) = turfPos;
        TurfZ = z;
    }

    public AtomReference(NetEntity entity) {
        AtomType = RefType.Entity;
        Entity = entity;
    }
}
