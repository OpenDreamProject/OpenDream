using Lidgren.Network;
using Robust.Shared.Network;

namespace OpenDreamShared.Network.Messages
{
    public sealed class MsgRequestResource : NetMessage
    {
        public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableUnordered;

        public string ResourcePath;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            ResourcePath = buffer.ReadString();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(ResourcePath);
        }
    }
}
