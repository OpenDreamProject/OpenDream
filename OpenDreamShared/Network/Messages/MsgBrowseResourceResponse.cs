using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace OpenDreamShared.Network.Messages;
public sealed class MsgBrowseResourceResponse : NetMessage {
    // TODO: Browse should be on its own channel or something.
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    public string Filename = string.Empty;
    public byte[] Data = [];

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
        Filename = buffer.ReadString();
        var bytes = buffer.ReadVariableInt32();
        Data = buffer.ReadBytes(bytes);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
        buffer.Write(Filename);
        buffer.WriteVariableInt32(Data.Length);
        buffer.Write(Data);
    }
}
