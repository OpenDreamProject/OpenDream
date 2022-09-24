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
        private static readonly byte[] PngHeader = { 0x89, 0x50, 0x4E, 0x47, 0xD, 0xA, 0x1A, 0xA };
        private static readonly AtomDirection[] DMIFrameDirections = new AtomDirection[] {
            AtomDirection.South,
            AtomDirection.North,
            AtomDirection.East,
            AtomDirection.West,
            AtomDirection.Southeast,
            AtomDirection.Southwest,
            AtomDirection.Northeast,
            AtomDirection.Northwest
        };

        public sealed class ParsedDMIDescription {
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

            private static ParsedDMIDescription CloneDescription(ParsedDMIDescription original)
            {
                Dictionary<string, ParsedDMIState> stateCopy = new(original.States.Count);
                foreach (var key in original.States.Keys)
                {
                    stateCopy.Add(key, original.States[key]);
                }

                return new ParsedDMIDescription() {
                    Source = original.Source,
                    Version = original.Version,
                    Width = original.Width,
                    Height = original.Height,
                    States = stateCopy
                };
            }

            public void InsertIcon(ParsedDMIDescription original_icon, string icon_state, AtomDirection? dir, int? frame, float? delay)
            {
                var icon = CloneDescription(original_icon);
                IEnumerable<ParsedDMIState> states;
                if (icon_state is null) {
                    states = icon.States.Values;
                } else {
                    states = new ParsedDMIState[] { icon.States[icon_state] };
                }

                foreach (var state in states)
                {
                    if (dir is not null)
                    {
                        var goodDir = state.Directions[dir.Value];
                        state.Directions.Clear();
                        state.Directions = new Dictionary<AtomDirection, ParsedDMIFrame[]>(1) { { dir.Value, goodDir } };
                    }

                    if (frame is not null)
                    {
                        // TODO Ref says it must start at 1, need to check behavior for when it's less. Manually validate it for now.
                        var goodFrame = Math.Max(frame.Value, 1);
                        foreach (var (direction, frames) in state.Directions)
                        {
                            state.Directions[direction] = new ParsedDMIFrame[1] { frames[goodFrame] };
                            if (delay is not null or 0) {
                                state.Rewind = delay < 0;
                                frames[goodFrame].Delay = Math.Abs(delay.Value);
                            }
                        }
                    }
                }

                // All of that above was just to adjust the inserted icon to match the args. Now we can actually insert it.

                foreach (var state in states)
                {
                    States[state.Name] = state;
                }
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
            public Dictionary<AtomDirection, ParsedDMIFrame[]> Directions = new();
            public bool Loop = true;
            public bool Rewind = false;

            public ParsedDMIFrame[] GetFrames(AtomDirection direction = AtomDirection.South) {
                if (!Directions.ContainsKey(direction)) direction = Directions.Keys.First();

                return Directions[direction];
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

        public static ParsedDMIDescription ParseDMIDescription(string dmiDescription, uint imageWidth) {
            ParsedDMIDescription description = new ParsedDMIDescription();
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
