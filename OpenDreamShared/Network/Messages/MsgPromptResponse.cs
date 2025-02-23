using System;
using Lidgren.Network;
using OpenDreamShared.Common.DM;
using OpenDreamShared.Dream;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace OpenDreamShared.Network.Messages;

public sealed class MsgPromptResponse : NetMessage {
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    public int PromptId;
    public DMValueType Type;
    public object? Value;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
        PromptId = buffer.ReadVariableInt32();
        Type = (DMValueType)buffer.ReadUInt16();

        Value = Type switch {
            DMValueType.Null => null,
            DMValueType.Text or DMValueType.Message => buffer.ReadString(),
            DMValueType.Num => buffer.ReadSingle(),
            DMValueType.Color => new Color(buffer.ReadByte(), buffer.ReadByte(), buffer.ReadByte(), buffer.ReadByte()),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
        buffer.WriteVariableInt32(PromptId);

        buffer.Write((ushort)Type);
        switch (Type) {
            case DMValueType.Null: break;
            case DMValueType.Text or DMValueType.Message:
                buffer.Write((string)Value!);
                break;
            case DMValueType.Num:
                buffer.Write((float)Value!);
                break;
            case DMValueType.Color:
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
