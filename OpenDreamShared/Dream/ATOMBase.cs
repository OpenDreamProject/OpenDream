using System;
using System.Collections.Generic;

namespace OpenDreamShared.Dream {
    public enum ATOMType {
        Atom = 0x0,
        Area = 0x1,
        Turf = 0x2,
        Movable = 0x3
    }

    public struct ATOMBase {
        public UInt16 ID;
        public ATOMType Type;
        public IconVisualProperties VisualProperties;

        public static Dictionary<UInt16, ATOMBase> AtomBases;

        public ATOMBase(UInt16 id, ATOMType type, IconVisualProperties visualProperties) {
            ID = id;
            Type = type;
            VisualProperties = visualProperties;
        }
    }
}
