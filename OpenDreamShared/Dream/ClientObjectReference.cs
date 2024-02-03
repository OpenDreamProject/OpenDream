using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace OpenDreamShared.Dream;

/// <summary>
/// Used by the client to refer to something that could be either its own client, a turf, or an entity
/// </summary>
/// <remarks>This should only be used on the client or when communicating with the client</remarks>
[Serializable, NetSerializable]
public struct ClientObjectReference {
    public enum RefType {
        Client,
        Turf,
        Entity
    }

    public static readonly ClientObjectReference Client = new() { Type = RefType.Client };

    public RefType Type;
    public NetEntity Entity;
    public int TurfX, TurfY, TurfZ;

    public ClientObjectReference(Vector2i turfPos, int z) {
        Type = RefType.Turf;
        (TurfX, TurfY) = turfPos;
        TurfZ = z;
    }

    public ClientObjectReference(NetEntity entity) {
        Type = RefType.Entity;
        Entity = entity;
    }
}
