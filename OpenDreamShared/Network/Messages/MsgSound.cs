using System;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace OpenDreamShared.Network.Messages {
    public sealed class MsgSound : NetMessage {
        public enum FormatType : byte {
            Ogg,
            Wav
        }

        public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

        public ushort Channel;
        public ushort Volume;
        public int Offset;
        public int? ResourceId;
        public FormatType? Format; // TODO: This should probably be sent along with the sound resource instead somehow
        //TODO: Frequency and friends

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
            Channel = buffer.ReadUInt16();
            Volume = buffer.ReadUInt16();
            Offset = buffer.ReadInt32();

            if (buffer.ReadBoolean()) {
                ResourceId = buffer.ReadInt32();
                Format = (FormatType)buffer.ReadByte();
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
            buffer.Write(Channel);
            buffer.Write(Volume);
            buffer.Write(Offset);

            buffer.Write(ResourceId != null);
            if (ResourceId != null) {
                buffer.Write(ResourceId.Value);

                if (Format == null)
                    throw new InvalidOperationException("Format cannot be null if there is a resource");
                buffer.Write((byte)Format);
            }
        }
    }
}
