using Robust.Shared.Maths;
using System.Collections.Generic;

namespace OpenDreamShared.Dream;

public static class ColorHelpers {
    public static readonly Dictionary<string, Color> Colors = new() {
        {"black", new Color(00, 00, 00)},
        {"silver", new Color(192, 192, 192)},
        {"gray", new Color(128, 128, 128)},
        {"grey", new Color(128, 128, 128)},
        {"white", new Color(255, 255, 255)},
        {"maroon", new Color(128, 0, 0)},
        {"red", new Color(255, 0, 0)},
        {"purple", new Color(128, 0, 128)},
        {"fuchsia", new Color(255, 0, 255)},
        {"magenta", new Color(255, 0, 255)},
        {"green", new Color(0, 192, 0)},
        {"lime", new Color(0, 255, 0)},
        {"olive", new Color(128, 128, 0)},
        {"gold", new Color(128, 128, 0)},
        {"yellow", new Color(255, 255, 0)},
        {"navy", new Color(0, 0, 128)},
        {"blue", new Color(0, 0, 255)},
        {"teal", new Color(0, 128, 128)},
        {"aqua", new Color(0, 255, 255)},
        {"cyan", new Color(0, 255, 255)}
    };

    public enum ColorSpace {
        RGB = 0,
        HSV = 1,
        HSL = 2
    }

    public static bool TryParseColor(string color, out Color colorOut, string defaultAlpha = "ff") {
        if (color.StartsWith("#")) {
            if (color.Length == 4 || color.Length == 5) { //4-bit color; repeat each digit
                string alphaComponent = (color.Length == 5) ? new string(color[4], 2) : defaultAlpha;

                color = new string('#', 1) + new string(color[1], 2) + new string(color[2], 2) +
                        new string(color[3], 2) + alphaComponent;
            } else if (color.Length == 7) { //Missing alpha
                color += defaultAlpha;
            }

            colorOut = Color.FromHex(color, Color.White);
            return true;
        }

        return Colors.TryGetValue(color.ToLower(), out colorOut);
    }
}
