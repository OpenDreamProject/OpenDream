using Lidgren.Network;
using Robust.Shared.Network;

namespace Content.Shared.Network.Messages
{
    public sealed class MsgOutput : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

        public string Control;
        public string Value;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            Value = buffer.ReadString();
            Control = buffer.ReadString();
            if (Control == string.Empty)
                Control = null;
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(Value);
            buffer.Write(Control ?? string.Empty);
        }
    }
}
