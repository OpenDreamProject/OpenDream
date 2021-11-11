using Lidgren.Network;
using Robust.Shared.Network;

namespace OpenDreamShared.Network.Messages
{
    //Client -> Server: Tell the server what stat panel the client is now looking at
    //Server -> Client: Tell the client to switch stat panels
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
