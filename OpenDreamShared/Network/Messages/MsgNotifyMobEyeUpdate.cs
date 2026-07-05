using Lidgren.Network;
using OpenDreamShared.Dream;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System.IO;

namespace OpenDreamShared.Network.Messages;

//Server -> Client: Tell the client about the current entity UIDs of its mob and eye
public sealed class MsgNotifyMobEyeUpdate : NetMessage {
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    public NetEntity MobNetEntity;

    public ClientObjectReference EyeRef;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
        MobNetEntity = new(buffer.ReadInt32());

        var length = buffer.ReadVariableInt32();
        var memoryStream = new MemoryStream(length);
        buffer.ReadAlignedMemory(memoryStream, length);
        serializer.DeserializeDirect<ClientObjectReference>(memoryStream, out var res);
        EyeRef = res;
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
        buffer.Write(MobNetEntity.Id);

        var memoryStream = new MemoryStream();
        serializer.SerializeDirect(memoryStream, EyeRef);
        buffer.WriteVariableInt32((int)memoryStream.Length);
        buffer.Write(memoryStream.AsSpan());
    }
}
