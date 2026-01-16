using Lidgren.Network;
using OpenDreamShared.Dream;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using static OpenDreamShared.Dream.ClientObjectReference;

namespace OpenDreamShared.Network.Messages;

//Server -> Client: Tell the client about the current entity UIDs of its mob and eye
public sealed class MsgNotifyMobEyeUpdate : NetMessage {
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    public NetEntity MobNetEntity;
    // Type = Client -> null
    public ClientObjectReference EyeRef;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
        MobNetEntity = new(buffer.ReadInt32());

        RefType eyeType = (RefType)buffer.ReadInt32();
        switch (eyeType) {
            case RefType.Entity:
                EyeRef = new(new NetEntity(buffer.ReadInt32()));
                break;
            case RefType.Turf:
                int x = buffer.ReadInt32();
                int y = buffer.ReadInt32();
                int z = buffer.ReadInt32();
                EyeRef = new(new(x, y), z);
                break;
            default:
                // null eye
                EyeRef = new();
                break;
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
        buffer.Write(MobNetEntity.Id);

        buffer.Write((int)EyeRef.Type);
        switch (EyeRef.Type) {
            case RefType.Entity:
                buffer.Write(EyeRef.Entity.Id);
                break;
            case RefType.Turf:
                buffer.Write(EyeRef.TurfX);
                buffer.Write(EyeRef.TurfY);
                buffer.Write(EyeRef.TurfZ);
                break;
        }
    }
}
