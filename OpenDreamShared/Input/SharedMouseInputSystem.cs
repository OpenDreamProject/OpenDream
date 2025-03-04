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
    public struct ClickParams(ScreenLocation screenLoc, bool right, bool middle, bool shift, bool ctrl, bool alt, int iconX, int iconY) {
        public ScreenLocation ScreenLoc { get; } = screenLoc;
        public bool Right { get; } = right;
        public bool Middle { get; } = middle;
        public bool Shift { get; } = shift;
        public bool Ctrl { get; } = ctrl;
        public bool Alt { get; } = alt;
        public int IconX { get; } = iconX;
        public int IconY { get; } = iconY;
    }

    [Serializable, NetSerializable]
    public sealed class AtomClickedEvent(ClientObjectReference clickedAtom, ClickParams clickParams) : EntityEventArgs, IAtomMouseEvent {
        public ClientObjectReference ClickedAtom { get; } = clickedAtom;
        public ClickParams Params { get; } = clickParams;
    }

    [Serializable, NetSerializable]
    public sealed class AtomDraggedEvent(ClientObjectReference srcAtom, ClientObjectReference? over, ClickParams clickParams) : EntityEventArgs, IAtomMouseEvent {
        public ClientObjectReference SrcAtom { get; } = srcAtom;
        public ClientObjectReference? OverAtom { get; } = over;
        public ClickParams Params { get; } = clickParams;
    }

    [Serializable, NetSerializable]
    public sealed class StatClickedEvent(string atomRef, bool right, bool middle, bool shift, bool ctrl, bool alt)
        : EntityEventArgs, IAtomMouseEvent {
        public string AtomRef = atomRef; // TODO: Use ClientObjectReference

        // TODO: icon-x and icon-y
        // TODO: ScreenLoc doesn't appear at all in the click params
        public ClickParams Params { get; } = new(new(0, 0, 32), right, middle, shift, ctrl, alt, 0, 0);
    }

    [Serializable, NetSerializable]
    public sealed class MouseEnteredEvent(ClientObjectReference atom, ClickParams clickParams) : EntityEventArgs, IAtomMouseEvent {
        public ClientObjectReference Atom = atom;
        public ClickParams Params { get; } = clickParams;
    }

    [Serializable, NetSerializable]
    public sealed class MouseExitedEvent(ClientObjectReference atom, ClickParams clickParams) : EntityEventArgs, IAtomMouseEvent {
        public ClientObjectReference Atom = atom;
        public ClickParams Params { get; } = clickParams;
    }
}
