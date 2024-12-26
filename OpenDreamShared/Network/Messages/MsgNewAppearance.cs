using Lidgren.Network;
using OpenDreamShared.Dream;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace OpenDreamShared.Network.Messages;

public sealed class MsgNewAppearance: NetMessage {
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    public MsgNewAppearance() : this(new ImmutableAppearance(MutableAppearance.Default, null)) {}
    public MsgNewAppearance(ImmutableAppearance appearance) => Appearance = appearance;
    public ImmutableAppearance Appearance;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
        Appearance = new ImmutableAppearance(buffer, serializer);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
        Appearance.WriteToBuffer(buffer,serializer);
    }
}
