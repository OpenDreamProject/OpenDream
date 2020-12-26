using OpenDreamShared.Dream;
using OpenDreamShared.Net.Packets;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace OpenDreamClient.Dream {
    class ATOM {
        public UInt16 ID;
        public ATOMType Type;
        public DreamIcon Icon { get; } = new DreamIcon();
        public List<ATOM> Contents = new List<ATOM>();
        public ScreenLocation ScreenLocation = new ScreenLocation();

        public ATOM Loc {
            get {
                if (Type == ATOMType.Turf) {
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

        public int X {
            get {
                if (Type == ATOMType.Turf) {
                    return _x;
                } else {
                    return (Loc != null) ? Loc.X : 0;
                }
            }
            set {
                if (Type == ATOMType.Turf) {
                    _x = value;
                }
            }
        }

        public int Y {
            get {
                if (Type == ATOMType.Turf) {
                    return _y;
                } else {
                    return (Loc != null) ? Loc.Y : 0;
                }
            }
            set {
                if (Type == ATOMType.Turf) {
                    _y = value;
                }
            }
        }

        private ATOM _loc = null;
        private int _x, _y; //Only used for turfs

        public ATOM(UInt16 id, ATOMBase atomBase) {
            ID = id;
            Type = atomBase.Type;
            Icon.VisualProperties = atomBase.VisualProperties;

            Program.OpenDream.AddATOM(this);
        }

        public static void HandlePacketAtomBases(PacketATOMTypes pAtomBases) {
            ATOMBase.AtomBases = pAtomBases.AtomBases;
        }
    }
}
