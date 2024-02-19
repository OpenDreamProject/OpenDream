using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;

namespace OpenDreamShared.Dream {
    // TODO: Wow this is huge! Probably look into splitting this by most used/least used to reduce the size of these
    [Serializable, NetSerializable]
    public sealed class IconAppearance : IEquatable<IconAppearance> {
        [ViewVariables] public int? Icon;
        [ViewVariables] public string? IconState;
        [ViewVariables] public AtomDirection Direction = AtomDirection.South;
        [ViewVariables] public bool InheritsDirection = true; // Inherits direction when used as an overlay
        [ViewVariables] public Vector2i PixelOffset;
        [ViewVariables] public Color Color = Color.White;
        [ViewVariables] public byte Alpha = 255;
        [ViewVariables] public float GlideSize;
        /// <summary>
        /// An appearance can gain a color matrix filter by two possible forces: <br/>
        /// 1. the /atom.color var is modified. <br/>
        /// 2. the /atom.filters var gets a new filter of type "color". <br/>
        /// DM crashes in some circumstances of this but we, as an extension :^), should try not to. <br/>
        /// So, this exists as a way for the appearance to remember whether it's coloured by .color, specifically.
        /// </summary>
        /// <remarks>
        /// The reason we don't just take the slow path and always use this filter is not just for optimization,<br/>
        /// it's also for parity! See <see cref="TryRepresentMatrixAsRGBAColor(in ColorMatrix, out Color?)"/> for more.
        /// </remarks>
        [ViewVariables] public ColorMatrix ColorMatrix = ColorMatrix.Identity;
        [ViewVariables] public float Layer = -1f;
        [ViewVariables] public int Plane = -32767;
        [ViewVariables] public BlendMode BlendMode = BlendMode.Default;
        [ViewVariables] public AppearanceFlags AppearanceFlags = AppearanceFlags.None;
        [ViewVariables] public int Invisibility;
        [ViewVariables] public bool Opacity;
        [ViewVariables] public bool Override;
        [ViewVariables] public string? RenderSource;
        [ViewVariables] public string? RenderTarget;
        [ViewVariables] public MouseOpacity MouseOpacity = MouseOpacity.PixelOpaque;
        [ViewVariables] public List<int> Overlays = new();
        [ViewVariables] public List<int> Underlays = new();
        [ViewVariables] public List<NetEntity> VisContents = new();
        [ViewVariables] public List<DreamFilter> Filters = new();
        [ViewVariables] public List<int> Verbs = new();
        /// <summary> The Transform property of this appearance, in [a,d,b,e,c,f] order</summary>
        [ViewVariables] public float[] Transform = [
            1, 0,   // a d
            0, 1,   // b e
            0, 0    // c f
        ];

        public IconAppearance() { }

        public IconAppearance(IconAppearance appearance) {
            Icon = appearance.Icon;
            IconState = appearance.IconState;
            Direction = appearance.Direction;
            InheritsDirection = appearance.InheritsDirection;
            PixelOffset = appearance.PixelOffset;
            Color = appearance.Color;
            Alpha = appearance.Alpha;
            GlideSize = appearance.GlideSize;
            ColorMatrix = appearance.ColorMatrix;
            Layer = appearance.Layer;
            Plane = appearance.Plane;
            RenderSource = appearance.RenderSource;
            RenderTarget = appearance.RenderTarget;
            BlendMode = appearance.BlendMode;
            AppearanceFlags = appearance.AppearanceFlags;
            Invisibility = appearance.Invisibility;
            Opacity = appearance.Opacity;
            MouseOpacity = appearance.MouseOpacity;
            Overlays = new(appearance.Overlays);
            Underlays = new(appearance.Underlays);
            VisContents = new(appearance.VisContents);
            Filters = new(appearance.Filters);
            Verbs = new(appearance.Verbs);
            Override = appearance.Override;

            for (int i = 0; i < 6; i++) {
                Transform[i] = appearance.Transform[i];
            }
        }

        public override bool Equals(object? obj) => obj is IconAppearance appearance && Equals(appearance);

        public bool Equals(IconAppearance? appearance) {
            if (appearance == null) return false;

            if (appearance.Icon != Icon) return false;
            if (appearance.IconState != IconState) return false;
            if (appearance.Direction != Direction) return false;
            if (appearance.InheritsDirection != InheritsDirection) return false;
            if (appearance.PixelOffset != PixelOffset) return false;
            if (appearance.Color != Color) return false;
            if (appearance.Alpha != Alpha) return false;
            if (appearance.GlideSize != GlideSize) return false;
            if (!appearance.ColorMatrix.Equals(ColorMatrix)) return false;
            if (!appearance.Layer.Equals(Layer)) return false;
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
            if (appearance.VisContents.Count != VisContents.Count) return false;
            if (appearance.Filters.Count != Filters.Count) return false;
            if (appearance.Verbs.Count != Verbs.Count) return false;
            if (appearance.Override != Override) return false;

            for (int i = 0; i < Filters.Count; i++) {
                if (appearance.Filters[i] != Filters[i]) return false;
            }

            for (int i = 0; i < Overlays.Count; i++) {
                if (appearance.Overlays[i] != Overlays[i]) return false;
            }

            for (int i = 0; i < Underlays.Count; i++) {
                if (appearance.Underlays[i] != Underlays[i]) return false;
            }

            for (int i = 0; i < VisContents.Count; i++) {
                if (appearance.VisContents[i] != VisContents[i]) return false;
            }

            for (int i = 0; i < Verbs.Count; i++) {
                if (appearance.Verbs[i] != Verbs[i]) return false;
            }

            for (int i = 0; i < 6; i++) {
                if (!appearance.Transform[i].Equals(Transform[i])) return false;
            }

            return true;
        }

