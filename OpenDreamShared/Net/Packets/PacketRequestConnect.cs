/*using System.Text.Json;
using Lidgren.Network;
using Robust.Shared.Network;

namespace OpenDreamShared.Net.Packets {
    public class PacketRequestConnect : NetMessage {
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

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            CKey = buffer.ReadString();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            throw new System.NotImplementedException();
        }
    }
}
*/
