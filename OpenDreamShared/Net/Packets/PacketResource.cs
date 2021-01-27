using System;

namespace OpenDreamShared.Net.Packets {
    class PacketResource : IPacket {
        public PacketID PacketID => PacketID.Resource;

        public string ResourcePath;
        public byte[] ResourceData;

        public PacketResource() { }

        public PacketResource(string resourcePath, byte[] resourceData) {
            ResourcePath = resourcePath;
            ResourceData = resourceData;
        }

        public void ReadFromStream(PacketStream stream) {
            ResourcePath = stream.ReadString();
            ResourceData = stream.ReadBuffer();
        }

        public void WriteToStream(PacketStream stream) {
            stream.WriteString(ResourcePath);
            stream.WriteBuffer(ResourceData);
        }
    }
}
