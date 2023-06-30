using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace OpenDreamShared.Network.Messages;

public sealed class MsgFtp : NetMessage {
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    public int ResourceId;
    public string SuggestedName = string.Empty;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
        ResourceId = buffer.ReadInt32();
        SuggestedName = buffer.ReadString();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
        buffer.Write(ResourceId);
        buffer.Write(SuggestedName);
    }
}
