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
            ClientData = JsonSerializer.Deserialize<ClientData>(stream.ReadString());
        }

        public void WriteToStream(PacketStream stream) {
            stream.WriteString(CKey);
            stream.WriteString(JsonSerializer.Serialize(ClientData));
        }
    }
}
