using System;

namespace OpenDreamShared.Net.Packets {
    //Client -> Server: Tell the server what stat panel the client is now looking at
    //Server -> Client: Tell the client to switch stat panels
    public class PacketSelectStatPanel : IPacket {
        public PacketID PacketID => PacketID.SelectStatPanel;

        public string StatPanel;

        public PacketSelectStatPanel() { }

        public PacketSelectStatPanel(string statPanel) {
            if (statPanel == null) throw new ArgumentException("Selected stat panel cannot be null");

            StatPanel = statPanel;
        }

        public void ReadFromStream(PacketStream stream) {
            StatPanel = stream.ReadString();
        }

        public void WriteToStream(PacketStream stream) {
            stream.WriteString(StatPanel);
        }
    }
}
