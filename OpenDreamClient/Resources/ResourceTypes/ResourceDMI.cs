using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Text;
using OpenDreamShared.Resources;

namespace OpenDreamClient.Resources.ResourceTypes {
    class ResourceDMI : Resource {
        public Bitmap ImageBitmap;
        public DMIParser.ParsedDMIDescription Description;

        private readonly byte[] _pngHeader = new byte[8] { 0x89, 0x50, 0x4E, 0x47, 0xD, 0xA, 0x1A, 0xA };

        public ResourceDMI(string resourcePath, byte[] data) : base(resourcePath, data) {
            if (!IsValidPNG()) throw new Exception("Attempted to create a DMI using an invalid PNG");

            ImageBitmap = (Bitmap)Image.FromStream(new MemoryStream(data));
            Description = ParseDMI();
        }

        ~ResourceDMI() {
            ImageBitmap.Dispose();
        }

        private bool IsValidPNG() {
            if (Data.Length < _pngHeader.Length) return false;

            for (int i=0; i<_pngHeader.Length; i++) {
                if (Data[i] != _pngHeader[i]) return false;
            }

            return true;
        }

        private string ReadDMIDescription() {
            MemoryStream stream = new MemoryStream(Data);
            BinaryReader reader = new BinaryReader(stream);

            stream.Seek(8, SeekOrigin.Begin);
            while (stream.Position < stream.Length) {
                byte[] chunkLengthBytes = reader.ReadBytes(4);
                Array.Reverse(chunkLengthBytes); //Little to Big-Endian
                UInt32 chunkLength = BitConverter.ToUInt32(chunkLengthBytes);
                string chunkType = new string(reader.ReadChars(4));

                if (chunkType != "zTXt") {
                    stream.Seek(chunkLength + 4, SeekOrigin.Current);
                } else {
                    long chunkDataPosition = stream.Position;
                    string keyword = String.Empty + reader.ReadChar();

                    while (reader.PeekChar() != 0 && keyword.Length < 79) {
                        keyword += reader.ReadChar();
                    }
                    stream.Seek(2, SeekOrigin.Current); //Skip over null-terminator and compression method

                    if (keyword == "Description") {
                        stream.Seek(2, SeekOrigin.Current); //Skip the first 2 bytes in the zlib format

                        DeflateStream deflateStream = new DeflateStream(stream, CompressionMode.Decompress);
                        MemoryStream uncompressedDataStream = new MemoryStream();

                        deflateStream.CopyTo(uncompressedDataStream, (int)chunkLength - keyword.Length - 2);

                        byte[] uncompressedData = new byte[uncompressedDataStream.Length];
                        uncompressedDataStream.Seek(0, SeekOrigin.Begin);
                        uncompressedDataStream.Read(uncompressedData);

                        return Encoding.UTF8.GetString(uncompressedData, 0, uncompressedData.Length); ;
                    } else {
                        stream.Position = chunkDataPosition + chunkLength + 4;
                    }
                }
            }

            return null;
        }

        private DMIParser.ParsedDMIDescription ParseDMI() {
            string dmiDescription = ReadDMIDescription();

            try {
                return DMIParser.ParseDMIDescription(dmiDescription, ImageBitmap.Width);
            } catch (Exception e) {
            Console.WriteLine(dmiDescription);
                Console.WriteLine("Error while parsing dmi '" + ResourcePath + "': " + e.Message);
            }

            return null;
        }
    }
}
