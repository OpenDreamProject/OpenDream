using System;
using System.Collections.Generic;
using System.Drawing;

namespace OpenDreamShared.Dream {
    public struct ScreenLocation {
        public int X, Y;
        public int PixelOffsetX, PixelOffsetY;

        public ScreenLocation(int x, int y, int pixelOffsetX, int pixelOffsetY) {
            X = x;
            Y = y;
            PixelOffsetX = pixelOffsetX;
            PixelOffsetY = pixelOffsetY;
        }

        public ScreenLocation(string screenLocation) {
            string[] coordinateSplit = screenLocation.Split(",");
            if (coordinateSplit.Length != 2) throw new Exception("Invalid screen_loc");
            (int Coordinate, int PixelOffset) x = ParseScreenLocCoordinate(coordinateSplit[0]);
            (int Coordinate, int PixelOffset) y = ParseScreenLocCoordinate(coordinateSplit[1]);

            X = x.Coordinate;
            Y = y.Coordinate;
            PixelOffsetX = x.PixelOffset;
            PixelOffsetY = y.PixelOffset;
        }

        public Point GetScreenCoordinates(int iconSize) {
            return new Point(iconSize * (X - 1) + PixelOffsetX, iconSize * (Y - 1) + PixelOffsetY);
        }

        private static (int Coordinate, int PixelOffset) ParseScreenLocCoordinate(string coordinate) {
            coordinate = coordinate.Trim();
            if (coordinate == String.Empty) throw new Exception("Invalid screen_loc coordinate");
            coordinate = coordinate.Replace("SOUTH", "1");
            coordinate = coordinate.Replace("WEST", "1");
            coordinate = coordinate.Replace("NORTH", "15");
            coordinate = coordinate.Replace("EAST", "15");
            coordinate = coordinate.Replace("CENTER", "8");

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
                    if (currentNumber == String.Empty) throw new Exception("Expected a number in screen_loc");

                    string[] numberSplit = currentNumber.Split(":");
                    if (numberSplit.Length > 2) throw new Exception("Invalid number in screen_loc");

                    operations.Add((currentOperation, float.Parse(numberSplit[0]), (numberSplit.Length == 2) ? int.Parse(numberSplit[1]) : 0));
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
