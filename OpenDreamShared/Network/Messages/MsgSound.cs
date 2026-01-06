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

        public SoundData SoundData;
        public int? ResourceId;
        public FormatType? Format; // TODO: This should probably be sent along with the sound resource instead somehow
        //TODO: Frequency and friends

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
            SoundData = new SoundData(buffer);

            if (buffer.ReadBoolean()) {
                ResourceId = buffer.ReadInt32();
                Format = (FormatType)buffer.ReadByte();
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
            SoundData.WriteToBuffer(buffer);

            buffer.Write(ResourceId != null);
            if (ResourceId != null) {
                buffer.Write(ResourceId.Value);

                if (Format == null)
                    throw new InvalidOperationException("Format cannot be null if there is a resource");
                buffer.Write((byte)Format);
            }
        }
    }

    public struct SoundData {
        public ushort Channel;
        public ushort Volume;
        public float Offset;

        public SoundData(NetIncomingMessage buffer) {
            ReadFromBuffer(buffer);
        }

        public void ReadFromBuffer(NetIncomingMessage buffer) {
            Channel = buffer.ReadUInt16();
            Volume = buffer.ReadUInt16();
            Offset = buffer.ReadFloat();
        }

        public void WriteToBuffer(NetOutgoingMessage buffer) {
            buffer.Write(Channel);
            buffer.Write(Volume);
            buffer.Write(Offset);
        }
    }
}
