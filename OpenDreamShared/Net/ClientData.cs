using System;
using OpenDreamShared.Net.Packets;

namespace OpenDreamShared.Net {
    public class ClientData {

        public TimeZoneInfo Timezone;

        public ClientData(bool setDefaults = false) {
            if (setDefaults) {
                Timezone = TimeZoneInfo.Local;
            }
        }

        public void WriteToPacket(PacketStream packetStream) {
            packetStream.WriteString(Timezone.ToSerializedString());
        }

        public static ClientData ReadFromPacket(PacketStream packetStream) {
            return new ClientData() {
                Timezone = TimeZoneInfo.Local // TODO ROBUST: Fix this
            };
        }
    }
}
