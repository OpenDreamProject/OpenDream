using Lidgren.Network;
using Robust.Shared.Serialization;

namespace OpenDreamShared.Dream;

public interface IBufferableAppearance {
    public int ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer);
    public void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer);
}
