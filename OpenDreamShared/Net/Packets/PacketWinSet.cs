using System;

namespace OpenDreamShared.Net.Packets {
    public class PacketWinSet : IPacket {
        public PacketID PacketID => PacketID.WinSet;

        public string ControlId, Params;

        public PacketWinSet() { }

        public PacketWinSet(string controlId, string @params) {
            ControlId = controlId;
            Params = @params;
        }

        public void ReadFromStream(PacketStream stream) {
            ControlId = stream.ReadString();
            Params = stream.ReadString();
        }

        public void WriteToStream(PacketStream stream) {
            stream.WriteString(ControlId ?? String.Empty);
            stream.WriteString(Params);
        }
    }
}
