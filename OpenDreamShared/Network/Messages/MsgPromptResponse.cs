using System;
using Lidgren.Network;
using OpenDreamShared.Dream;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace OpenDreamShared.Network.Messages;

public sealed class MsgPromptResponse : NetMessage {
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    public int PromptId;
    public DreamValueType Type;
    public object? Value;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
        PromptId = buffer.ReadVariableInt32();
        Type = (DreamValueType)buffer.ReadUInt16();

        Value = Type switch {
            DreamValueType.Null => null,
            DreamValueType.Text or DreamValueType.Message => buffer.ReadString(),
            DreamValueType.Num => buffer.ReadSingle(),
            DreamValueType.Color => new Color(buffer.ReadByte(), buffer.ReadByte(), buffer.ReadByte(), buffer.ReadByte()),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
        buffer.WriteVariableInt32(PromptId);

        buffer.Write((ushort)Type);
        switch (Type) {
            case DreamValueType.Null: break;
            case DreamValueType.Text or DreamValueType.Message:
                buffer.Write((string)Value!);
                break;
            case DreamValueType.Num:
                buffer.Write((float)Value!);
                break;
            case DreamValueType.Color:
                var color = (Color)Value!;
                buffer.Write(color.RByte);
                buffer.Write(color.GByte);
                buffer.Write(color.BByte);
                buffer.Write(color.AByte);
                break;
            default: throw new Exception("Invalid prompt response type '" + Type + "'");
        }
    }
}
