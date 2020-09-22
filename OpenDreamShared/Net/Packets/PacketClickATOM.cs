﻿using System;

namespace OpenDreamShared.Net.Packets {
    class PacketClickAtom : IPacket {
        public PacketID PacketID => PacketID.ClickAtom;
        public UInt16 AtomID;
        public int IconX, IconY;

        public PacketClickAtom() { }

        public PacketClickAtom(UInt16 atomID, int iconX, int iconY) {
            AtomID = atomID;
            IconX = iconX;
            IconY = iconY;
        }

        public void ReadFromStream(PacketStream stream) {
            AtomID = stream.ReadUInt16();
            IconX = stream.ReadByte();
            IconY = stream.ReadByte();
        }

        public void WriteToStream(PacketStream stream) {
            stream.WriteUInt16(AtomID);
            stream.WriteByte((byte)IconX);
            stream.WriteByte((byte)IconY);
        }
    }
}
