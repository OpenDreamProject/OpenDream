using Lidgren.Network;
using OpenDreamShared.Dream.Procs;
using Robust.Shared.Network;

namespace OpenDreamShared.Network.Messages;

public sealed class MsgPromptList : NetMessage {
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    public int PromptId;
    public string Title;
    public string Message;
    public string DefaultValue;
    public bool CanCancel;
    public string[] Values;

    public override void ReadFromBuffer(NetIncomingMessage buffer) {
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

    public override void WriteToBuffer(NetOutgoingMessage buffer) {
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
