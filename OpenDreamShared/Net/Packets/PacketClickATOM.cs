using OpenDreamShared.Dream;
using System;

namespace OpenDreamShared.Net.Packets {
    class PacketClickAtom : IPacket {
        public PacketID PacketID => PacketID.ClickAtom;

        public UInt32 AtomID;
        public int IconX, IconY;
        public ScreenLocation ScreenLocation;
        public bool ModifierShift, ModifierCtrl, ModifierAlt;

        public PacketClickAtom() { }

        public PacketClickAtom(UInt32 atomID, int iconX, int iconY, ScreenLocation screenLocation) {
            AtomID = atomID;
            IconX = iconX;
            IconY = iconY;
            ScreenLocation = screenLocation;
        }

        public void ReadFromStream(PacketStream stream) {
            AtomID = stream.ReadUInt32();
            IconX = stream.ReadByte();
            IconY = stream.ReadByte();
            ScreenLocation = stream.ReadScreenLocation();
            ModifierShift = stream.ReadBool();
            ModifierCtrl = stream.ReadBool();
            ModifierAlt = stream.ReadBool();
        }

        public void WriteToStream(PacketStream stream) {
            stream.WriteUInt32(AtomID);
            stream.WriteByte((byte)IconX);
            stream.WriteByte((byte)IconY);
            stream.WriteScreenLocation(ScreenLocation);
            stream.WriteBool(ModifierShift);
            stream.WriteBool(ModifierCtrl);
            stream.WriteBool(ModifierAlt);
        }
    }
}