        /// <summary>
        /// This is a helper used for both optimization and parity. <br/>
        /// In BYOND, if a color matrix is representable as an RGBA color string, <br/>
        /// then it is coerced into one internally before being saved onto some appearance. <br/>
        /// This does the linear algebra madness necessary to determine whether this is the case or not.
        /// </summary>
        private static bool TryRepresentMatrixAsRGBAColor(in ColorMatrix matrix, [NotNullWhen(true)] out Color? maybeColor) {
            maybeColor = null;
            // The R G B A values need to be bounded [0,1] for a color conversion to work;
            // anything higher implies trying to render "superblue" or something.
            float diagonalSum = 0f;
            foreach (float diagonalValue in matrix.GetDiagonal()) {
                if (diagonalValue < 0 || diagonalValue > 1)
                    return false;
                diagonalSum += diagonalValue;
            }
            // and then all of the other values need to be zero, including the offset vector.
            float sum = 0f;
            foreach (float value in matrix.GetValues()) {
                if (value < 0f) // To avoid situations like negatives and positives cancelling out this checksum.
                    return false;
                sum += value;
            }
            if (sum - diagonalSum == 0) // PREEETTY sure I can trust the floating-point math here. Not 100% though
                maybeColor = new Color(matrix.c11, matrix.c22, matrix.c33, matrix.c44);
            return maybeColor is not null;
        }

        public override int GetHashCode() {
            HashCode hashCode = new HashCode();

            hashCode.Add(Icon);
            hashCode.Add(IconState);
            hashCode.Add(Direction);
            hashCode.Add(InheritsDirection);
            hashCode.Add(PixelOffset);
            hashCode.Add(Color);
            hashCode.Add(ColorMatrix);
            hashCode.Add(Layer);
            hashCode.Add(Invisibility);
            hashCode.Add(Opacity);
            hashCode.Add(MouseOpacity);
            hashCode.Add(Alpha);
            hashCode.Add(GlideSize);
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

            foreach (int visContent in VisContents) {
                hashCode.Add(visContent);
            }

            foreach (DreamFilter filter in Filters) {
                hashCode.Add(filter);
            }

            foreach (int verb in Verbs) {
                hashCode.Add(verb);
            }

            for (int i = 0; i < 6; i++) {
                hashCode.Add(Transform[i]);
            }

            return hashCode.ToHashCode();
        }

        /// <summary>
        /// Parses the given colour string and sets this appearance to use it.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if color is not valid.</exception>
        public void SetColor(string color) {
            // TODO: the BYOND compiler enforces valid colors *unless* it's a map edit, in which case an empty string is allowed
            ColorMatrix = ColorMatrix.Identity; // reset our color matrix if we had one

            if (!ColorHelpers.TryParseColor(color, out Color)) {
                Color = Color.White;
            }
        }
        /// <summary>
        /// Sets the 'color' attribute to a color matrix, which will be used on the icon later on by a shader.
        /// </summary>
        public void SetColor(in ColorMatrix matrix) {

            if (TryRepresentMatrixAsRGBAColor(matrix, out var matrixColor)) {
                Color = matrixColor.Value;
                ColorMatrix = ColorMatrix.Identity;
                return;
            }
            Color = Color.White;
            ColorMatrix = matrix;
        }
    }

    public enum BlendMode {
        Default,
        Overlay,
        Add,
        Subtract,
        Multiply,
        InsertOverlay
    }

    [Flags]
    public enum AppearanceFlags {
        None = 0,
        LongGlide = 1,
        ResetColor = 2,
        ResetAlpha = 4,
        ResetTransform = 8,
        NoClientColor = 16,
        KeepTogether = 32,
        KeepApart = 64,
        PlaneMaster = 128,
        TileBound = 256,
        PixelScale = 512,
        PassMouse = 1024,
        TileMover = 2048
    }

    [Flags] //kinda, but only EASE_IN and EASE_OUT are used as bitflags, everything else is an enum
    public enum AnimationEasing {
        Linear = 0,
        Sine = 1,
        Circular = 2,
        Cubic = 3,
        Bounce = 4,
        Elastic = 5,
        Back = 6,
        Quad = 7,
        Jump = 8,
        Ease_In = 64,
        Ease_Out = 128,
    }
}
