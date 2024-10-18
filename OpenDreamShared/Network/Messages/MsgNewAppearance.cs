using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lidgren.Network;
using OpenDreamShared.Dream;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace OpenDreamShared.Network.Messages;

public sealed class MsgNewAppearance: NetMessage {
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    public MsgNewAppearance() : this(new ImmutableIconAppearance(MutableIconAppearance.Default, null)) {}
    public MsgNewAppearance(ImmutableIconAppearance appearance) => Appearance = appearance;
    public ImmutableIconAppearance Appearance;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
        Appearance = new ImmutableIconAppearance(buffer, serializer);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
        Appearance.WriteToBuffer(buffer,serializer);
    }
}
