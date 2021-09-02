using Lidgren.Network;
using Robust.Shared.Network;

namespace Content.Shared.Network.Messages
{
    /// <summary>
    /// Sent server -> client to tell the client to load the interface after connecting, before going in-game.
    /// </summary>
    public class MsgLoadInterface : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Core;

        public string InterfaceText;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            InterfaceText = buffer.ReadString();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(InterfaceText);
        }
    }
}
