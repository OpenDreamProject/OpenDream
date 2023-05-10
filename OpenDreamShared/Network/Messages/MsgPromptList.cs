using System;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace OpenDreamShared.Network.Messages;

public sealed class MsgPromptList : NetMessage {
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    public int PromptId;
    public string Title = String.Empty;
    public string Message = String.Empty;
    public string DefaultValue = String.Empty;
    public bool CanCancel;
    public string[] Values = Array.Empty<string>();

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
        PromptId = buffer.ReadVariableInt32();
        Title = buffer.ReadString();
        Message = buffer.ReadString();
        DefaultValue = buffer.ReadString();
        CanCancel = buffer.ReadBoolean();

        Values = new string[buffer.ReadVariableInt32()];
        for (int i = 0; i < Values.Length; i++) {
            Values[i] = buffer.ReadString();
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
        buffer.WriteVariableInt32(PromptId);
        buffer.Write(Title);
        buffer.Write(Message);
        buffer.Write(DefaultValue);
        buffer.Write(CanCancel);

        buffer.WriteVariableInt32(Values.Length);
        foreach (string value in Values) {
            buffer.Write(value);
        }
    }
}
