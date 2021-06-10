using System;

namespace OpenDreamShared.Net.Packets {
    public class PacketConnectionResult : IPacket {
        public PacketID PacketID => PacketID.ConnectionResult;

        public bool ConnectionSuccessful;
        public string ErrorMessage = String.Empty;
        public string InterfaceData = String.Empty;

        public PacketConnectionResult() { }

        public PacketConnectionResult(bool connectionSuccessful, string errorMessage, string interfaceData) {
            ConnectionSuccessful = connectionSuccessful;
            ErrorMessage = errorMessage;
            InterfaceData = interfaceData;
        }

        public void ReadFromStream(PacketStream stream) {
            ConnectionSuccessful = stream.ReadBool();

            if (ConnectionSuccessful) {
                InterfaceData = stream.ReadString();
            } else {
                ErrorMessage = stream.ReadString();
            }
        }

        public void WriteToStream(PacketStream stream) {
            stream.WriteBool(ConnectionSuccessful);

            if (ConnectionSuccessful) {
                stream.WriteString(InterfaceData);
            } else {
                stream.WriteString(ErrorMessage);
            }
        }
    }
}
