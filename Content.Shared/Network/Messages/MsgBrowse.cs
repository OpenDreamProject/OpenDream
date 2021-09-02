using Lidgren.Network;
using Robust.Shared.Maths;
using Robust.Shared.Network;

namespace Content.Shared.Network.Messages
{
    public sealed class MsgBrowse : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

        public string Window;
        public string HtmlSource;
        public Vector2i Size;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            var hasWindow = buffer.ReadBoolean();
            var hasHtml = buffer.ReadBoolean();
            buffer.ReadPadBits();

            if (hasWindow)
                Window = buffer.ReadString();
            if (hasHtml)
                HtmlSource = buffer.ReadString();

            Size = (buffer.ReadUInt16(), buffer.ReadUInt16());
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(Window != null);
            buffer.Write(HtmlSource != null);
            buffer.WritePadBits();

            if (Window != null)
                buffer.Write(Window);
            if (HtmlSource != null)
                buffer.Write(HtmlSource);

            buffer.Write((ushort) Size.X);
            buffer.Write((ushort) Size.Y);
        }
    }
}
