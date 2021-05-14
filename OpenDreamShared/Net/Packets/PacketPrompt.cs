using OpenDreamShared.Dream.Procs;
using System;

namespace OpenDreamShared.Net.Packets {
    class PacketPrompt : IPacket {
        public PacketID PacketID => PacketID.Prompt;

        public int PromptId;
        public DMValueType Types;
        public string Title;
        public string Message;
        public String DefaultValue;

        public PacketPrompt() { }

        public PacketPrompt(int promptId, DMValueType types, String title, String message, String defaultValue) {
            PromptId = promptId;
            Types = types;
            Title = title;
            Message = message;
            DefaultValue = defaultValue;
        }

        public void ReadFromStream(PacketStream stream) {
            PromptId = stream.ReadUInt16();
            Types = (DMValueType)stream.ReadUInt16();
            Title = stream.ReadString();
            Message = stream.ReadString();
            DefaultValue = stream.ReadString();
        }

        public void WriteToStream(PacketStream stream) {
            stream.WriteUInt16((UInt16)PromptId);
            stream.WriteUInt16((UInt16)Types);
            stream.WriteString(Title);
            stream.WriteString(Message);
            stream.WriteString(DefaultValue);
        }
    }
}
