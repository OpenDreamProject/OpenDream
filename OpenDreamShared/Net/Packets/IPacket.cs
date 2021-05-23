using System;
using System.Collections.Generic;

namespace OpenDreamShared.Net.Packets {
    public interface IPacket {
        public PacketID PacketID { get; }

        public void ReadFromStream(PacketStream stream);
        public void WriteToStream(PacketStream stream);

        public static Dictionary<PacketID, Type> PacketIDToType { get; } = new Dictionary<PacketID, Type>();

        static IPacket() {
            RegisterPacket<PacketConnectionResult>(PacketID.ConnectionResult);
            RegisterPacket<PacketRequestConnect>(PacketID.RequestConnect);
            RegisterPacket<PacketInterfaceData>(PacketID.InterfaceData);
            RegisterPacket<PacketOutput>(PacketID.Output);
            RegisterPacket<PacketRequestResource>(PacketID.RequestResource);
            RegisterPacket<PacketResource>(PacketID.Resource);
            RegisterPacket<PacketFullGameState>(PacketID.FullGameState);
            RegisterPacket<PacketDeltaGameState>(PacketID.DeltaGameState);
            RegisterPacket<PacketKeyboardInput>(PacketID.KeyboardInput);
            RegisterPacket<PacketTopic>(PacketID.Topic);
            RegisterPacket<PacketClickAtom>(PacketID.ClickAtom);
            RegisterPacket<PacketSound>(PacketID.Sound);
            RegisterPacket<PacketBrowse>(PacketID.Browse);
            RegisterPacket<PacketBrowseResource>(PacketID.BrowseResource);
            RegisterPacket<PacketPrompt>(PacketID.Prompt);
            RegisterPacket<PacketPromptResponse>(PacketID.PromptResponse);
            RegisterPacket<PacketCallVerb>(PacketID.CallVerb);
            RegisterPacket<PacketUpdateAvailableVerbs>(PacketID.UpdateAvailableVerbs);
            RegisterPacket<PacketUpdateStatPanels>(PacketID.UpdateStatPanels);
        }

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
            if (PacketIDToType.ContainsKey(packetID)) throw new Exception("Packet ID '" + packetID.ToString() + "' was already registered");

            PacketIDToType[packetID] = typeof(PacketClass);
        }
    }
}
