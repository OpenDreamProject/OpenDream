using Lidgren.Network;
using Robust.Shared.Network;

namespace OpenDreamShared.Network.Messages
{
    public sealed class MsgSound : NetMessage {
        public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

        public ushort Channel;
        public ushort Volume;
        public string File;
        //TODO: Frequency and friends

        public override void ReadFromBuffer(NetIncomingMessage buffer) {
            Channel = buffer.ReadUInt16();
            Volume = buffer.ReadUInt16();
            File = buffer.ReadString();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer) {
            buffer.Write(Channel);
            buffer.Write(Volume);
            buffer.Write(File);
        }
    }
}
