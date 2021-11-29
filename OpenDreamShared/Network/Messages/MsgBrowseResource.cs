using Lidgren.Network;
using Robust.Shared.Network;

namespace OpenDreamShared.Network.Messages
{
    public sealed class MsgBrowseResource : NetMessage
    {
        // TODO: Browse should be on its own channel or something.
        public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

        public string Filename;
        public byte[] Data;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            Filename = buffer.ReadString();
            var bytes = buffer.ReadVariableInt32();
            Data = buffer.ReadBytes(bytes);
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(Filename);
            buffer.WriteVariableInt32(Data.Length);
            buffer.Write(Data);
        }
    }
}
