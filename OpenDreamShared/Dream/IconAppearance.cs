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
        [ViewVariables] public float BlendMode;
        [ViewVariables] public int AppearanceFlags = 0;
        [ViewVariables] public int Invisibility;
        [ViewVariables] public bool Opacity;
        [ViewVariables] public string RenderSource = "";
        [ViewVariables] public string RenderTarget = "";
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
            RenderSource = appearance.RenderSource;
            RenderTarget = appearance.RenderTarget;
            BlendMode = appearance.BlendMode;
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
            if (appearance.RenderSource != RenderSource) return false;
            if (appearance.RenderTarget != RenderTarget) return false;
            if (appearance.BlendMode != BlendMode) return false;
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
            HashCode hashCode = new HashCode();

            hashCode.Add(Icon);
            hashCode.Add(IconState);
            hashCode.Add(Direction);
            hashCode.Add(PixelOffset);
            hashCode.Add(Color);
            hashCode.Add(Layer);
            hashCode.Add(Invisibility);
            hashCode.Add(Opacity);
            hashCode.Add(MouseOpacity);
            hashCode.Add(Alpha);
            hashCode.Add(Plane);
            hashCode.Add(RenderSource);
            hashCode.Add(RenderTarget);
            hashCode.Add(BlendMode);
            hashCode.Add(AppearanceFlags);

            foreach (int overlay in Overlays) {
                hashCode.Add(overlay);
            }

            foreach (int underlay in Underlays) {
                hashCode.Add(underlay);
            }

            foreach (DreamFilter filter in Filters) {
                hashCode.Add(filter);
            }

            for (int i = 0; i < 6; i++) {
                hashCode.Add(Transform[i]);
            }

            return hashCode.ToHashCode();
        }

        public void SetColor(string color) {
            // TODO the BYOND compiler enforces valid colors *unless* it's a map edit, in which case an empty string is allowed
            if (color == string.Empty) color = "#ffffff";
            if (!ColorHelpers.TryParseColor(color, out Color)) {
                throw new ArgumentException($"Invalid color '{color}'");
            }
        }
    }
}
