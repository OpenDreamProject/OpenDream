﻿using System.Collections.Generic;
using Lidgren.Network;
using OpenDreamShared.Dream;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace OpenDreamShared.Network.Messages;

public sealed class MsgAllAppearances(Dictionary<uint, ImmutableAppearance> allAppearances) : NetMessage {
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;
    public Dictionary<uint, ImmutableAppearance> AllAppearances = allAppearances;

    public MsgAllAppearances() : this(new()) { }

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
        var count = buffer.ReadInt32();
        AllAppearances = new(count);

        for (int i = 0; i < count; i++) {
            var appearance = new ImmutableAppearance(buffer, serializer);
            AllAppearances.Add(appearance.MustGetId(), appearance);
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
        buffer.Write(AllAppearances.Count);
        foreach (var pair in AllAppearances) {
            pair.Value.WriteToBuffer(buffer,serializer);
        }
    }
}
