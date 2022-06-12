using Lidgren.Network;
using Robust.Shared.Network;

namespace OpenDreamShared.Network.Messages
{
    public sealed class MsgAckLoadInterface : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Core;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
        }
    }
}
