using System;

namespace OpenDreamShared.Net.Packets {
    class PacketBrowseResource : IPacket {
        public PacketID PacketID => PacketID.BrowseResource;
        public string Filename;
        public byte[] Data;

        public PacketBrowseResource() { }

        public PacketBrowseResource(string filename, byte[] data) {
            Filename = filename;
            Data = data;
        }

        public void ReadFromStream(PacketStream stream) {
            Filename = stream.ReadString();
            Data = stream.ReadBuffer();
        }

        public void WriteToStream(PacketStream stream) {
            stream.WriteString(Filename);
            stream.WriteBuffer(Data);
        }
    }
}
