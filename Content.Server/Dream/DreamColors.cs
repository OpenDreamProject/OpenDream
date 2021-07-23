using Robust.Shared.Maths;
using System;
using System.Collections.Generic;

namespace Content.Server.Dream {
    static class DreamColors {
        private static readonly Dictionary<String, Color> _colors = new() {
            { "black", new Color(0, 0, 0) },
            { "silver", new Color(192, 192, 192) },
            { "gray", new Color(128, 128, 128) },
            { "grey", new Color(128, 128, 128) },
            { "white", new Color(255, 255, 255) },
            { "maroon", new Color(128, 0, 0) },
            { "red", new Color(255, 0, 0) },
            { "purple", new Color(128, 0, 128) },
            { "fuchsia", new Color(255, 0, 255) },
            { "magenta", new Color(255, 0, 255) },
            { "green", new Color(0, 192, 0) },
            { "lime", new Color(0, 255, 0) },
            { "olive", new Color(128, 128, 0) },
            { "gold", new Color(128, 128, 0) },
            { "yellow", new Color(255, 255, 0) },
            { "navy", new Color(0, 0, 128) },
            { "blue", new Color(0, 0, 255) },
            { "teal", new Color(0, 128, 128) },
            { "aqua", new Color(0, 255, 255) },
            { "cyan", new Color(0, 255, 255) }
        };

        public static Color GetColor(string text) {
            if (!_colors.TryGetValue(text, out Color color)) {
                color = Color.TryFromHex(text) ?? Color.White;
            }

            return color;
        }
    }
}
