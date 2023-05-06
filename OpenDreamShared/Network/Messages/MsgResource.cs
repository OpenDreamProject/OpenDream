using System;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace OpenDreamShared.Network.Messages;

public sealed class MsgResource : NetMessage {
    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableUnordered;

    public int ResourceId;
    public byte[] ResourceData = Array.Empty<byte>();

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
        ResourceId = buffer.ReadInt32();
        var dataLen = buffer.ReadVariableInt32();
        ResourceData = buffer.ReadBytes(dataLen);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
        buffer.Write(ResourceId);
        buffer.WriteVariableInt32(ResourceData.Length);
        buffer.Write(ResourceData);
    }
}
