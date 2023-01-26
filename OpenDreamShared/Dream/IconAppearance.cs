using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;

namespace OpenDreamShared.Dream {
    [Serializable, NetSerializable]
    public sealed class IconAppearance : IEquatable<IconAppearance> {
        [ViewVariables] public int? Icon;
        [ViewVariables] public string IconState;
        [ViewVariables] public AtomDirection Direction;
        [ViewVariables] public Vector2i PixelOffset;
        [ViewVariables] public Color Color = Color.White;
        [ViewVariables] public byte Alpha;
        [ViewVariables] public float Layer;
        [ViewVariables] public float Plane;
        [ViewVariables] public int AppearanceFlags = 0;
        [ViewVariables] public int Invisibility;
        [ViewVariables] public bool Opacity;
        [ViewVariables] public MouseOpacity MouseOpacity = MouseOpacity.PixelOpaque;
        [ViewVariables] public List<uint> Overlays = new();
        [ViewVariables] public List<uint> Underlays = new();
        [ViewVariables] public List<DreamFilter> Filters = new();
        /// <summary> The Transform property of this appearance, in [a,d,b,e,c,f] order</summary>
        [ViewVariables] public float[] Transform = new float[6] {   1, 0,   // a d
                                                                    0, 1,   // b e
                                                                    0, 0 }; // c f

        public IconAppearance() { }

        public IconAppearance(IconAppearance appearance) {
            Icon = appearance.Icon;
            IconState = appearance.IconState;
            Direction = appearance.Direction;
            PixelOffset = appearance.PixelOffset;
            Color = appearance.Color;
            Alpha = appearance.Alpha;
            Layer = appearance.Layer;
            Plane = appearance.Plane;
            AppearanceFlags = appearance.AppearanceFlags;
            Invisibility = appearance.Invisibility;
            Opacity = appearance.Opacity;
            MouseOpacity = appearance.MouseOpacity;
            Overlays = new List<uint>(appearance.Overlays);
            Underlays = new List<uint>(appearance.Underlays);
            Filters = new List<DreamFilter>(appearance.Filters);

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
            if (appearance.Alpha != Alpha) return false;
            if (appearance.Layer != Layer) return false;
            if (appearance.Plane != Plane) return false;
            if (appearance.AppearanceFlags != AppearanceFlags) return false;
            if (appearance.Invisibility != Invisibility) return false;
            if (appearance.Opacity != Opacity) return false;
            if (appearance.MouseOpacity != MouseOpacity) return false;
            if (appearance.Overlays.Count != Overlays.Count) return false;
            if (appearance.Underlays.Count != Underlays.Count) return false;
            if (appearance.Filters.Count != Filters.Count) return false;

            for (int i = 0; i < Filters.Count; i++) {
                if (appearance.Filters[i] != Filters[i]) return false;
            }

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
            hashCode += Alpha.GetHashCode();
            hashCode += Layer.GetHashCode();
            hashCode += Plane.GetHashCode();
            hashCode += AppearanceFlags.GetHashCode();
            hashCode += Invisibility;
            hashCode += Opacity.GetHashCode();
            hashCode += MouseOpacity.GetHashCode();

            foreach (int overlay in Overlays) {
                hashCode += overlay;
            }

            foreach (int underlay in Underlays) {
                hashCode += underlay;
            }

            foreach (DreamFilter filter in Filters) {
                hashCode += filter.GetHashCode();
            }

            for (int i = 0; i < 6; i++) {
                hashCode += Transform[i].GetHashCode();
            }

            return hashCode;
        }

        public void SetColor(string color)
        {
            // TODO the BYOND compiler enforces valid colors *unless* it's a map edit, in which case an empty string is allowed
            if (color == string.Empty) color = "#ffffff";
            if (!ColorHelpers.TryParseColor(color, out Color)) {
                throw new ArgumentException($"Invalid color '{color}'");
            }
        }
    }
}
