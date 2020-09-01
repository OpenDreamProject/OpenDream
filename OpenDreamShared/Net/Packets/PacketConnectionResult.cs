using System;

namespace OpenDreamShared.Net.Packets {
    class PacketConnectionResult : IPacket {
        public PacketID PacketID => PacketID.ConnectionResult;
        public bool ConnectionSuccessful;
        public string ErrorMessage = String.Empty;

        public PacketConnectionResult() { }

        public PacketConnectionResult(bool connectionSuccessful, string errorMessage) {
            ConnectionSuccessful = connectionSuccessful;
            ErrorMessage = errorMessage;
        }

        public void ReadFromStream(PacketStream stream) {
            ConnectionSuccessful = stream.ReadBool();
            if (!ConnectionSuccessful) {
                ErrorMessage = stream.ReadString();
            }
        }

        public void WriteToStream(PacketStream stream) {
            stream.WriteBool(ConnectionSuccessful);
            if (!ConnectionSuccessful) {
                stream.WriteString(ErrorMessage);
            }
        }
    }
}
