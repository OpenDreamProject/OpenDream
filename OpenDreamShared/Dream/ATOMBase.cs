using System;
using System.Collections.Generic;

namespace OpenDreamShared.Dream {
    public enum ATOMType {
        Atom = 0x0,
        Area = 0x1,
        Turf = 0x2,
        Movable = 0x3
    }

    struct ATOMBase {
        public UInt16 ID;
        public ATOMType Type;
        public int IconAppearanceID;

        public static Dictionary<UInt16, ATOMBase> AtomBases;

        public ATOMBase(UInt16 id, ATOMType type, int iconAppearanceID) {
            ID = id;
            Type = type;
            IconAppearanceID = iconAppearanceID;
        }
    }
}
