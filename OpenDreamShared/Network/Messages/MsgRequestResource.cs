using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace OpenDreamShared.Network.Messages
{
    public sealed class MsgRequestResource : NetMessage
    {
        public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableUnordered;

        public string ResourcePath;

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            ResourcePath = buffer.ReadString();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
            buffer.Write(ResourcePath);
        }
    }
}
