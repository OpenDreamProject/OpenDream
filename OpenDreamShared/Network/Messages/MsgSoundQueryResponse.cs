using System.Collections.Generic;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace OpenDreamShared.Network.Messages;

public sealed class MsgSoundQueryResponse : NetMessage {
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    public int PromptId;
    public List<SoundData>? Sounds;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
        PromptId = buffer.ReadVariableInt32();
        var soundCount = buffer.ReadUInt16();

        if(soundCount == 0) return;

        Sounds ??= new List<SoundData>(soundCount);

        for (var i = 0; i < soundCount; i++) {
            Sounds.Add(new SoundData(buffer));
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
        buffer.WriteVariableInt32(PromptId);

        var soundCount = Sounds?.Count ?? 0;
        buffer.Write((ushort)soundCount);

        for (var i = 0; i < soundCount; i++) {
            Sounds![i].WriteToBuffer(buffer);
        }
    }
}
