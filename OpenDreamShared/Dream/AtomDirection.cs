using Robust.Shared.Serialization;
using System;

namespace OpenDreamShared.Dream;

[Serializable, NetSerializable, Flags]
public enum AtomDirection : byte {
    None = 0,

    North = 1,
    South = 2,
    East = 4,
    West = 8,

    Up = 16,
    Down = 32,

    Northeast = North | East,
    Southeast = South | East,
    Southwest = South | West,
    Northwest = North | West
}

public static class AtomDirectionExtensions {
    public static bool IsValid(this AtomDirection dir) =>
        dir != AtomDirection.None
        && (dir & (AtomDirection.North | AtomDirection.South)) != (AtomDirection.North | AtomDirection.South)
        && (dir & (AtomDirection.East | AtomDirection.West)) != (AtomDirection.East | AtomDirection.West)
        && (dir & (AtomDirection.Up | AtomDirection.Down)) != (AtomDirection.Up | AtomDirection.Down);

    public static AtomDirection Cardinals(this AtomDirection dir) => dir & (AtomDirection.Northeast | AtomDirection.Southwest);
}
