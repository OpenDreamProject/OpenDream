using System;

namespace OpenDreamShared.Net.Packets {
    class PacketOutput : IPacket {
        public PacketID PacketID => PacketID.Output;
        public string Value;

        public PacketOutput() { }

        public PacketOutput(string value) {
            Value = value;
        }

        public void ReadFromStream(PacketStream stream) {
            Value = stream.ReadString();
        }

        public void WriteToStream(PacketStream stream) {
            stream.WriteString(Value);
        }
    }
}
