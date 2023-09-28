using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;
using OpenDreamShared.Dream;
using Robust.Shared.Maths;

namespace OpenDreamShared.Input;

[Virtual]
public class SharedMouseInputSystem : EntitySystem {
    protected interface IAtomClickedEvent {
        public ScreenLocation ScreenLoc { get; }
        public bool Middle { get; }
        public bool Shift { get; }
        public bool Ctrl { get; }
        public bool Alt { get; }
        public int IconX { get; }
        public int IconY { get; }
    }

    [Serializable, NetSerializable]
    public sealed class EntityClickedEvent : EntityEventArgs, IAtomClickedEvent {
        public NetEntity NetEntity { get; }
        public ScreenLocation ScreenLoc { get; }
        public bool Middle { get; }
        public bool Shift { get; }
        public bool Ctrl { get; }
        public bool Alt { get; }
        public int IconX { get; }
        public int IconY { get; }

        public EntityClickedEvent(NetEntity netEntity, ScreenLocation screenLoc, bool middle, bool shift, bool ctrl, bool alt, Vector2i iconPos) {
            NetEntity = netEntity;
            ScreenLoc = screenLoc;
            Middle = middle;
            Shift = shift;
            Ctrl = ctrl;
            Alt = alt;
            IconX = iconPos.X;
            IconY = iconPos.Y;
        }
    }

    [Serializable, NetSerializable]
    public sealed class TurfClickedEvent : EntityEventArgs, IAtomClickedEvent {
        public Vector2i Position;
        public ScreenLocation ScreenLoc { get; }
        public int Z;
        public bool Middle { get; }
        public bool Shift { get; }
        public bool Ctrl { get; }
        public bool Alt { get; }
        public int IconX { get; }
        public int IconY { get; }

        public TurfClickedEvent(Vector2i position, int z, ScreenLocation screenLoc, bool middle, bool shift, bool ctrl, bool alt, Vector2i iconPos) {
            Position = position;
            Z = z;
            ScreenLoc = screenLoc;
            Middle = middle;
            Shift = shift;
            Ctrl = ctrl;
            Alt = alt;
            IconX = iconPos.X;
            IconY = iconPos.Y;
        }
    }

    [Serializable, NetSerializable]
    public sealed class StatClickedEvent : EntityEventArgs, IAtomClickedEvent {
        public string AtomRef;
        public bool Middle { get; }
        public bool Shift { get; }
        public bool Ctrl { get; }
        public bool Alt { get; }

        // TODO: icon-x and icon-y
        public int IconX => 0;
        public int IconY => 0;

        // This doesn't seem to appear at all in the click params
        public ScreenLocation ScreenLoc => new(0, 0, 32);

        public StatClickedEvent(string atomRef, bool middle, bool shift, bool ctrl, bool alt) {
            AtomRef = atomRef;
            Middle = middle;
            Shift = shift;
            Ctrl = ctrl;
            Alt = alt;
        }
    }
}
