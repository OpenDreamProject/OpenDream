using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace OpenDreamShared.Network.Messages;

public sealed class MsgBrowseResource : NetMessage {
    // TODO: Browse should be on its own channel or something.
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    public string Filename = string.Empty;
    public int DataHash;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
        Filename = buffer.ReadString();
        DataHash = buffer.ReadVariableInt32();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
        buffer.Write(Filename);
        buffer.WriteVariableInt32(DataHash);
    }
}
