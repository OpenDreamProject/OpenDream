using Lidgren.Network;
using OpenDreamShared.Dream.Procs;
using Robust.Shared.Network;

namespace OpenDreamShared.Network.Messages
{
    public sealed class MsgPrompt : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

        public int PromptId;
        public DMValueType Types;
        public string Title;
        public string Message;
        public string DefaultValue;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            PromptId = buffer.ReadVariableInt32();
            Types = (DMValueType)buffer.ReadUInt16();
            Title = buffer.ReadString();
            Message = buffer.ReadString();
            DefaultValue = buffer.ReadString();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.WriteVariableInt32(PromptId);
            buffer.Write((ushort) Types);
            buffer.Write(Title);
            buffer.Write(Message);
            buffer.Write(DefaultValue);
        }
    }
}
