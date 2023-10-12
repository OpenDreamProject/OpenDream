using Lidgren.Network;
using OpenDreamShared.Dream;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace OpenDreamShared.Network.Messages;

/// <summary>
/// A NetMessage intended for information the client needs, is small, and infrequently changes
/// </summary>
public sealed class MsgUpdateClientInfo : NetMessage {
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    public ViewRange View;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
        View = new(buffer.ReadInt32(), buffer.ReadInt32());
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
        buffer.Write(View.Width);
        buffer.Write(View.Height);
    }
}
