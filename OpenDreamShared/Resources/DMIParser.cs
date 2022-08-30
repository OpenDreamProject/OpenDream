using Robust.Shared.Maths;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using OpenDreamShared.Dream;
using System.Globalization;

namespace OpenDreamShared.Resources {
    public static class DMIParser {
        /// <summary>This is the specific order that directional icon substates appear in within a DMI. </summary>
        private static readonly AtomDirection[] DMIDirectionOrder = new AtomDirection[]
        {
            AtomDirection.South,
            AtomDirection.North,
            AtomDirection.East,
            AtomDirection.West,
            AtomDirection.Southeast,
            AtomDirection.Southwest,
            AtomDirection.Northeast,
            AtomDirection.Northwest
        };
        private static readonly byte[] PNGHeader = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0xD, 0xA, 0x1A, 0xA };

        /// <summary>
        /// This is what BYOND declares to be the "current version" of the DMI file format.
        /// To keep parity, we should mimic this number.
        /// </summary>
        private const float CURRENT_DMI_VERSION = 4f;


        /// <summary>
        /// This data structure contains the metadata of a DMI or /icon.<br/>
        /// It is held separate from DMIResource because /icon manipulations on a DMI may cause the underlying graphics to copy-on-write, but not the metadata.<br/>
        /// Vice versa may occur in other instances, like with /icon.Insert()
        /// </summary>
        public sealed class DMIDescription {
            public string Source;
            public float Version;
            /// <summary>
            /// The height and width of each individual icon frame.<br/>
            /// X is width, Y is height. Held in this format because DMIResource likes it this way
            /// </summary>
            public Vector2i Dimensions; 
            public Dictionary<string, ParsedDMIState> States;

            public static DMIDescription CreateEmpty(int width, int height) {
                ParsedDMIFrame[] frames = { new() };
                ParsedDMIState state = new();
                state.Directions[(int)AtomDirection.South] = frames;

                return new DMIDescription() {
                    Source = null,
                    Version = CURRENT_DMI_VERSION,
                    Dimensions = { X= width, Y = height },
                    States = new() {
                        { "", state }
                    }
                };
            }

            public bool HasState(string stateName = null) {
                return States.ContainsKey(stateName ?? "");
            }

            public ParsedDMIState GetState(string stateName = null) {
                States.TryGetValue(stateName ?? "", out var state);

                return state;
            }
        }

        public sealed class ParsedDMIState {
            public string Name;
            /// <remarks>
            /// Stores specifically 12 because 11 is the maximum possible value of <see cref="AtomDirection"/>.<br/>
            /// This means that we can index into this array with the AtomDirection directly, without indirecting through a Dictionary or something,<br/>
            /// with probably equivalent memory overhead (hashtables are greedy about memory!) <br/>
            /// Might be a micro-optimization but note that this also probably improves performance of deep-copying this ParsedDMIState during /icon ops.
            /// </remarks>
            public ParsedDMIFrame[][] Directions = new ParsedDMIFrame[12][];
            public bool Loop = true;
            public bool Rewind = false;

            public ParsedDMIFrame[] GetFrames(AtomDirection direction = AtomDirection.South) {
                ParsedDMIFrame[]? frames = Directions[(int)direction];
                if (frames != null)
                    return frames;
                return Directions[(int)AtomDirection.South];
            }

            /// <summary>A smart iterator over Directions that better handles the fact that it's a sparsely-populated array. </summary>
            public IEnumerable<KeyValuePair<AtomDirection, ParsedDMIFrame[]>> IterateDirections()
            {
                foreach(AtomDirection dir in DMIDirectionOrder) // Using this instead of Enum.GetValues to implicitly skip over AtomDirection.None
                {
                    ParsedDMIFrame[]? frames = Directions[(int)dir];
                    if (frames == null)
                        continue;
                    yield return new KeyValuePair<AtomDirection, ParsedDMIFrame[]>(dir, frames);
                }
            }
        }

        public sealed class ParsedDMIFrame {
            public int X, Y;
            public float Delay;
        }

        public static DMIDescription ParseDMI(Stream stream) {
            if (!VerifyPNG(stream)) throw new Exception("Provided stream was not a valid PNG");

            BinaryReader reader = new BinaryReader(stream);
            Vector2u? imageSize = null;

            while (stream.Position < stream.Length) {
                uint chunkLength = ReadBigEndianUint32(reader);
                string chunkType = new string(reader.ReadChars(4));

                switch (chunkType) {
                    case "IHDR": //Image header, contains the image size
                        imageSize = new Vector2u(ReadBigEndianUint32(reader), ReadBigEndianUint32(reader));
                        stream.Seek(chunkLength - 4, SeekOrigin.Current); //Skip the rest of the chunk
                        break;
                    case "zTXt": //Compressed text, likely contains our DMI description
                        if (imageSize == null) throw new Exception("The PNG did not contain an IHDR chunk");

                        long chunkDataPosition = stream.Position;
                        string keyword = char.ToString(reader.ReadChar());

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

                            string dmiDescription = Encoding.UTF8.GetString(uncompressedData, 0, uncompressedData.Length);
                            return ParseDMIDescription(dmiDescription, imageSize.Value.X);
                        } else {
                            stream.Position = chunkDataPosition + chunkLength + 4;
                        }
                        break;
                    default: //Nothing we care about, skip it
                        stream.Seek(chunkLength + 4, SeekOrigin.Current);
                        break;
                }
            }

