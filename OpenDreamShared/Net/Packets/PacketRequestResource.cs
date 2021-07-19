/*using System;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace OpenDreamShared.Net.Packets {
    [Serializable, NetSerializable]
    public class PacketRequestResource : NetMessage
    {
        public string ResourcePath { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            ResourcePath = buffer.ReadString();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(ResourcePath);
        }
    }
}
*/
