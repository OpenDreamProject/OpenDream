using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lidgren.Network;
using OpenDreamShared.Dream;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace OpenDreamShared.Network.Messages;

public sealed class MsgAllAppearances(Dictionary<int, ImmutableAppearance> allAppearances) : NetMessage {
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;
    public Dictionary<int, ImmutableAppearance> AllAppearances = allAppearances;
    public MsgAllAppearances() : this(new()) { }

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
        var count = buffer.ReadInt32();
        AllAppearances = new(count);

        for (int i = 0; i < count; i++) {
            var appearance = new ImmutableAppearance(buffer, serializer);
            AllAppearances.Add(appearance.GetHashCode(), appearance);
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
        buffer.Write(AllAppearances.Count);
        foreach (var pair in AllAppearances) {
            pair.Value.WriteToBuffer(buffer,serializer);
        }
    }
}
