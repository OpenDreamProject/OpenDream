using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;
using OpenDreamShared.Dream;

namespace OpenDreamShared.Input;

[Virtual]
public class SharedMouseInputSystem : EntitySystem {
    protected interface IAtomMouseEvent {
        public ClickParams Params { get; }
    }

    [Serializable, NetSerializable]
    public struct ClickParams(ScreenLocation screenLoc, bool middle, bool shift, bool ctrl, bool alt, int iconX, int iconY) {
        public ScreenLocation ScreenLoc { get; } = screenLoc;
        public bool Middle { get; } = middle;
        public bool Shift { get; } = shift;
        public bool Ctrl { get; } = ctrl;
        public bool Alt { get; } = alt;
        public int IconX { get; } = iconX;
        public int IconY { get; } = iconY;
    }

    [Serializable, NetSerializable]
    public sealed class AtomClickedEvent(AtomReference atom, ClickParams clickParams) : EntityEventArgs, IAtomMouseEvent {
        public AtomReference Atom { get; } = atom;
        public ClickParams Params { get; } = clickParams;
    }

    [Serializable, NetSerializable]
    public sealed class AtomDraggedEvent(AtomReference src, AtomReference? over, ClickParams clickParams) : EntityEventArgs, IAtomMouseEvent {
        public AtomReference SrcAtom { get; } = src;
        public AtomReference? OverAtom { get; } = over;
        public ClickParams Params { get; } = clickParams;
    }

    [Serializable, NetSerializable]
    public sealed class StatClickedEvent(string atomRef, bool middle, bool shift, bool ctrl, bool alt)
        : EntityEventArgs, IAtomMouseEvent {
        public string AtomRef = atomRef;

        // TODO: icon-x and icon-y
        // TODO: ScreenLoc doesn't appear at all in the click params
        public ClickParams Params { get; } = new(new(0, 0, 32), middle, shift, ctrl, alt, 0, 0);
    }
}
