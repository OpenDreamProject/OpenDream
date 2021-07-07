using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using OpenDreamShared.Dream;
using OpenDreamShared.Resources;
using Robust.Client.Graphics;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace OpenDreamClient.Resources.ResourceTypes {
    public class ResourceDMI : Resource {
        public Texture Texture { get; }
        public DMIParser.ParsedDMIDescription Description { get; }

        private static readonly byte[] PngHeader = { 0x89, 0x50, 0x4E, 0x47, 0xD, 0xA, 0x1A, 0xA };

        public ResourceDMI(string resourcePath, byte[] data) : base(resourcePath, data) {
            if (!IsValidPNG()) throw new Exception("Attempted to create a DMI using an invalid PNG");

            Texture = IoCManager.Resolve<IClyde>().LoadTextureFromPNGStream(new MemoryStream(data));
            Description = ParseDMI();
        }

        public UIBox2 GetTextureRect(string stateName, AtomDirection direction = AtomDirection.South, int animationFrame = 0) {
            DMIParser.ParsedDMIState state = Description.GetState(stateName);
            DMIParser.ParsedDMIFrame frame = state.GetFrames(direction)[animationFrame];

            return UIBox2.FromDimensions(new Vector2(frame.X, frame.Y), new Vector2(Description.Width, Description.Height));
        }

        private bool IsValidPNG() {
            if (Data.Length < PngHeader.Length) return false;

            for (int i=0; i<PngHeader.Length; i++) {
                if (Data[i] != PngHeader[i]) return false;
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

                        return Encoding.UTF8.GetString(uncompressedData, 0, uncompressedData.Length);
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
                return DMIParser.ParseDMIDescription(dmiDescription, Texture.Width);
            } catch (Exception e) {
                Console.WriteLine("Error while parsing dmi '" + ResourcePath + "': " + e.Message);
            }

            return null;
        }
    }
}
