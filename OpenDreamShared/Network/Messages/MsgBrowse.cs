using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace OpenDreamShared.Network.Messages {
    public sealed class MsgBrowse : NetMessage {
        public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

        public string? Window;
        public string? HtmlSource;
        public string? Options;

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
            var hasWindow = buffer.ReadBoolean();
            var hasHtml = buffer.ReadBoolean();
            var hasOptions = buffer.ReadBoolean();
            buffer.ReadPadBits();

            if (hasWindow)
                Window = buffer.ReadString();
            if (hasHtml)
                HtmlSource = buffer.ReadString();
            if (hasOptions)
                Options = buffer.ReadString();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
            buffer.Write(Window != null);
            buffer.Write(HtmlSource != null);
            buffer.Write(Options != null);
            buffer.WritePadBits();

            if (Window != null)
                buffer.Write(Window);
            if (HtmlSource != null)
                buffer.Write(HtmlSource);
            if (Options != null)
                buffer.Write(Options);
        }
    }
}
