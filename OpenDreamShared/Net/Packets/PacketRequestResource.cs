namespace OpenDreamShared.Net.Packets {
    class PacketRequestResource : IPacket {
        public PacketID PacketID => PacketID.RequestResource;
        public string ResourcePath;

        public PacketRequestResource() { }

        public PacketRequestResource(string resourcePath) {
            ResourcePath = resourcePath;
        }

        public void ReadFromStream(PacketStream stream) {
            ResourcePath = stream.ReadString();
        }

        public void WriteToStream(PacketStream stream) {
            stream.WriteString(ResourcePath);
        }
    }
}
