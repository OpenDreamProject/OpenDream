using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace OpenDreamShared.Resources {
    public static class DMIParser {
        private static readonly AtomDirection[] DMIFrameDirections = {
            AtomDirection.South,
            AtomDirection.North,
            AtomDirection.East,
            AtomDirection.West,
            AtomDirection.Southeast,
            AtomDirection.Southwest,
            AtomDirection.Northeast,
            AtomDirection.Northwest
        };

        public class ParsedDMIDescription {
            public string Source;
            public float Version;
            public int Width, Height;
            public Dictionary<string, ParsedDMIState> States;

            public static ParsedDMIDescription CreateEmpty(int width, int height) {
                ParsedDMIFrame[] frames = { new() };
                ParsedDMIState state = new();
                state.Directions.Add(AtomDirection.South, frames);

                return new ParsedDMIDescription() {
                    Source = null,
                    Version = 4f,
                    Width = width,
                    Height = height,
                    States = new() {
                        { "", state }
                    }
                };
            }

            public bool HasState(string stateName = null) {
                return States.ContainsKey(stateName ?? "");
            }

            public ParsedDMIState GetState(string stateName = null) {
                return States[stateName ?? ""];
            }
        }

        public class ParsedDMIState {
            public string Name;
            public Dictionary<AtomDirection, ParsedDMIFrame[]> Directions = new();
            public bool Loop = true;
            public bool Rewind = false;

            public ParsedDMIFrame[] GetFrames(AtomDirection direction = AtomDirection.South) {
                if (!Directions.ContainsKey(direction)) direction = Directions.Keys.First();

                return Directions[direction];
            }
        }

        public class ParsedDMIFrame {
            public int X, Y;
            public float Delay;
        }

        private static uint _readBigEndianUint32(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(4);
            Array.Reverse(bytes); //Little to Big-Endian
            return BitConverter.ToUInt32(bytes);
        }

        private static (uint, string) _readChunk(BinaryReader reader)
        {
            uint chunkLength = _readBigEndianUint32(reader);
            string chunkType = new string(reader.ReadChars(4));

            return (chunkLength, chunkType);
        }

        public static bool TryReadDMIDescription(byte[] bytes, out string description) {
            MemoryStream stream = new MemoryStream(bytes);
            BinaryReader reader = new BinaryReader(stream);

            stream.Seek(8, SeekOrigin.Begin);

            while (stream.Position < stream.Length) {
                var (chunkLength, chunkType) = _readChunk(reader);

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

                        description = Encoding.UTF8.GetString(uncompressedData, 0, uncompressedData.Length);
                        return true;
                    } else {
                        stream.Position = chunkDataPosition + chunkLength + 4;
                    }
                }
            }

            description = null;
            return false;
        }

        public static List<string> GetIconStatesFromDescription(string dmiDescription)
        {
            var states = new List<string>();
            dmiDescription = TrimDescription(dmiDescription);

            var lines = dmiDescription.Split("\n");
            foreach (var line in lines)
            {
                var equalsIndex = line.IndexOf('=');

                if (equalsIndex == -1)
                {
                    throw new Exception("Invalid line in DMI description: \"" + line + "\"");
                }
                var key = line[..equalsIndex].Trim();
                var value = line[(equalsIndex + 1)..].Trim();

                if (key != "state")
                {
                    continue;
                }
                var stateName = ParseString(value);
                states.Add(stateName);
            }

            return states;
        }

        private static string TrimDescription(string dmiDescription)
        {
            return dmiDescription
                .Replace("# BEGIN DMI", "")
                .Replace("# END DMI", "")
                .Replace("\t", "")
                .Trim();
        }

        public static ParsedDMIDescription ParseDMIDescription(string dmiDescription, int imageWidth) {
            ParsedDMIDescription description = new ParsedDMIDescription();
            ParsedDMIState currentState = null;
            int currentFrameX = 0;
            int currentFrameY = 0;
            int currentStateDirectionCount = 1;
            int currentStateFrameCount = 1;
            float[] currentStateFrameDelays = null;

            description.States = new Dictionary<string, ParsedDMIState>();

            dmiDescription = TrimDescription(dmiDescription);
            description.Source = dmiDescription;

            string[] lines = dmiDescription.Split("\n");
            foreach (string line in lines) {
                int equalsIndex = line.IndexOf('=');

                if (equalsIndex != -1) {
                    string key = line.Substring(0, equalsIndex).Trim();
                    string value = line.Substring(equalsIndex + 1).Trim();

                    switch (key) {
                        case "version":
                            description.Version = float.Parse(value);
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
                                currentStateFrameDelays[i] = float.Parse(frameDelays[i]);
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
                throw new Exception("Invalid string in DMI description: " + value);
            }
        }
    }
}
