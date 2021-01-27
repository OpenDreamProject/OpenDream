using System;

namespace OpenDreamShared.Net.Packets {
    class PacketOutput : IPacket {
        public PacketID PacketID => PacketID.Output;

        public string Value;
        public string Control;

        public PacketOutput() { }

        public PacketOutput(string value, string control) {
            Value = value;
            Control = control;
        }

        public void ReadFromStream(PacketStream stream) {
            Value = stream.ReadString();
            Control = stream.ReadString();
            if (Control == String.Empty) Control = null;
        }

        public void WriteToStream(PacketStream stream) {
            stream.WriteString(Value);
            stream.WriteString((Control != null) ? Control : String.Empty);
        }
    }
}
