using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenDreamShared.Resources {
    static class DMIParser {
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

        public class ParsedDMIDescription {
            public string Source;
            public float Version;
            public int Width, Height;
            public string DefaultStateName;
            public Dictionary<string, ParsedDMIState> States;

            public bool HasState(string stateName = null) {
                if (stateName == null) stateName = DefaultStateName;

                return States.ContainsKey(stateName);
            }

            public ParsedDMIState GetState(string stateName = null) {
                if (!States.ContainsKey(stateName)) stateName = DefaultStateName;

                return States[stateName];
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

        public static ParsedDMIDescription ParseDMIDescription(string dmiDescription, int imageWidth) {
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

                            if (description.States.ContainsKey(stateName)) description.States.Remove(stateName);
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
                            } else {
                                description.DefaultStateName = stateName;
                            }

                            currentStateFrameCount = 1;
                            currentStateFrameDelays = null;

                            currentState = new ParsedDMIState();
                            currentState.Name = stateName;
                            description.States.Add(stateName, currentState);

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
