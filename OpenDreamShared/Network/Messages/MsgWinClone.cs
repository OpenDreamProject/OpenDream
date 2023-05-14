using System;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace OpenDreamShared.Network.Messages;

public sealed class MsgWinClone : NetMessage {
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    public string ControlId = String.Empty;
    public string CloneId = String.Empty;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
        ControlId = buffer.ReadString();
        CloneId = buffer.ReadString();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
        buffer.Write(ControlId);
        buffer.Write(CloneId);
    }
}
