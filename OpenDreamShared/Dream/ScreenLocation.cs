using OpenDreamShared.Net.Packets;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace OpenDreamShared.Dream {
    public class ScreenLocation {
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
            string[] rangeSplit = screenLocation.Split(" to ");
            if (rangeSplit.Length > 1) Range = new ScreenLocation(rangeSplit[1]);
            else Range = null;

            string[] coordinateSplit = rangeSplit[0].Split(",");
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

        public override string ToString() {
            return X + ":" + PixelOffsetX + "," + Y + ":" + PixelOffsetY;
        }

        public void WriteToPacket(PacketStream stream) {
            stream.WriteSByte((sbyte)X);
            stream.WriteSByte((sbyte)Y);
            stream.WriteSByte((sbyte)PixelOffsetX);
            stream.WriteSByte((sbyte)PixelOffsetY);

            stream.WriteBool(Range != null);
            Range?.WriteToPacket(stream);
        }

        public static ScreenLocation ReadFromPacket(PacketStream stream) {
            int X = stream.ReadSByte();
            int Y = stream.ReadSByte();
            int pixelOffsetX = stream.ReadSByte();
            int pixelOffsetY = stream.ReadSByte();

            ScreenLocation range = null;
            if (stream.ReadBool()) {
                range = ReadFromPacket(stream);
            }

            return new ScreenLocation(X, Y, pixelOffsetX, pixelOffsetY, range);
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
