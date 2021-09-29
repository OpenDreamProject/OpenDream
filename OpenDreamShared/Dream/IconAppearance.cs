using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;

namespace OpenDreamShared.Dream {
    [Serializable, NetSerializable]
    public class IconAppearance : IEquatable<IconAppearance> {
        public static readonly Dictionary<String, Color> Colors = new() {
            { "black", new Color(00, 00, 00) },
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

        public uint Id;
        public ResourcePath Icon;
        public string IconState;
        public AtomDirection Direction;
        public Vector2i PixelOffset;
        public Color Color = Color.White;
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
            PixelOffset = appearance.PixelOffset;
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
            if (appearance.PixelOffset != PixelOffset) return false;
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
            hashCode += PixelOffset.GetHashCode();
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

                Color = Color.FromHex(color, Color.White);
            } else if (!Colors.TryGetValue(color.ToLower(), out Color)) {
                throw new ArgumentException("Invalid color '" + color + "'");
            }
        }
    }
}
