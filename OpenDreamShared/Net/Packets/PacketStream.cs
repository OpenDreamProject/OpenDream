using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenDreamShared.Net.Packets {
    class PacketStream : MemoryStream {

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

        public Int16 ReadInt16() {
            return _binaryReader.ReadInt16();
        }

        public void WriteInt16(Int16 data) {
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

        public ScreenLocation ReadScreenLocation() {
            return new ScreenLocation(ReadSByte(), ReadSByte(), ReadSByte(), ReadSByte());
        }

        public void WriteScreenLocation(ScreenLocation screenLocation) {
            WriteSByte((sbyte)screenLocation.X);
            WriteSByte((sbyte)screenLocation.Y);
            WriteSByte((sbyte)screenLocation.PixelOffsetX);
            WriteSByte((sbyte)screenLocation.PixelOffsetY);
        }
    }
}
