﻿using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace OpenDreamShared.Dream {
    [Serializable, NetSerializable]
    public sealed class IconAppearance : IEquatable<IconAppearance> {
        [ViewVariables] public int? Icon;
        [ViewVariables] public string IconState;
        [ViewVariables] public AtomDirection Direction;
        [ViewVariables] public Vector2i PixelOffset;
        [ViewVariables] public Color Color = Color.White;
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
        [ViewVariables] public DreamFilterColor? SillyColorFilter;
        [ViewVariables] public float Layer;
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
            SillyColorFilter = appearance.SillyColorFilter;
            Layer = appearance.Layer;
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
            if (appearance.SillyColorFilter != SillyColorFilter) return false;
            if (appearance.Layer != Layer) return false;
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

        public void RemoveColorFilter() {
            for (int i = 0; i < Filters.Count; i++) {
                if (Filters[i] is DreamFilterColor) {
                    Filters.RemoveAt(i);
                    return;
                }
            }
        }

        /// <summary>
        /// Iterates over everything in <see cref="Filters"/>, plus <see cref="SillyColorFilter"/> if it's there, bein silly.
        /// </summary>
        public IEnumerable<DreamFilter> GetAllFilters() {
            if (SillyColorFilter is not null)
                yield return SillyColorFilter;
            foreach (DreamFilter filter in Filters) {
                yield return filter;
            }
        }

        public override int GetHashCode() {
            HashCode hashCode = new HashCode();

            hashCode.Add(Icon);
            hashCode.Add(IconState);
            hashCode.Add(Direction);
            hashCode.Add(PixelOffset);
            hashCode.Add(Color);
            hashCode.Add(SillyColorFilter);
            hashCode.Add(Layer);
            hashCode.Add(Invisibility);
            hashCode.Add(Opacity);
            hashCode.Add(MouseOpacity);

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

        /// <summary>
        /// Parses the given colour string and sets this appearance to use it.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if color is not valid.</exception>
        public void SetColor(string color) {
            // TODO: the BYOND compiler enforces valid colors *unless* it's a map edit, in which case an empty string is allowed
            if (SillyColorFilter is not null) {
                SillyColorFilter = null; // reset our color matrix if we had one
            }
            RemoveColorFilter();
            if (color == string.Empty) {
                Color = Color.White;
                return;
            }
            if (!ColorHelpers.TryParseColor(color, out Color)) {
                throw new ArgumentException($"Invalid color '{color}'");
            }
        }
        /// <summary>
        /// Sets the 'color' attribute to a color matrix, which will be used on the icon later on by a shader.
        /// </summary>
        public void SetColor(in ColorMatrix matrix) {
            RemoveColorFilter();
            if (TryRepresentMatrixAsRGBAColor(matrix, out var matrixColor)) {
                Color = matrixColor.Value;
                SillyColorFilter = null;
                return;
            }
            Color = Color.White;
            if (SillyColorFilter is not null) { // If we already had a matrix!
                SillyColorFilter.Color = matrix;
                return;
            }
            SillyColorFilter = new DreamFilterColor { // Am I like... doing this right?
                Color = matrix,
                Space = 0, // TODO: Support color mappings that aren't RGB.
                FilterType = "color"
            };
        }
    }
}
