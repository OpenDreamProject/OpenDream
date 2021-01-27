using OpenDreamShared.Dream.Procs;
using System;

namespace OpenDreamShared.Net.Packets {
    class PacketPrompt : IPacket {
        public PacketID PacketID => PacketID.Prompt;

        public int PromptId;
        public DMValueType Types;
        public string Message;

        public PacketPrompt() { }

        public PacketPrompt(int promptId, DMValueType types, string message) {
            PromptId = promptId;
            Types = types;
            Message = message;
        }

        public void ReadFromStream(PacketStream stream) {
            PromptId = stream.ReadUInt16();
            Types = (DMValueType)stream.ReadUInt16();
            Message = stream.ReadString();
        }

        public void WriteToStream(PacketStream stream) {
            stream.WriteUInt16((UInt16)PromptId);
            stream.WriteUInt16((UInt16)Types);
            stream.WriteString(Message);
        }
    }
}
