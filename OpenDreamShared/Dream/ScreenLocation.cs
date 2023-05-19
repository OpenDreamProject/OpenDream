using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace OpenDreamShared.Dream {
    [Serializable, NetSerializable]
    public sealed class ScreenLocation {
        public string? MapControl;
        public int X, Y;
        public int PixelOffsetX, PixelOffsetY;
        public ScreenLocation? Range;

        public int RepeatX => Range?.X - X + 1 ?? 1;
        public int RepeatY => Range?.Y - Y + 1 ?? 1;

        public ScreenLocation(int x, int y, int pixelOffsetX, int pixelOffsetY, ScreenLocation? range = null) {
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

            (MapControl, X, Y, PixelOffsetX, PixelOffsetY, Range) = ParseScreenLoc(screenLocation);
        }

        public Vector2 GetViewPosition(Vector2 viewOffset, float iconSize) {
            return viewOffset + (X - 1 + PixelOffsetX / iconSize, Y - 1 + PixelOffsetY / iconSize);
        }

        public override string ToString() {
            string mapControl = MapControl != null ? $"{MapControl}:" : string.Empty;

            return $"{mapControl}{X}:{PixelOffsetX},{Y}:{PixelOffsetY}";
        }

        private static (string? MapControl, int X, int Y, int PixelOffsetX, int PixelOffsetY, ScreenLocation? Range) ParseScreenLoc(string screenLoc) {
            string[] rangeSplit = screenLoc.Split(" TO ");
            ScreenLocation? range = rangeSplit.Length > 1 ? new ScreenLocation(rangeSplit[1]) : null;

            string[] coordinateSplit = rangeSplit[0].Split(",");

            if (coordinateSplit.Length == 1) {
                string replaced = ReplaceMacros(coordinateSplit[0], true);

                coordinateSplit = replaced.Split(",");
            }

            if (coordinateSplit.Length != 2)
                throw new Exception($"Invalid screen_loc \"{screenLoc}\"");

            string horizontal = ReplaceMacros(coordinateSplit[0], false);
            string vertical = ReplaceMacros(coordinateSplit[1], false);
            (string? mapControl, horizontal) = ParseSecondaryMapControl(horizontal);

            (int x, int pixelOffsetX) = ParseScreenLocCoordinate(horizontal);
            (int y, int pixelOffsetY) = ParseScreenLocCoordinate(vertical);
            return (mapControl, x, y, pixelOffsetX, pixelOffsetY, range);
        }

        private static string ReplaceMacros(string coordinate, bool isSingular) {
            coordinate = coordinate.Trim();
            if (coordinate == string.Empty) throw new Exception("Invalid screen_loc coordinate");
            coordinate = coordinate.Replace("SOUTH", "1");
            coordinate = coordinate.Replace("WEST", "1");
            coordinate = coordinate.Replace("NORTH", "15");
            coordinate = coordinate.Replace("EAST", "15");

            // TODO: These interact with map zoom in some way
            coordinate = coordinate.Replace("LEFT", "1");
            coordinate = coordinate.Replace("BOTTOM", "1");
            coordinate = coordinate.Replace("RIGHT", "15");
            coordinate = coordinate.Replace("TOP", "15");

            if (isSingular) {
                coordinate = coordinate.Replace("TOPLEFT", "1,15");
                coordinate = coordinate.Replace("TOPRIGHT", "15,15");
                coordinate = coordinate.Replace("BOTTOMLEFT", "1,1");
                coordinate = coordinate.Replace("BOTTOMRIGHT", "1,15");
                coordinate = coordinate.Replace("CENTER", "8,8");
            } else {
                coordinate = coordinate.Replace("CENTER", "8");
            }

            return coordinate;
        }

        private static (string? SecondaryMapControl, string Other) ParseSecondaryMapControl(string coordinate) {
            if (char.IsLetter(coordinate, 0)) { // If it starts with a letter then treat it as a map control
                string[] split = coordinate.Split(':', 2);
                if (split.Length != 2)
                    throw new Exception($"Invalid coordinate {coordinate}");

                return (split[0], split[1]);
            }

            return (null, coordinate);
        }

        private static (int Coordinate, int PixelOffset) ParseScreenLocCoordinate(string coordinate) {
            List<(string? Operation, float Number, int PixelOffset)> operations = new();
            string? currentOperation = null;
            string currentNumber = string.Empty;
            int i = 0;
            do {
                char c = i < coordinate.Length ? coordinate[i] : '\0';
                i++;
                if (c is ' ' or '\t')
                    continue;

                if (c >= '0' && c <= '9' || currentNumber != string.Empty && (c == '.' || c == ':') || (currentNumber == string.Empty || currentNumber.EndsWith(":")) && c is '-') {
                    currentNumber += c;
                } else {
                    if (currentNumber == string.Empty) throw new Exception("Expected a number in screen_loc");

                    string[] numberSplit = currentNumber.Split(":");
                    if (numberSplit.Length > 2) throw new Exception("Invalid number in screen_loc");

                    operations.Add((currentOperation, float.Parse(numberSplit[0], CultureInfo.InvariantCulture), numberSplit.Length == 2 ? int.Parse(numberSplit[1]) : 0));
                    currentOperation = c.ToString();
                    currentNumber = String.Empty;
                }
            } while (i <= coordinate.Length);

            float coordinateResult = 0.0f;
            int pixelOffsetResult = 0;
            foreach ((string? Operation, float Number, int PixelOffset) operation in operations) {
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
