using Robust.Shared.Maths;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using OpenDreamShared.Dream;
using System.Globalization;
using JetBrains.Annotations;

namespace OpenDreamShared.Resources {
    public static class DMIParser {
        public static readonly AtomDirection[] DMIFrameDirections = {
            AtomDirection.South,
            AtomDirection.North,
            AtomDirection.East,
            AtomDirection.West,
            AtomDirection.Southeast,
            AtomDirection.Southwest,
            AtomDirection.Northeast,
            AtomDirection.Northwest
        };

        private static readonly byte[] PngHeader = { 0x89, 0x50, 0x4E, 0x47, 0xD, 0xA, 0x1A, 0xA };

        public sealed class ParsedDMIDescription {
            public int Width, Height;
            public Dictionary<string, ParsedDMIState> States;

            /// <summary>
            /// Gets the requested state, or the default if it doesn't exist
            /// </summary>
            /// <remarks>The default state could also not exist</remarks>
            /// <param name="stateName">The requested state's name</param>
            /// <returns>The requested state, default state, or null</returns>
            [CanBeNull]
            public ParsedDMIState GetStateOrDefault(string stateName) {
                if (String.IsNullOrEmpty(stateName) || !States.TryGetValue(stateName, out var state)) {
                    States.TryGetValue(String.Empty, out state);
                }

                return state;
            }

            /// <summary>
            /// Construct a string describing this DMI description<br/>
            /// In the same format as the text found in .dmi files
            /// </summary>
            /// <returns>This ParsedDMIDescription represented as text</returns>
            public string ExportAsText() {
                StringBuilder text = new();

                text.AppendLine("# BEGIN DMI");

                // This could either end up compressed or decompressed depending on how large this text ends up being.
                // So go with version 3.0, BYOND doesn't seem to care either way
                text.AppendLine("version = 3.0");
                text.Append("\twidth = ");
                text.Append(Width);
                text.AppendLine();
                text.Append("\theight = ");
                text.Append(Height);
                text.AppendLine();

                foreach (var state in States.Values) {
                    state.ExportAsText(text);
                }

                text.Append("# END DMI");

                return text.ToString();
            }
        }

        public sealed class ParsedDMIState {
            public string Name;
            public Dictionary<AtomDirection, ParsedDMIFrame[]> Directions = new();
            public bool Loop = true;
            public bool Rewind = false;

            /// <summary>
            /// The amount of animation frames this state has
            /// </summary>
            public int FrameCount {
                get {
                    if (Directions.Count == 0)
                        return 0;

                    return Directions.Values.First().Length;
                }
            }

            public ParsedDMIFrame[] GetFrames(AtomDirection direction = AtomDirection.South) {
                if (!Directions.ContainsKey(direction)) direction = Directions.Keys.First();

                return Directions[direction];
            }

            public void ExportAsText(StringBuilder text) {
                text.Append("state = \"");
                text.Append(Name);
                text.AppendLine("\"");

                text.Append("\tdirs = ");
                text.Append(Directions.Count);
                text.AppendLine();

                text.Append("\tframes = ");
                text.Append(FrameCount);
                text.AppendLine();

                if (!Loop) {
                    text.AppendLine("\tloop = 0");
                }

                if (Rewind) {
                    text.AppendLine("\trewind = 1");
                }
            }

            /// <summary>
            /// Get this state's frames
            /// </summary>
            /// <param name="dir">Which direction to get. Every direction if null.</param>
            /// <param name="frame">Which frame to get. Every frame if null.</param>
            /// <returns>A dictionary containing the specified frames for each specified direction</returns>
            public Dictionary<AtomDirection, ParsedDMIFrame[]> GetFrames(AtomDirection? dir = null, int? frame = null) {
                Dictionary<AtomDirection, ParsedDMIFrame[]> directions;
                if (dir == null) { // Get every direction
                    directions = new(Directions);
                } else {
                    directions = new(1);

                    if (!Directions.TryGetValue(dir.Value, out var frames))
                        frames = Array.Empty<ParsedDMIFrame>();

                    // Getting only one direction will give it to you with AtomDirection.South
                    directions.Add(AtomDirection.South, frames);
                }

                if (frame != null) { // Only get a specified frame
                    foreach (var direction in directions) {
                        ParsedDMIFrame[] newFrames = new ParsedDMIFrame[1];

                        newFrames[0] = direction.Value[frame.Value];
                        directions[direction.Key] = newFrames;
                    }
                }

                return directions;
            }
        }

        public sealed class ParsedDMIFrame {
            public int X, Y;
            public float Delay;
        }

