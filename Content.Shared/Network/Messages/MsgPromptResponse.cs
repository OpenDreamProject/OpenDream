using System;
using Content.Shared.Dream.Procs;
using Lidgren.Network;
using Robust.Shared.Network;

namespace Content.Shared.Network.Messages
{
    public sealed class MsgPromptResponse : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

        public int PromptId;
        public DMValueType Type;
        public object Value;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            PromptId = buffer.ReadVariableInt32();
            Type = (DMValueType)buffer.ReadUInt16();

            Value = Type switch
            {
                DMValueType.Null => null,
                DMValueType.Text or DMValueType.Message => buffer.ReadString(),
                DMValueType.Num => buffer.ReadSingle(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.WriteVariableInt32(PromptId);

            buffer.Write((ushort)Type);
            switch (Type) {
                case DMValueType.Null: break;
                case DMValueType.Text or DMValueType.Message: buffer.Write((string)Value); break;
                case DMValueType.Num: buffer.Write((float)Value); break;
                default: throw new Exception("Invalid prompt response type '" + Type + "'");
            }
        }
    }
}
