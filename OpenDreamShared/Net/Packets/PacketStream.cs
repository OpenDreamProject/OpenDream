using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenDreamShared.Net.Packets {
    class PacketStream : MemoryStream {
        public enum PacketStreamVisualPropertyID {
            Icon = 0x0,
            IconState = 0x1,
            Color = 0x2,
            Direction = 0x3,
            Layer = 0x4,
            End = 0x5
        }

        private BinaryWriter _binaryWriter;
        private BinaryReader _binaryReader;

        public PacketStream() : base() {
            _binaryWriter = new BinaryWriter(this);
            _binaryReader = new BinaryReader(this);
        }

        public PacketStream(byte[] buffer) : base(buffer) {
            _binaryWriter = new BinaryWriter(this);
            _binaryReader = new BinaryReader(this);
        }

        public sbyte ReadSByte() {
            return _binaryReader.ReadSByte();
        }

        public void WriteSByte(sbyte data) {
            _binaryWriter.Write(data);
        }

        public UInt16 ReadUInt16() {
            return _binaryReader.ReadUInt16();
        }

        public void WriteUInt16(UInt16 data) {
            _binaryWriter.Write(data);
        }

        public UInt32 ReadUInt32() {
            return _binaryReader.ReadUInt32();
        }

        public void WriteUInt32(UInt32 data) {
            _binaryWriter.Write(data);
        }

        public string ReadString() {
            string data = String.Empty;

            while (_binaryReader.PeekChar() != 0 && this.Position < this.Length) {
                data += _binaryReader.ReadChar();
            }
            _binaryReader.ReadChar();

            return data;
        }

        public void WriteString(string data) {
            byte[] bytes = Encoding.ASCII.GetBytes(data + '\0');

            this.Write(bytes);
        }

        public bool ReadBool() {
            return (this.ReadByte() != 0);
        }

        public void WriteBool(bool data) {
            this.WriteByte(data ? (byte)0x1 : (byte)0x0);
        }

        public float ReadFloat() {
            return _binaryReader.ReadSingle();
        }

        public void WriteFloat(float data) {
            _binaryWriter.Write(data);
        }

        public void WriteBuffer(byte[] data) {
            _binaryWriter.Write((UInt32)data.Length);
            this.Write(data);
        }

        public byte[] ReadBuffer() {
            UInt32 length = _binaryReader.ReadUInt32();
            byte[] buffer = new byte[length];

            this.Read(buffer, 0, (int)length);
            return buffer;
        }

        public IconVisualProperties ReadIconVisualProperties() {
            IconVisualProperties visualProperties = new IconVisualProperties();
            PacketStreamVisualPropertyID propertyID = (PacketStreamVisualPropertyID)ReadByte();

            while (propertyID != PacketStreamVisualPropertyID.End) {
                if (propertyID == PacketStreamVisualPropertyID.IconState) {
                    visualProperties.IconState = ReadString();
                } else if (propertyID == PacketStreamVisualPropertyID.Icon) {
                    visualProperties.Icon = ReadString();
                } else if (propertyID == PacketStreamVisualPropertyID.Color) {
                    visualProperties.Color = ReadUInt32();
                } else if (propertyID == PacketStreamVisualPropertyID.Direction) {
                    visualProperties.Direction = (AtomDirection)ReadByte();
                } else if (propertyID == PacketStreamVisualPropertyID.Layer) {
                    visualProperties.Layer = ReadFloat();
                } else if (propertyID != PacketStreamVisualPropertyID.End) {
                    throw new Exception("Invalid visual property ID (" + propertyID.ToString() + ")");
                }

                propertyID = (PacketStreamVisualPropertyID)ReadByte();
            }

            return visualProperties;
        }

        public void WriteIconVisualProperties(IconVisualProperties visualProperties, IconVisualProperties? defaultVisualProperties = null) {
            if (visualProperties.Icon != default && visualProperties.Icon != defaultVisualProperties?.Icon) {
                WriteByte((byte)PacketStreamVisualPropertyID.Icon);
                WriteString(visualProperties.Icon);
            }

            if (visualProperties.IconState != default && visualProperties.IconState != defaultVisualProperties?.IconState) {
                WriteByte((byte)PacketStreamVisualPropertyID.IconState);
                WriteString(visualProperties.IconState);
            }

            if (visualProperties.Color != default && visualProperties.Color != defaultVisualProperties?.Color) {
                WriteByte((byte)PacketStreamVisualPropertyID.Color);
                WriteUInt32(visualProperties.Color);
            }

            if (visualProperties.Direction != default && visualProperties.Direction != defaultVisualProperties?.Direction) {
                WriteByte((byte)PacketStreamVisualPropertyID.Direction);
                WriteByte((byte)visualProperties.Direction);
            }

            if (visualProperties.Layer != default && visualProperties.Layer != defaultVisualProperties?.Layer) {
                WriteByte((byte)PacketStreamVisualPropertyID.Layer);
                WriteFloat(visualProperties.Layer);
            }

            WriteByte((byte)PacketStreamVisualPropertyID.End);
        }

        public ScreenLocation ReadScreenLocation() {
            return new ScreenLocation(ReadSByte(), ReadSByte(), ReadSByte(), ReadSByte());
        }

        public void WriteScreenLocation(ScreenLocation screenLocation) {
            WriteSByte((sbyte)screenLocation.X);
            WriteSByte((sbyte)screenLocation.Y);
            WriteSByte((sbyte)screenLocation.PixelOffsetX);
            WriteSByte((sbyte)screenLocation.PixelOffsetY);
        }

        public Dictionary<UInt16, IconVisualProperties> ReadOverlays() {
            Dictionary<UInt16, IconVisualProperties> overlays = new Dictionary<UInt16, IconVisualProperties>();
            int overlayCount = ReadByte();

            for (int i = 0; i < overlayCount; i++) {
                IconVisualProperties overlayVisualProperties = new IconVisualProperties();
                UInt16 overlayID = (UInt16)ReadByte();

                overlayVisualProperties.Icon = ReadString();
                overlayVisualProperties.IconState = ReadString();
                overlayVisualProperties.Direction = (AtomDirection)ReadByte();
                overlayVisualProperties.Color = ReadUInt32();
                overlayVisualProperties.Layer = ReadFloat();
                overlays[overlayID] = overlayVisualProperties;
            }

            return overlays;
        }

        public void WriteOverlays(Dictionary<UInt16, IconVisualProperties> overlays) {
            if (overlays != null) {
                WriteByte((byte)overlays.Count);

                foreach (KeyValuePair<UInt16, IconVisualProperties> overlay in overlays) {
                    WriteByte((byte)overlay.Key);

                    WriteString(overlay.Value.Icon);
                    WriteString(overlay.Value.IconState);
                    WriteByte((byte)overlay.Value.Direction);
                    WriteUInt32(overlay.Value.Color);
                    WriteFloat(overlay.Value.Layer);
                }
            } else {
                WriteByte(0);
            }
        }
    }
}
