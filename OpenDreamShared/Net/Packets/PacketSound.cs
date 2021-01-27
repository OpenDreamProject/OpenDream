using System;

namespace OpenDreamShared.Net.Packets {
    class PacketSound : IPacket {
        public PacketID PacketID => PacketID.Sound;

        public UInt16 Channel;
        public string File;
        public int Volume;

        public PacketSound() { }

        public PacketSound(UInt16 channel, string file, int volume) {
            Channel = channel;
            File = file;
            Volume = volume;
        }

        public void ReadFromStream(PacketStream stream) {
            File = stream.ReadString();
            if (File == String.Empty) File = null;

            Channel = stream.ReadUInt16();
            Volume = (File != null) ? stream.ReadByte() : 100;
        }

        public void WriteToStream(PacketStream stream) {
            stream.WriteString((File != null) ? File : String.Empty);
            stream.WriteUInt16(Channel);

            if (File != null) stream.WriteByte((byte)Volume);
        }
    }
}
