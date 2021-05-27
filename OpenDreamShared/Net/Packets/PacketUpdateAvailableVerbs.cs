namespace OpenDreamShared.Net.Packets {
    public class PacketUpdateAvailableVerbs : IPacket {
        public PacketID PacketID => PacketID.UpdateAvailableVerbs;

        public string[] AvailableVerbs;

        public PacketUpdateAvailableVerbs() { }

        public PacketUpdateAvailableVerbs(string[] availableVerbs) {
            AvailableVerbs = availableVerbs;
        }

        public void ReadFromStream(PacketStream stream) {
            int count = stream.ReadByte();

            AvailableVerbs = new string[count];
            for (int i = 0; i < count; i++) {
                AvailableVerbs[i] = stream.ReadString();
            }
        }

        public void WriteToStream(PacketStream stream) {
            stream.WriteByte((byte)AvailableVerbs.Length);

            foreach (string verb in AvailableVerbs) {
                stream.WriteString(verb);
            }
        }
    }
}
