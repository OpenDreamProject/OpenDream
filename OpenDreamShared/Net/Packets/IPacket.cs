using System;
using System.Collections.Generic;

namespace OpenDreamShared.Net.Packets {
    public interface IPacket {
        public PacketID PacketID { get; }

        public void ReadFromStream(PacketStream stream);
        public void WriteToStream(PacketStream stream);

        public static Dictionary<PacketID, Type> PacketIDToType { get; } = new();

        public static IPacket CreatePacketFromData(byte[] packetData) {
            PacketStream stream = new PacketStream(packetData);
            PacketID packetID = (PacketID)stream.ReadByte();

            if (PacketIDToType.ContainsKey(packetID)) {
                Type packetType = PacketIDToType[packetID];
                IPacket packet = (IPacket)Activator.CreateInstance(packetType);

                packet.ReadFromStream(stream);
                return packet;
            } else {
                throw new ArgumentException("Invalid packet ID (" + packetID + ")");
            }
        }

        private static void RegisterPacket<PacketClass>(PacketID packetID) where PacketClass : IPacket, new() {
            if (PacketIDToType.ContainsKey(packetID)) throw new Exception("Packet ID '" + packetID + "' was already registered");

            PacketIDToType[packetID] = typeof(PacketClass);
        }
    }
}
