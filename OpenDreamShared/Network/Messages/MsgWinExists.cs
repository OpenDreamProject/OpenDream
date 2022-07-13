using Lidgren.Network;
using Robust.Shared.Network;

namespace OpenDreamShared.Network.Messages {
    public sealed class MsgWinExists : NetMessage {
        public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

        public int PromptId;
        public string ControlId;

        public override void ReadFromBuffer(NetIncomingMessage buffer) {
            PromptId = buffer.ReadVariableInt32();
            ControlId = buffer.ReadString();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer) {
            buffer.WriteVariableInt32(PromptId);
            buffer.Write(ControlId);
        }
    }
}
