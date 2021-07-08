using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace OpenDreamClient.Dream {
    public class ATOM {
        [ViewVariables] public UInt32 ID;
        [ViewVariables] public AtomType Type;
        [ViewVariables] public DreamIcon Icon { get; } = new DreamIcon();
        [ViewVariables] public List<ATOM> Contents = new();
        [ViewVariables] public ScreenLocation ScreenLocation = new ScreenLocation(0, 0, 0, 0);

        [ViewVariables]
        public ATOM Loc {
            get {
                if (Type == AtomType.Turf) {
                    return null;
                } else {
                    return _loc;
                }
            }
            set {
                if (_loc != null) {
                    _loc.Contents.Remove(this);
                }

                _loc = value;
                if (_loc != null) _loc.Contents.Add(this);
            }
        }

        [ViewVariables]
        public int X {
            get {
                if (Type == AtomType.Turf) {
                    return _x;
                } else {
                    return (Loc != null) ? Loc.X : 0;
                }
            }
            set {
                if (Type == AtomType.Turf) {
                    _x = value;
                }
            }
        }

        [ViewVariables]
        public int Y {
            get {
                if (Type == AtomType.Turf) {
                    return _y;
                } else {
                    return (Loc != null) ? Loc.Y : 0;
                }
            }
            set {
                if (Type == AtomType.Turf) {
                    _y = value;
                }
            }
        }

        [ViewVariables]
        public int Z {
            get {
                if (Type == AtomType.Turf) {
                    return _z;
                } else {
                    return (Loc != null) ? Loc.Z : 0;
                }
            }
            set {
                if (Type == AtomType.Turf) {
                    _z = value;
                }
            }
        }

        [ViewVariables] private ATOM _loc = null;
        [ViewVariables] private int _x, _y, _z; //Only used for turfs

        public ATOM(UInt32 id, AtomType type, int appearanceId) {
            ID = id;
            Type = type;
            var openDream = IoCManager.Resolve<OpenDream>();
            Icon.Appearance = openDream.IconAppearances[appearanceId];

            openDream.AddATOM(this);
        }
    }
}
