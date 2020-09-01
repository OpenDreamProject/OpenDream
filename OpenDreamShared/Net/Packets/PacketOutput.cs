using System;

namespace OpenDreamShared.Net.Packets {
    class PacketOutput : IPacket {
        public enum PacketOutputType {
            String = 0x0,
            BrowseObject = 0x1,
            Sound = 0x2
        }

        public interface IPacketOutputValue {

        }

        public struct OutputString : IPacketOutputValue {
            public string Value;

            public OutputString(string value) {
                Value = value;
            }
        }

        public struct OutputSound : IPacketOutputValue {
            public UInt16 Channel;
            public string File;
            public int Volume;

            public OutputSound(UInt16 channel, string file, int volume) {
                Channel = channel;
                File = file;
                Volume = volume;
            }
        }

        public PacketID PacketID => PacketID.Output;
        public PacketOutputType ValueType;
        public IPacketOutputValue Value = null;

        public PacketOutput() { }

        public PacketOutput(string value) {
            ValueType = PacketOutputType.String;
            Value = new OutputString(value);
        }

        public PacketOutput(OutputSound sound) {
            ValueType = PacketOutputType.Sound;
            Value = sound;
        }

        public void ReadFromStream(PacketStream stream) {
            ValueType = (PacketOutputType)stream.ReadByte();

            if (ValueType == PacketOutputType.String) {
                Value = new OutputString(stream.ReadString());
            } else if (ValueType == PacketOutputType.Sound) {
                OutputSound soundValue = new OutputSound();
                bool hasFile = stream.ReadBool();

                soundValue.Channel = stream.ReadUInt16();
                if (hasFile) {
                    soundValue.File = stream.ReadString();
                    soundValue.Volume = stream.ReadByte();
                } else {
                    soundValue.File = null;
                    soundValue.Volume = 100;
                }
                
                Value = soundValue;
            }
        }

        public void WriteToStream(PacketStream stream) {
            stream.WriteByte((byte)ValueType);

            if (ValueType == PacketOutputType.String) {
                OutputString stringValue = (OutputString)Value;

                stream.WriteString(stringValue.Value);
            } else if (ValueType == PacketOutputType.Sound) {
                OutputSound soundValue = (OutputSound)Value;

                stream.WriteBool(soundValue.File != null);
                stream.WriteUInt16(soundValue.Channel);
                if (soundValue.File != null) {
                    stream.WriteString(soundValue.File);
                    stream.WriteByte((byte)soundValue.Volume);
                }
            }
        }
    }
}
