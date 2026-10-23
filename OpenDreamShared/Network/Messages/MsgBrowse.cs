using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace OpenDreamShared.Network.Messages {
    public sealed class MsgBrowse : NetMessage {
        public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

        public bool Display;
        public string? Window;
        public string? FileName;
        public string? Options;
        public byte[]? Data;

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
            Display = buffer.ReadBoolean();
            var hasWindow = buffer.ReadBoolean();
            var hasFileName = buffer.ReadBoolean();
            var hasOptions = buffer.ReadBoolean();
            buffer.ReadPadBits();

            if (hasWindow)
                Window = buffer.ReadString();
            if (hasFileName)
                FileName = buffer.ReadString();
            if (hasOptions)
                Options = buffer.ReadString();

            var dataLength = buffer.ReadVariableInt32();
            if(dataLength > -1) {
                Data = buffer.ReadBytes(dataLength);
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
            buffer.Write(Display);
            buffer.Write(Window != null);
            buffer.Write(FileName != null);
            buffer.Write(Options != null);
            buffer.WritePadBits();

            if (Window != null)
                buffer.Write(Window);
            if (FileName != null)
                buffer.Write(FileName);
            if (Options != null)
                buffer.Write(Options);

            buffer.WriteVariableInt32(Data?.Length ?? -1);
            if(Data is not null) {
                buffer.Write(Data);
            }
        }
    }
}
