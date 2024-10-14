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
    public MsgNewAppearance() : this(MutableIconAppearance.Default) { }
    public MsgNewAppearance(IBufferableAppearance appearance) => Appearance = appearance;
    public IBufferableAppearance Appearance;
    public int AppearanceId;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
        Appearance = new MutableIconAppearance();
        AppearanceId = Appearance.ReadFromBuffer(buffer, serializer);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
        AppearanceId = Appearance.GetHashCode();
        Appearance.WriteToBuffer(buffer,serializer);
    }
}
