namespace OpenDreamShared.Net.Packets {
    public class PacketCallVerb : IPacket {
        public PacketID PacketID => PacketID.CallVerb;

        public string VerbName;

        public PacketCallVerb() { }

        public PacketCallVerb(string verbName) {
            VerbName = verbName;
        }

        public void ReadFromStream(PacketStream stream) {
            VerbName = stream.ReadString();
        }

        public void WriteToStream(PacketStream stream) {
            stream.WriteString(VerbName);
        }
    }
}
