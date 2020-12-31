using System;
using System.Collections.Generic;
using System.Drawing;

namespace OpenDreamShared.Dream {
    public struct IconVisualProperties {
        public static Dictionary<string, UInt32> Colors = new() {
            { "black", 0x000000FF},
            { "silver", 0xC0C0C0FF},
            { "gray", 0x808080FF},
            { "grey", 0x808080FF},
            { "white", 0xFFFFFFFF},
            { "maroon", 0x800000FF},
            { "red", 0xFF0000FF },
            { "purple", 0x800080FF },
            { "fuschia", 0xFF00FFFF },
            { "magenta", 0xFF00FFFF },
            { "green", 0x00C000FF },
            { "lime", 0x00FF00FF },
            { "olive", 0x808000FF },
            { "gold", 0x808000FF },
            { "yellow", 0xFFFF00FF },
            { "navy", 0x000080FF },
            { "blue", 0x0000FFFF },
            { "teal", 0x008080FF },
            { "aqua", 0x00FFFFFF },
            { "cyan", 0x00FFFFFF }
        };

        public string Icon, IconState;
        public UInt32 Color;
        public AtomDirection Direction;
        public float Layer;
        public int PixelX, PixelY;

        public bool Equals(IconVisualProperties other) {
            if (other.Icon != Icon) return false;
            if (other.IconState != IconState) return false;
            if (other.Direction != Direction) return false;
            if (other.Color != Color) return false;
            if (other.Layer != Layer) return false;
            if (other.PixelX != PixelX) return false;
            if (other.PixelY != PixelY) return false;

            return true;
        }

        public IconVisualProperties Merge(IconVisualProperties other) {
            IconVisualProperties newVisualProperties = this;

            if (other.Icon != default) newVisualProperties.Icon = other.Icon;
            if (other.IconState != default) newVisualProperties.IconState = other.IconState;
            if (other.Direction != default) newVisualProperties.Direction = other.Direction;
            if (other.Color != default) newVisualProperties.Color = other.Color;
            if (other.Layer != default) newVisualProperties.Layer = other.Layer;
            if (other.PixelX != default) newVisualProperties.PixelX = other.PixelX;
            if (other.PixelY != default) newVisualProperties.PixelY = other.PixelY;

            return newVisualProperties;
        }

        public bool IsDefault() {
            if (Icon != default) return false;
            if (IconState != default) return false;
            if (Direction != default) return false;
            if (Color != default) return false;
            if (Layer != default) return false;
            if (PixelX != default) return false;
            if (PixelY != default) return false;

            return true;
        }

        public void SetColor(string color) {
            if (color.StartsWith("#")) {
                color = color.Substring(1);

                if (color.Length == 3 || color.Length == 4) { //4-bit color; repeat each digit
                    string alphaComponent = (color.Length == 4) ? new string(color[3], 2) : "ff";

                    color = new string(color[0], 2) + new string(color[1], 2) + new string(color[2], 2) + alphaComponent;
                } else if (color.Length == 6) { //Missing alpha
                    color += "ff";
                }

                Color = Convert.ToUInt32(color, 16);
            } else if (!Colors.TryGetValue(color.ToLower(), out Color)) {
                throw new ArgumentException("Invalid color '" + color + "'");
            }
        }
    }

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
