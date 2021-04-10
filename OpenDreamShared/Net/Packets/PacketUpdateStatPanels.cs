using System.Collections.Generic;

namespace OpenDreamShared.Net.Packets {
    class PacketUpdateStatPanels : IPacket {
        public PacketID PacketID => PacketID.UpdateStatPanels;

        public Dictionary<string, List<string>> StatPanels;

        public PacketUpdateStatPanels() { }

        public PacketUpdateStatPanels(Dictionary<string, List<string>> statPanels) {
            StatPanels = statPanels;
        }

        public void ReadFromStream(PacketStream stream) {
            StatPanels = new Dictionary<string, List<string>>();

            int statPanelCount = stream.ReadByte();
            for (int i = 0; i < statPanelCount; i++) {
                string panelName = stream.ReadString();
                List<string> lines = new();

                int lineCount = stream.ReadByte();
                for (int j = 0; j < lineCount; j++) {
                    lines.Add(stream.ReadString());
                }

                StatPanels.Add(panelName, lines);
            }
        }

        public void WriteToStream(PacketStream stream) {
            stream.WriteByte((byte)StatPanels.Count);
            foreach (KeyValuePair<string, List<string>> statPanel in StatPanels) {
                stream.WriteString(statPanel.Key);

                stream.WriteByte((byte)statPanel.Value.Count);
                foreach (string line in statPanel.Value) {
                    stream.WriteString(line);
                }
            }
        }
    }
}
