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
        /// <summary>
        /// The DreamSoundChannel channel (out of 1024) that the sound is set to play on
        /// </summary>
        public ushort Channel;

        /// <summary>
        /// Volume as a percentage
        /// </summary>
        public ushort Volume;

        /// <summary>
        /// Current playback position in seconds
        /// </summary>
        public float Offset;

        /// <summary>
        /// Total playtime of the song in seconds, adjusted for frequency
        /// TODO: adjust for freq
        /// </summary>
        public float Length;

        /// <summary>
        /// Set to 0 to not repeat, 1 to repeat indefinitely, or 2 to repeat forwards and backwards
        /// TODO: Implement repeat=2
        /// </summary>
        public byte Repeat;

        /// <summary>
        /// Filepath to the resource, if present
        /// </summary>
        public string File = string.Empty;

        public SoundData(NetIncomingMessage buffer) {
            ReadFromBuffer(buffer);
        }

        private void ReadFromBuffer(NetIncomingMessage buffer) {
            Channel = buffer.ReadUInt16();
            Volume = buffer.ReadUInt16();
            Offset = buffer.ReadFloat();
            Length = buffer.ReadFloat();
            Repeat = buffer.ReadByte();
            File = buffer.ReadString();
        }

        public void WriteToBuffer(NetOutgoingMessage buffer) {
            buffer.Write(Channel);
            buffer.Write(Volume);
            buffer.Write(Offset);
            buffer.Write(Length);
            buffer.Write(Repeat);
            buffer.Write(File);
        }
    }
}
