using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using Robust.Shared.Log;

namespace OpenDreamShared.Dream {
    [Serializable, NetSerializable]
    public sealed class ScreenLocation {
        public int X, Y;
        public int PixelOffsetX, PixelOffsetY;
        public ScreenLocation Range;
        public int RepeatX { get => (Range?.X - X + 1) ?? 1; }
        public int RepeatY { get => (Range?.Y - Y + 1) ?? 1; }

        public ScreenLocation(int x, int y, int pixelOffsetX, int pixelOffsetY, ScreenLocation range = null) {
            X = x;
            Y = y;
            PixelOffsetX = pixelOffsetX;
            PixelOffsetY = pixelOffsetY;
            Range = range;
        }

        public ScreenLocation(int x, int y, int iconSize) {
            X = x / iconSize + 1;
            Y = y / iconSize + 1;

            PixelOffsetX = x % iconSize;
            PixelOffsetY = y % iconSize;
            Range = null;
        }

        public ScreenLocation(string screenLocation) {
            screenLocation = screenLocation.ToUpper(CultureInfo.InvariantCulture);

            // TODO Support non-15x15
            (X, Y, PixelOffsetX, PixelOffsetY, Range) = screenLocation switch {
                "TOPLEFT" => (1, 15, 0, 0, null),
                "TOPRIGHT" => (15, 15, 0, 0, null),
                "BOTTOMLEFT" => (1, 1, 0, 0, null),
                "BOTTOMRIGHT" => (1, 15, 0, 0, null),
                "CENTER" => (7, 7, 0, 0, null),
                _ => ParseScreenLoc(screenLocation)
            };
        }

        public Vector2 GetViewPosition(Vector2 viewOffset, float iconSize) {
            return viewOffset + ((X - 1) + (PixelOffsetX / iconSize), (Y - 1) + (PixelOffsetY / iconSize));
        }

        public override string ToString() {
            return X + ":" + PixelOffsetX + "," + Y + ":" + PixelOffsetY;
        }

        private static (int X, int Y, int PixelOffsetX, int PixelOffsetY, ScreenLocation Range) ParseScreenLoc(string screenLoc) {
            string[] rangeSplit = screenLoc.Split(" TO ");
            ScreenLocation range = (rangeSplit.Length > 1) ? new ScreenLocation(rangeSplit[1]) : null;

            string[] coordinateSplit = rangeSplit[0].Split(",");

            if (coordinateSplit[0].Contains(':'))
            {
                var innerSplit = coordinateSplit[0].Split(':');
                if (!int.TryParse(innerSplit[0], out _))
                {
                    //TODO Implement this
                    Logger.LogS(LogLevel.Warning, "opendream.unimplemented", $"Cannot render screen_loc to secondary map control ({innerSplit[0]}) yet, ignoring");
                    coordinateSplit[0] = innerSplit[1];
                }
            }

            if (coordinateSplit.Length != 2)
            {
                // Doing "CENTER" is equivalent to "CENTER,CENTER"
                if (coordinateSplit.Length == 1 && coordinateSplit[0] == "CENTER")
                {
                    coordinateSplit = new[] { coordinateSplit[0], "CENTER" };
                }
                else
                {
                    throw new Exception($"Invalid screen_loc: {screenLoc}");
                }
            }

            (int x, int pixelOffsetX) = ParseScreenLocCoordinate(coordinateSplit[0]);
            (int y, int pixelOffsetY) = ParseScreenLocCoordinate(coordinateSplit[1]);
            return (x, y, pixelOffsetX, pixelOffsetY, range);
        }

        private static (int Coordinate, int PixelOffset) ParseScreenLocCoordinate(string coordinate) {
            coordinate = coordinate.Trim();
            if (coordinate == String.Empty) throw new Exception($"Invalid screen_loc coordinate: {coordinate}");
            // TODO Support non-15x15
            coordinate = coordinate.Replace("SOUTH", "1");
            coordinate = coordinate.Replace("WEST", "1");
            coordinate = coordinate.Replace("NORTH", "15");
            coordinate = coordinate.Replace("EAST", "15");
            coordinate = coordinate.Replace("CENTER", "8");

            // TODO: These interact with map zoom in some way
            coordinate = coordinate.Replace("LEFT", "1");
            coordinate = coordinate.Replace("BOTTOM", "1");
            coordinate = coordinate.Replace("RIGHT", "15");
            coordinate = coordinate.Replace("TOP", "15");

            List<(string Operation, float Number, int PixelOffset)> operations = new();
            string currentOperation = null;
            string currentNumber = String.Empty;
            int i = 0;
            do {
                char c = (i < coordinate.Length) ? coordinate[i] : '\0';
                i++;

                if ((c >= '0' && c <= '9') || (currentNumber != String.Empty && (c == '.' || c == ':')) || ((currentNumber == String.Empty || currentNumber.EndsWith(":")) && c == '-')) {
                    currentNumber += c;
                } else {
                    if (currentNumber == String.Empty) throw new Exception($"Expected a number in screen_loc, got coordinate: {coordinate}");

                    string[] numberSplit = currentNumber.Split(":");
                    if (numberSplit.Length > 2) throw new Exception($"Invalid number in screen_loc, got coordinate: {coordinate}");

                    operations.Add((currentOperation, float.Parse(numberSplit[0], CultureInfo.InvariantCulture), (numberSplit.Length == 2) ? int.Parse(numberSplit[1]) : 0));
                    currentOperation = c.ToString();
                    currentNumber = String.Empty;
                }
            } while (i <= coordinate.Length);

            float coordinateResult = 0.0f;
            int pixelOffsetResult = 0;
            foreach ((string Operation, float Number, int PixelOffset) operation in operations) {
                pixelOffsetResult += operation.PixelOffset;

                switch (operation.Operation) {
                    case null: coordinateResult = operation.Number; break;
                    case "+": coordinateResult += operation.Number; break;
                    case "-": coordinateResult -= operation.Number; break;
                    default: throw new Exception("Invalid operation '" + operation.Operation + "' in screen_loc");
                }
            }

            double fractionalOffset = coordinateResult - Math.Floor(coordinateResult);
            pixelOffsetResult += (int)(32 * fractionalOffset);
            return ((int)coordinateResult, pixelOffsetResult);
        }
    }
}
