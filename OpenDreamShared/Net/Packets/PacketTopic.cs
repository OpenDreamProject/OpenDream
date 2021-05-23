using System;
using System.Collections.Generic;
using System.Text;

namespace OpenDreamShared.Net.Packets {
    public class PacketTopic : IPacket {
        public PacketID PacketID => PacketID.Topic;

        public string Query;

        public PacketTopic() { }

        public PacketTopic(string query) {
            Query = query;
        }

        public void ReadFromStream(PacketStream stream) {
            Query = stream.ReadString();
        }

        public void WriteToStream(PacketStream stream) {
            stream.WriteString(Query);
        }
    }
}
