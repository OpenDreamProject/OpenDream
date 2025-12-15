using Lidgren.Network;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace OpenDreamShared.Network.Messages;

//Server -> Client: Tell the client about the current entity UIDs of its mob and eye
public sealed class MsgNotifyMobEyeUpdate : NetMessage {
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    public NetEntity MobNetEntity;
    public NetEntity EyeNetEntity;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
        MobNetEntity = new(buffer.ReadInt32());
        EyeNetEntity = new(buffer.ReadInt32());
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
        buffer.Write(MobNetEntity.Id);
        buffer.Write(EyeNetEntity.Id);
    }
}
