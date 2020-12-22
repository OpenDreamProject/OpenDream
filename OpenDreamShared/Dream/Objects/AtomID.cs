using System;

namespace OpenDreamShared.Dream.Objects {
    struct AtomID {
        public static AtomID NullAtom = new AtomID(UInt32.MaxValue);

        public UInt32 ID;

        public AtomID(UInt32 id) {
            ID = id;
        }

        public override bool Equals(object obj) {
            if (obj is AtomID) return ((AtomID)obj).ID == ID;
            else return base.Equals(obj);
        }

        public override int GetHashCode() {
            return ID.GetHashCode();
        }

        public static bool operator ==(AtomID a, AtomID b) {
            return a.Equals(b);
        }

        public static bool operator !=(AtomID a, AtomID b) {
            return !a.Equals(b);
        }
    }
}
