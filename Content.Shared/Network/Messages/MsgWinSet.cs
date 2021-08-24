using Lidgren.Network;
using Robust.Shared.Network;

namespace Content.Shared.Network.Messages
{
    public sealed class MsgWinSet : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

        public string ControlId;
        public string Params;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            ControlId = buffer.ReadString();
            Params = buffer.ReadString();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(ControlId);
            buffer.Write(Params);
        }
    }
}