            throw new Exception("Could not find a DMI description");
        }

        public static DMIDescription ParseDMIDescription(string dmiDescription, uint imageWidth) {
            DMIDescription description = new DMIDescription();
            ParsedDMIState currentState = null;
            int currentFrameX = 0;
            int currentFrameY = 0;
            int currentStateDirectionCount = 1;
            int currentStateFrameCount = 1;
            float[] currentStateFrameDelays = null;

            description.States = new Dictionary<string, ParsedDMIState>();

            dmiDescription = dmiDescription.Replace("# BEGIN DMI", "");
            dmiDescription = dmiDescription.Replace("# END DMI", "");
            dmiDescription = dmiDescription.Replace("\t", "");
            dmiDescription = dmiDescription.Trim();
            description.Source = dmiDescription;

            string[] lines = dmiDescription.Split("\n");
            foreach (string line in lines) {
                int equalsIndex = line.IndexOf('=');

                if (equalsIndex != -1) {
                    string key = line.Substring(0, equalsIndex).Trim();
                    string value = line.Substring(equalsIndex + 1).Trim();

                    switch (key) {
                        case "version":
                            description.Version = float.Parse(value, CultureInfo.InvariantCulture);
                            break;
                        case "width":
                            description.Dimensions.X = int.Parse(value);
                            break;
                        case "height":
                            description.Dimensions.Y = int.Parse(value);
                            break;
                        case "state":
                            string stateName = ParseString(value);

                            if (currentState != null) { // If we already were working on a state, that means it's done and we should write its metadata to the description
                                for(int d = 0; d < currentStateDirectionCount; d++)// For each direction in this icon_state
                                {
                                    ParsedDMIFrame[] frames = new ParsedDMIFrame[currentStateFrameCount];
                                    AtomDirection direction = DMIDirectionOrder[d];
                                    currentState.Directions[(int)direction] = frames;
                                    for (int f = 0; f < currentStateFrameCount; f++)// For each frame in this direction
                                    {
                                        ParsedDMIFrame frame = new ParsedDMIFrame();

                                        frame.X = currentFrameX;
                                        frame.Y = currentFrameY;
                                        frame.Delay = (currentStateFrameDelays != null) ? currentStateFrameDelays[f] : 1;
                                        frame.Delay *= 100; //Convert from deciseconds to milliseconds
                                        currentState.Directions[(int)direction][f] = frame;

                                        currentFrameX += description.Dimensions.Y;
                                        if (currentFrameX >= imageWidth)
                                        {
                                            currentFrameY += description.Dimensions.X;
                                            currentFrameX = 0;
                                        }
                                    }
                                }
                            }

                            currentStateFrameCount = 1;
                            currentStateFrameDelays = null;

                            currentState = new ParsedDMIState();
                            currentState.Name = stateName;
                            if (!description.States.ContainsKey(stateName)) description.States.Add(stateName, currentState);

                            break;
                        case "dirs":
                            currentStateDirectionCount = int.Parse(value);
                            break;
                        case "frames":
                            currentStateFrameCount = int.Parse(value);
                            break;
                        case "delay":
                            string[] frameDelays = value.Split(",");

                            currentStateFrameDelays = new float[frameDelays.Length];
                            for (int i = 0; i < frameDelays.Length; i++) {
                                currentStateFrameDelays[i] = float.Parse(frameDelays[i], CultureInfo.InvariantCulture);
                            }

                            break;
                        case "loop":
                            currentState.Loop = (int.Parse(value) == 0);
                            break;
                        case "rewind":
                            currentState.Rewind = (int.Parse(value) == 1);
                            break;
                        case "movement":
                            //TODO
                            break;
                        case "hotspot":
                            //TODO
                            break;
                        default:
                            throw new Exception("Invalid key \"" + key + "\" in DMI description");
                    }
                } else {
                    throw new Exception("Invalid line in DMI description: \"" + line + "\"");
                }
            }
            for (int d = 0; d < currentStateDirectionCount; d++) // for each direction
            {
                ParsedDMIFrame[] frames = new ParsedDMIFrame[currentStateFrameCount];
                AtomDirection direction = DMIDirectionOrder[d];
                currentState.Directions[(int)direction] = frames;
                for (int f = 0; f < currentStateFrameCount; f++) // for each frame in the direciton
                {
                    ParsedDMIFrame frame = new ParsedDMIFrame();

                    frame.X = currentFrameX;
                    frame.Y = currentFrameY;
                    frame.Delay = (currentStateFrameDelays != null) ? currentStateFrameDelays[f] : 1;
                    frame.Delay *= 100; //Convert from deciseconds to milliseconds
                    currentState.Directions[(int)direction][f] = frame;

                    currentFrameX += description.Dimensions.X;
                    if (currentFrameX >= imageWidth) {
                        currentFrameY += description.Dimensions.Y;
                        currentFrameX = 0;
                    }
                }
            }

            return description;
        }

        private static string ParseString(string value) {
            if (value.StartsWith("\"") && value.EndsWith("\"")) {
                return value.Substring(1, value.Length - 2);
            } else {
                throw new Exception("Invalid string in DMI description: " + value);
            }
        }

        private static bool VerifyPNG(Stream stream) {
            Span<byte> header = new byte[PNGHeader.Length];
            if (stream.Read(header) < PNGHeader.Length) return false;
            return VerifyPNG(header);
        }
        /// <summary>
        /// Does a simple PNG verification by checking the first eight bytes for the PNG header.
        /// </summary>
        /// <returns>true if valid PNG file, false if not</returns>
        public static bool VerifyPNG(Span<byte> header)
        {
            if (header.Length < PNGHeader.Length)
                return false;
            for (int i = 0; i < PNGHeader.Length; i++)
            {
                if (header[i] != PNGHeader[i]) return false;
            }

            return true;
        }

        private static uint ReadBigEndianUint32(BinaryReader reader) {
            byte[] bytes = reader.ReadBytes(4);
            Array.Reverse(bytes); //Little to Big-Endian
            return BitConverter.ToUInt32(bytes);
        }
    }
}
