using Lidgren.Network;
using Robust.Shared.Network;

namespace Content.Shared.Network.Messages
{
    public sealed class MsgSelectStatPanel : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

        public string StatPanel;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            StatPanel = buffer.ReadString();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(StatPanel);
        }
    }
}
