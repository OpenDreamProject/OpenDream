using System.Text.Json;

namespace OpenDreamShared.Net.Packets {
    public class PacketRequestConnect : IPacket {
        public PacketID PacketID => PacketID.RequestConnect;

        public string CKey;
        public ClientData ClientData;

        public PacketRequestConnect() { }

        public PacketRequestConnect(string cKey, ClientData clientData) {
            CKey = cKey;
            ClientData = clientData;
        }

        public void ReadFromStream(PacketStream stream) {
            CKey = stream.ReadString();
            ClientData = ClientData.ReadFromPacket(stream);
        }

        public void WriteToStream(PacketStream stream) {
            stream.WriteString(CKey);
            ClientData.WriteToPacket(stream);
        }
    }
}
