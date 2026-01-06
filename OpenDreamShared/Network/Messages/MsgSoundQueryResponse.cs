using System.Collections.Generic;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace OpenDreamShared.Network.Messages;

public sealed class MsgSoundQueryResponse : NetMessage {
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    public int PromptId;
    public ushort SoundCount;
    public List<SoundData>? Sounds;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
        PromptId = buffer.ReadVariableInt32();
        SoundCount = buffer.ReadUInt16();

        if(SoundCount == 0) return;

        Sounds ??= new List<SoundData>(SoundCount);

        for (var i = 0; i < SoundCount; i++) {
            Sounds.Add(new SoundData(buffer));
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
        buffer.WriteVariableInt32(PromptId);
        if (SoundCount <= 0 || Sounds is null) {
            buffer.Write(0); // SoundCount = 0
            return;
        }

        buffer.Write(SoundCount);

        for (var i = 0; i < SoundCount; i++) {
            Sounds[i].WriteToBuffer(buffer);
        }
    }
}
