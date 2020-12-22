using OpenDreamShared.Dream.Objects;
using System;

namespace OpenDreamShared.Net.Packets {
    class PacketClickAtom : IPacket {
        public PacketID PacketID => PacketID.ClickAtom;
        public AtomID AtomID;
        public int IconX, IconY;
        public bool ModifierShift, ModifierCtrl;

        public PacketClickAtom() { }

        public PacketClickAtom(AtomID atomID, int iconX, int iconY) {
            AtomID = atomID;
            IconX = iconX;
            IconY = iconY;
        }

        public void ReadFromStream(PacketStream stream) {
            AtomID = stream.ReadAtomID();
            IconX = stream.ReadByte();
            IconY = stream.ReadByte();
            ModifierShift = stream.ReadBool();
            ModifierCtrl = stream.ReadBool();
        }

        public void WriteToStream(PacketStream stream) {
            stream.WriteAtomID(AtomID);
            stream.WriteByte((byte)IconX);
            stream.WriteByte((byte)IconY);
            stream.WriteBool(ModifierShift);
            stream.WriteBool(ModifierCtrl);
        }
    }
}
