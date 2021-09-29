using Lidgren.Network;
using Robust.Shared.Network;

namespace OpenDreamShared.Network.Messages
{
    public sealed class MsgTopic : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

        public string Query;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            Query = buffer.ReadString();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(Query);
        }
    }
}
