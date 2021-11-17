using System;
using System.Collections.Generic;

namespace OpenDreamShared.Dream
{
    public static class ColorHelpers
    {
        public static readonly Dictionary<String, UInt32> Colors = new() {
            { "black", 0x000000FF },
            { "silver", 0xC0C0C0FF },
            { "gray", 0x808080FF },
            { "grey", 0x808080FF },
            { "white", 0xFFFFFFFF },
            { "maroon", 0x800000FF },
            { "red", 0xFF0000FF },
            { "purple", 0x800080FF },
            { "fuchsia", 0xFF00FFFF },
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

        // Takes a 4-bit or 8-bit hex color and returns an 8-bit hex color, minus the #
        public static string ParseHexColor(string color, bool insertAlpha = true)
        {
            if (color.StartsWith("#")) color = color.Substring(1);

            switch (color.Length)
            {
                case 3:
                case 4:
                {
                    //4-bit color; repeat each digit

                    string alphaComponent = "";
                    if (insertAlpha)
                    {
                        alphaComponent = (color.Length == 4) ? new string(color[3], 2) : "ff";
                    }

                    color = new string(color[0], 2) + new string(color[1], 2) + new string(color[2], 2) + alphaComponent;
                    break;
                }
                case 6:
                    //Missing alpha
                    if (insertAlpha)
                    {
                        color += "ff";
                    }
                    break;
            }
            return color;
        }

        public static bool IsValidHexLength(string color)
        {
            if (color.StartsWith("#")) color = color.Substring(1);

            return (color.Length == 3 || color.Length == 4 || color.Length == 6 || color.Length == 8);
        }
    }
}
