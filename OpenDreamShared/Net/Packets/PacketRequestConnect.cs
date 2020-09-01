namespace OpenDreamShared.Net.Packets {
    class PacketRequestConnect : IPacket {
        public PacketID PacketID => PacketID.RequestConnect;
        public string CKey;

        public PacketRequestConnect() { }

        public PacketRequestConnect(string cKey) {
            CKey = cKey;
        }

        public void ReadFromStream(PacketStream stream) {
            CKey = stream.ReadString();
        }

        public void WriteToStream(PacketStream stream) {
            stream.WriteString(CKey);
        }
    }
}
