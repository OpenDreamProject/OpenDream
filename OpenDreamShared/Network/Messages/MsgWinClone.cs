using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace OpenDreamShared.Network.Messages;

public sealed class MsgWinClone : NetMessage {
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    public string ControlId;
    public string CloneId;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
        ControlId = buffer.ReadString();
        CloneId = buffer.ReadString();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
        buffer.Write(ControlId);
        buffer.Write(CloneId);
    }
}
