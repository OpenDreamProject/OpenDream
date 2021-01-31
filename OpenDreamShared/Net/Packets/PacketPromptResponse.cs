using OpenDreamShared.Dream.Procs;
using System;

namespace OpenDreamShared.Net.Packets {
    class PacketPromptResponse : IPacket {
        public PacketID PacketID => PacketID.PromptResponse;

        public int PromptId;
        public DMValueType Type;
        public object Value;

        public PacketPromptResponse() { }

        public PacketPromptResponse(int promptId, DMValueType type, object value) {
            PromptId = promptId;
            Type = type;
            Value = value;
        }

        public void ReadFromStream(PacketStream stream) {
            PromptId = stream.ReadUInt16();
            
            Type = (DMValueType)stream.ReadUInt16();
            switch(Type) {
                case DMValueType.Null: Value = null; break;
                case DMValueType.Text: Value = stream.ReadString(); break;
                case DMValueType.Num: Value = stream.ReadInt32(); break;
                case DMValueType.Message: Value = stream.ReadString(); break;
                default: throw new Exception("Invalid prompt response type '" + Type + "'");
            }
        }

        public void WriteToStream(PacketStream stream) {
            stream.WriteUInt16((UInt16)PromptId);

            stream.WriteUInt16((UInt16)Type);
            switch (Type) {
                case DMValueType.Null: break;
                case DMValueType.Text: stream.WriteString((string)Value); break;
                case DMValueType.Num: stream.WriteInt32((Int32)Value); break;
                case DMValueType.Message: stream.WriteString((string)Value); break;
                default: throw new Exception("Invalid prompt response type '" + Type + "'");
            }
        }
    }
}
