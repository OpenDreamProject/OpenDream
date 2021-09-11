using System;
using System.Collections.Generic;

namespace OpenDreamShared.Dream {
    public class IconAppearance : IEquatable<IconAppearance> {
        public enum AppearanceProperty {
            End,
            Icon,
            IconState,
            Direction,
            PixelX,
            PixelY,
            Color,
            Layer,
            Invisibility,
            Overlays,
            Underlays,
            Transform,
            MouseOpacity
        }

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

        public string Icon;
        public string IconState;
        public AtomDirection Direction;
        public int PixelX, PixelY;
        public UInt32 Color = 0xFFFFFFFF;
        public float Layer;
        public int Invisibility;
        public MouseOpacity MouseOpacity = MouseOpacity.PixelOpaque;
        public List<int> Overlays = new();
        public List<int> Underlays = new();
        public float[] Transform = new float[6] {   1, 0,
                                                    0, 1,
                                                    0, 0 };

        public IconAppearance() { }

        public IconAppearance(IconAppearance appearance) {
            Icon = appearance.Icon;
            IconState = appearance.IconState;
            Direction = appearance.Direction;
            PixelX = appearance.PixelX;
            PixelY = appearance.PixelY;
            Color = appearance.Color;
            Layer = appearance.Layer;
            Invisibility = appearance.Invisibility;
            MouseOpacity = appearance.MouseOpacity;
            Overlays = new List<int>(appearance.Overlays);
            Underlays = new List<int>(appearance.Underlays);

            for (int i = 0; i < 6; i++) {
                Transform[i] = appearance.Transform[i];
            }
        }

        public override bool Equals(object obj) => obj is IconAppearance appearance && Equals(appearance);

        public bool Equals(IconAppearance appearance) {
            if (appearance == null) return false;

            if (appearance.Icon != Icon) return false;
            if (appearance.IconState != IconState) return false;
            if (appearance.Direction != Direction) return false;
            if (appearance.PixelX != PixelX) return false;
            if (appearance.PixelY != PixelY) return false;
            if (appearance.Color != Color) return false;
            if (appearance.Layer != Layer) return false;
            if (appearance.Invisibility != Invisibility) return false;
            if (appearance.MouseOpacity != MouseOpacity) return false;
            if (appearance.Overlays.Count != Overlays.Count) return false;

            for (int i = 0; i < Overlays.Count; i++) {
                if (appearance.Overlays[i] != Overlays[i]) return false;
            }

            for (int i = 0; i < Underlays.Count; i++) {
                if (appearance.Underlays[i] != Underlays[i]) return false;
            }

            for (int i = 0; i < 6; i++) {
                if (appearance.Transform[i] != Transform[i]) return false;
            }

            return true;
        }

        public override int GetHashCode() {
            int hashCode = (Icon + IconState).GetHashCode();
            hashCode += Direction.GetHashCode();
            hashCode += PixelX;
            hashCode += PixelY;
            hashCode += Color.GetHashCode();
            hashCode += Layer.GetHashCode();
            hashCode += Invisibility;
            hashCode += MouseOpacity.GetHashCode();

            foreach (int overlay in Overlays) {
                hashCode += overlay;
            }

            foreach (int underlay in Underlays) {
                hashCode += underlay;
            }

            for (int i = 0; i < 6; i++) {
                hashCode += Transform[i].GetHashCode();
            }

            return hashCode;
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

        private bool IsTransformIdentity() {
            if (Transform[0] != 1.0f) return false;
            if (Transform[1] != 0.0f) return false;
            if (Transform[2] != 0.0f) return false;
            if (Transform[3] != 1.0f) return false;
            if (Transform[4] != 0.0f) return false;
            if (Transform[5] != 0.0f) return false;

            return true;
        }
    }
}
