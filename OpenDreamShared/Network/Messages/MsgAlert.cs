using System;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace OpenDreamShared.Network.Messages {
    public sealed class MsgAlert : NetMessage {
        public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

        public int PromptId;
        public string Title = String.Empty;
        public string Message = String.Empty;
        public string Button1 = String.Empty;
        public string Button2 = String.Empty;
        public string Button3 = String.Empty;

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
            PromptId = buffer.ReadVariableInt32();
            Title = buffer.ReadString();
            Message = buffer.ReadString();
            Button1 = buffer.ReadString();
            Button2 = buffer.ReadString();
            Button3 = buffer.ReadString();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
            buffer.WriteVariableInt32(PromptId);
            buffer.Write(Title);
            buffer.Write(Message);
            buffer.Write(Button1);
            buffer.Write(Button2);
            buffer.Write(Button3);
        }
    }
}