        public static ParsedDMIDescription ParseDMI(Stream stream) {
            if (!VerifyPNG(stream)) throw new Exception("Provided stream was not a valid PNG");

            BinaryReader reader = new BinaryReader(stream);
            Vector2u? imageSize = null;

            while (stream.Position < stream.Length) {
                long chunkDataPosition = stream.Position;
                uint chunkLength = ReadBigEndianUint32(reader);
                string chunkType = new string(reader.ReadChars(4));

                switch (chunkType) {
                    case "IHDR": //Image header, contains the image size
                        imageSize = new Vector2u(ReadBigEndianUint32(reader), ReadBigEndianUint32(reader));
                        stream.Seek(chunkLength - 4, SeekOrigin.Current); //Skip the rest of the chunk
                        break;
                    case "zTXt": //Compressed text, likely contains our DMI description
                    case "tEXt": //Uncompressed text. Not typical, but also works.
                        if (imageSize == null) throw new Exception("The PNG did not contain an IHDR chunk");

                        StringBuilder keyword = new StringBuilder();
                        while (reader.PeekChar() != 0 && keyword.Length < 79) {
                            keyword.Append(reader.ReadChar());
                        }

                        stream.Seek(1, SeekOrigin.Current); //Skip over null-terminator
                        if (chunkType == "zTXt")
                            stream.Seek(1, SeekOrigin.Current); //Skip over compression type

                        if (keyword.ToString() == "Description") {
                            byte[] uncompressedData;

                            if (chunkType == "zTXt") {
                                stream.Seek(2, SeekOrigin.Current); //Skip the first 2 bytes in the zlib format

                                DeflateStream deflateStream = new DeflateStream(stream, CompressionMode.Decompress);
                                MemoryStream uncompressedDataStream = new MemoryStream();

                                deflateStream.CopyTo(uncompressedDataStream, (int)chunkLength - keyword.Length - 2);

                                uncompressedData = new byte[uncompressedDataStream.Length];
                                uncompressedDataStream.Seek(0, SeekOrigin.Begin);
                                uncompressedDataStream.Read(uncompressedData);
                            } else {
                                //The text is not compressed so nothing fancy is required
                                uncompressedData = reader.ReadBytes((int) chunkLength - keyword.Length - 1);
                            }

                            string dmiDescription = Encoding.UTF8.GetString(uncompressedData, 0, uncompressedData.Length);
                            return ParseDMIDescription(dmiDescription, imageSize.Value.X);
                        }

                        // Wasn't the description chunk we were looking for
                        stream.Position = chunkDataPosition + chunkLength + 4;
                        break;
                    default: //Nothing we care about, skip it
                        stream.Seek(chunkLength + 4, SeekOrigin.Current);
                        break;
                }
            }

            throw new Exception("Could not find a DMI description");
        }

        private static ParsedDMIDescription ParseDMIDescription(string dmiDescription, uint imageWidth) {
            ParsedDMIDescription description = new ParsedDMIDescription();
            ParsedDMIState currentState = null;
            int currentFrameX = 0;
            int currentFrameY = 0;
            int currentStateDirectionCount = 1;
            int currentStateFrameCount = 1;
            float[] currentStateFrameDelays = null;

            description.States = new Dictionary<string, ParsedDMIState>();

            string[] lines = dmiDescription.Split("\n");
            foreach (string line in lines) {
                if (line.StartsWith('#') || string.IsNullOrWhiteSpace(line))
                    continue;

                int equalsIndex = line.IndexOf('=');

                if (equalsIndex != -1) {
                    string key = line.Substring(0, equalsIndex-1).Trim();
                    string value = line.Substring(equalsIndex + 1).Trim();

                    switch (key) {
                        case "version":
                            // No need to care about this at the moment
                            break;
                        case "width":
                            description.Width = int.Parse(value);
                            break;
                        case "height":
                            description.Height = int.Parse(value);
                            break;
                        case "state":
                            string stateName = ParseString(value);

                            if (currentState != null) {
                                for (int i = 0; i < currentStateDirectionCount; i++) {
                                    ParsedDMIFrame[] frames = new ParsedDMIFrame[currentStateFrameCount];
                                    AtomDirection direction = DMIFrameDirections[i];

                                    currentState.Directions[direction] = frames;
                                }

                                for (int i = 0; i < currentStateFrameCount; i++) {
                                    for (int j = 0; j < currentStateDirectionCount; j++) {
                                        AtomDirection direction = DMIFrameDirections[j];

                                        ParsedDMIFrame frame = new ParsedDMIFrame();

                                        frame.X = currentFrameX;
                                        frame.Y = currentFrameY;
                                        frame.Delay = (currentStateFrameDelays != null) ? currentStateFrameDelays[i] : 1;
                                        frame.Delay *= 100; //Convert from deciseconds to milliseconds
                                        currentState.Directions[direction][i] = frame;

                                        currentFrameX += description.Width;
                                        if (currentFrameX >= imageWidth) {
                                            currentFrameY += description.Height;
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
                            throw new Exception($"Invalid key \"{key}\" in DMI description");
                    }
                } else {
                    throw new Exception($"Invalid line in DMI description: \"{line}\"");
                }
            }

            for (int i = 0; i < currentStateDirectionCount; i++) {
                ParsedDMIFrame[] frames = new ParsedDMIFrame[currentStateFrameCount];
                AtomDirection direction = DMIFrameDirections[i];

                currentState.Directions[direction] = frames;
            }

            for (int i = 0; i < currentStateFrameCount; i++) {
                for (int j = 0; j < currentStateDirectionCount; j++) {
                    AtomDirection direction = DMIFrameDirections[j];

                    ParsedDMIFrame frame = new ParsedDMIFrame();

                    frame.X = currentFrameX;
                    frame.Y = currentFrameY;
                    frame.Delay = (currentStateFrameDelays != null) ? currentStateFrameDelays[i] : 1;
                    frame.Delay *= 100; //Convert from deciseconds to milliseconds
                    currentState.Directions[direction][i] = frame;

                    currentFrameX += description.Width;
                    if (currentFrameX >= imageWidth) {
                        currentFrameY += description.Height;
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
                throw new Exception($"Invalid string in DMI description: {value}");
            }
        }

        private static bool VerifyPNG(Stream stream) {
            byte[] header = new byte[PngHeader.Length];
            if (stream.Read(header, 0, header.Length) < header.Length) return false;

            for (int i = 0; i < PngHeader.Length; i++) {
                if (header[i] != PngHeader[i]) return false;
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
