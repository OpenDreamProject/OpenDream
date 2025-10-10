using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace OpenDreamShared.Network.Messages;

public sealed class MsgLookupResource : NetMessage {
    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableUnordered;

    public string ResourcePathOrRef = "";

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
        ResourcePathOrRef = buffer.ReadString();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
        buffer.Write(ResourcePathOrRef);
    }
}
