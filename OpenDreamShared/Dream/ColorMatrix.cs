using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using System.Diagnostics.CodeAnalysis;

namespace OpenDreamShared.Dream;

/// <summary>
/// Holds the 5x4 matrix data necessary to encapsulate a color matrix: https://www.byond.com/docs/ref/#/{notes}/color-matrix
/// </summary>
/// <remarks>
/// This is going to be one of those structs that gets, absolutely destroyed by the fact <br/>
/// that fixed arrays are """"""unsafe"""""" in this language.
/// </remarks>
[Serializable, NetSerializable, StructLayout(LayoutKind.Sequential)]
public struct ColorMatrix(
    float m11, float m12, float m13, float m14,
    float m21, float m22, float m23, float m24,
    float m31, float m32, float m33, float m34,
    float m41, float m42, float m43, float m44,
    float m51, float m52, float m53, float m54) {
    /// <summary>Red -> Red</summary>
    public float rr = m11;
    /// <summary>Red -> Green</summary>
    public float rg = m12;
    /// <summary>Red -> Blue</summary>
    public float rb = m13;
    /// <summary>Red -> Alpha</summary>
    public float ra = m14;

    /// <summary>Green -> Red</summary>
    public float gr = m21;
    /// <summary>Green -> Green</summary>
    public float gg = m22;
    /// <summary>Green -> Blue</summary>
    public float gb = m23;
    /// <summary>Green -> Alpha</summary>
    public float ga = m24;

    /// <summary>Blue -> Red</summary>
    public float br = m31;
    /// <summary>Blue -> Green</summary>
    public float bg = m32;
    /// <summary>Blue -> Blue</summary>
    public float bb = m33;
    /// <summary>Blue -> Alpha</summary>
    public float ba = m34;

    /// <summary>Alpha -> Red</summary>
    public float ar = m41;
    /// <summary>Alpha -> Green</summary>
    public float ag = m42;
    /// <summary>Alpha -> Blue</summary>
    public float ab = m43;
    /// <summary>Alpha -> Alpha</summary>
    public float aa = m44;

    /// <summary>Additional Red.</summary>
    public float cr = m51;
    /// <summary>Additional Green.</summary>
    public float cg = m52;
    /// <summary>Additional Blue.</summary>
    public float cb = m53;
    /// <summary>Additional Alpha.</summary>
    public float ca = m54;

    public ColorMatrix(in ColorMatrix cloned)
        //I have never, ever missed the "pointer to member access" goofball operator from C++
        //until this exact, debilitating moment
        : this(
            cloned.rr, cloned.rg, cloned.rb, cloned.ra,
            cloned.gr, cloned.gg, cloned.gb, cloned.ga,
            cloned.br, cloned.bg, cloned.bb, cloned.ba,
            cloned.ar, cloned.ag, cloned.ab, cloned.aa,
            cloned.cr, cloned.cg, cloned.cb, cloned.ca) {}

    /// <summary>
    /// Constructs a ColorMatrix where the diagonal (main color) values are assigned to RGBA format
    /// </summary>
    public ColorMatrix(float r, float g, float b, float a = 1)
        : this(
                r, 0, 0, 0,
                0, g, 0, 0,
                0, 0, b, 0,
                0, 0, 0, a,
                0, 0, 0, 0) {}

    /// <summary>
    /// Constructs a ColorMatrix that is equivalent to the given color, during transformations.
    /// </summary>
    /// <remarks>Note: This constructor assumes that floats are zero-initialized.</remarks>
    /// <param name="basicColor"></param>
    public ColorMatrix(in Color basicColor)
        : this(
            basicColor.R, 0, 0, 0,
            0, basicColor.G, 0, 0,
            0, 0, basicColor.B, 0,
            0, 0, 0, basicColor.A,
            0, 0, 0, 0) {}

    /// <summary>
    /// The identity matrix. The equivalent would be <see cref="Color.White"/>.
    /// </summary>
    /// <seealso cref="Color"/>
    public static ColorMatrix Identity
        => new(
            1F, 0F, 0F, 0F,
            0F, 1F, 0F, 0F,
            0F, 0F, 1F, 0F,
            0F, 0F, 0F, 1F,
            0F, 0F, 0F, 0F);

    public void SetRow(int row, in Color color) => SetRow(row, color.R, color.G, color.B, color.A);

    public void SetRow(int row, float r, float g, float b, float a) {
        switch(row) {
            case 0:
                rr = r;
                rg = g;
                rb = b;
                ra = a;
                break;
            case 1:
                gr = r;
                gg = g;
                gb = b;
                ga = a;
                break;
            case 2:
                br = r;
                bg = g;
                bb = b;
                ba = a;
                break;
            case 3:
                ar = r;
                ag = g;
                ab = b;
                aa = a;
                break;
            case 4:
                cr = r;
                cg = g;
                cb = b;
                ca = a;
                break;
            default:
                //Should be an UnreachableException but it's verbotten or something by the sandboxer
                throw new Exception($"Cannot access {row}th row of a 5x4 matrix");
        }
    }

    /// <summary>
    /// Gets the diagonal values in this matrix. Used for detecting whether this matrix is convertible into a Color.
    /// </summary>
    [Pure]
    public readonly IEnumerable<float> EnumerateDiagonal() {
        yield return rr;
        yield return gg;
        yield return bb;
        yield return aa;
    }

    /// <summary>
    /// Returns all of the values in this struct, in order.
    /// </summary>
    [Pure]
    public readonly IEnumerable<float> EnumerateValues() {
        yield return rr;
        yield return rg;
        yield return rb;
        yield return ra;

        yield return gr;
        yield return gg;
        yield return gb;
        yield return ga;

        yield return br;
        yield return bg;
        yield return bb;
        yield return ba;

        yield return ar;
        yield return ag;
        yield return ab;
        yield return aa;

        yield return cr;
        yield return cg;
        yield return cb;
        yield return ca;
    }

    public readonly Matrix4x4 GetMatrix4()
        => new(
            rr, rg, rb, ra,
            gr, gg, gb, ga,
            br, bg, bb, ba,
            ar, ag, ab, aa);

    public readonly Vector4 GetOffsetVector()
        => new (cr, cg, cb, ca);

    // This method pretty much only exists as a placeholder,
    // all of its uses probably have a more correct alternative
    public readonly Color AsRgbaColor()
        => TryRepresentAsRGBAColor(in this, out var maybeColor) ? maybeColor.Value : Color.White;

    /// <summary>
    /// Fastest possible comparison between two color matrices.
    /// </summary>
    /// <remarks>
    /// STRONGLY prefer using this over <see cref="ValueType.Equals(object?)"/> if at all possible, <br/>
    /// since that (default) method actually does a lot of boxing, which causes LUDICROUS memory churning when running targets. <br/><br/>
    ///
    /// This method avoids implementing <see cref="IEquatable{T}"/> since that would make the argument be copied - <br/>
    /// the argument in that interface lacks an 'in' modifier and one cannot be provided!
    /// </remarks>
    [Pure]
    public readonly bool Equals(in ColorMatrix other) {
        //there is currently no kosher, "safe" C# way
        //of doing a fast-path pointer compare here.
        //(ReferenceEquals actually boxes structs just like default Equals)
        //so this pretty much MUST be a long elementwise compare on all elements.
        return rr.Equals(other.rr) && rg.Equals(other.rg) && rb.Equals(other.rb) && ra.Equals(other.ra) &&
               gr.Equals(other.gr) && gg.Equals(other.gg) && gb.Equals(other.gb) && ga.Equals(other.ga) &&
               br.Equals(other.br) && bg.Equals(other.bg) && bb.Equals(other.bb) && ba.Equals(other.ba) &&
               ar.Equals(other.ar) && ag.Equals(other.ag) && ab.Equals(other.ab) && aa.Equals(other.aa) &&
               cr.Equals(other.cr) && cg.Equals(other.cg) && cb.Equals(other.cb) && ca.Equals(other.ca);
    }

    public override int GetHashCode() {
        HashCode hashCode = new HashCode();
        hashCode.Add(rr);
        hashCode.Add(rg);
        hashCode.Add(rb);
        hashCode.Add(ra);

        hashCode.Add(gr);
        hashCode.Add(gg);
        hashCode.Add(gb);
        hashCode.Add(ga);

        hashCode.Add(br);
        hashCode.Add(bg);
        hashCode.Add(bb);
        hashCode.Add(ba);

        hashCode.Add(ar);
        hashCode.Add(ag);
        hashCode.Add(ab);
        hashCode.Add(aa);

        hashCode.Add(cr);
        hashCode.Add(cg);
        hashCode.Add(cb);
        hashCode.Add(ca);

        return hashCode.ToHashCode();
    }

    /// <summary>
    /// Multiplies two instances.
    /// </summary>
    /// <param name="left">The left operand of the multiplication.</param>
    /// <param name="right">The right operand of the multiplication.</param>
    /// <param name="result">A new instance that is the result of the multiplication</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Multiply(ref readonly ColorMatrix left, ref readonly ColorMatrix right, out ColorMatrix result) {
        float
            l_rr = left.rr,
            l_rg = left.rg,
            l_rb = left.rb,
            l_ra = left.ra,
            l_gr = left.gr,
            l_gg = left.gg,
            l_gb = left.gb,
            l_ga = left.ga,
            l_br = left.br,
            l_bg = left.bg,
            l_bb = left.bb,
            l_ba = left.ba,
            l_ar = left.ar,
            l_ag = left.ag,
            l_ab = left.ab,
            l_aa = left.aa;
        float
            r_rr = right.rr,
            r_rg = right.rg,
            r_rb = right.rb,
            r_ra = right.ra,
            r_gr = right.gr,
            r_gg = right.gg,
            r_gb = right.gb,
            r_ga = right.ga,
            r_br = right.br,
            r_bg = right.bg,
            r_bb = right.bb,
            r_ba = right.ba,
            r_ar = right.ar,
            r_ag = right.ag,
            r_ab = right.ab,
            r_aa = right.aa;

        result = new() {
            rr = l_rr * r_rr + l_rg * r_gr + l_rb * r_br + l_ra * r_ar,
            rg = l_rr * r_rg + l_rg * r_gg + l_rb * r_bg + l_ra * r_ag,
            rb = l_rr * r_rb + l_rg * r_gb + l_rb * r_bb + l_ra * r_ab,
            ra = l_rr * r_ra + l_rg * r_ga + l_rb * r_ba + l_ra * r_aa,
            gr = l_gr * r_rr + l_gg * r_gr + l_gb * r_br + l_ga * r_ar,
            gg = l_gr * r_rg + l_gg * r_gg + l_gb * r_bg + l_ga * r_ag,
            gb = l_gr * r_rb + l_gg * r_gb + l_gb * r_bb + l_ga * r_ab,
            ga = l_gr * r_ra + l_gg * r_ga + l_gb * r_ba + l_ga * r_aa,
            br = l_br * r_rr + l_bg * r_gr + l_bb * r_br + l_ba * r_ar,
            bg = l_br * r_rg + l_bg * r_gg + l_bb * r_bg + l_ba * r_ag,
            bb = l_br * r_rb + l_bg * r_gb + l_bb * r_bb + l_ba * r_ab,
            ba = l_br * r_ra + l_bg * r_ga + l_bb * r_ba + l_ba * r_aa,
            ar = l_ar * r_rr + l_ag * r_gr + l_ab * r_br + l_aa * r_ar,
            ag = l_ar * r_rg + l_ag * r_gg + l_ab * r_bg + l_aa * r_ag,
            ab = l_ar * r_rb + l_ag * r_gb + l_ab * r_bb + l_aa * r_ab,
            aa = l_ar * r_ra + l_ag * r_ga + l_ab * r_ba + l_aa * r_aa
        };
    }

    /// <summary>
    /// Linearly interpolates between two instances.
    /// </summary>
    /// <param name="left">The left operand of the interpolation.</param>
    /// <param name="right">The right operand of the interpolation.</param>
    /// <param name="factor">The amount to interpolate between them. 0..1 is equivalent to left..right.</param>
    /// <param name="result">A new instance that is the result of the interpolation</param>
    public static void Interpolate(ref readonly ColorMatrix left, ref readonly ColorMatrix right, float factor, out ColorMatrix result) {
        result = new ColorMatrix(
                    ((1-factor) * left.rr) + (factor * right.rr),
                    ((1-factor) * left.rg) + (factor * right.rg),
                    ((1-factor) * left.rb) + (factor * right.rb),
                    ((1-factor) * left.ra) + (factor * right.ra),
                    ((1-factor) * left.gr) + (factor * right.gr),
                    ((1-factor) * left.gg) + (factor * right.gg),
                    ((1-factor) * left.gb) + (factor * right.gb),
                    ((1-factor) * left.ga) + (factor * right.ga),
                    ((1-factor) * left.br) + (factor * right.br),
                    ((1-factor) * left.bg) + (factor * right.bg),
                    ((1-factor) * left.bb) + (factor * right.bb),
                    ((1-factor) * left.ba) + (factor * right.ba),
                    ((1-factor) * left.ar) + (factor * right.ar),
                    ((1-factor) * left.ag) + (factor * right.ag),
                    ((1-factor) * left.ab) + (factor * right.ab),
                    ((1-factor) * left.aa) + (factor * right.aa),
                    ((1-factor) * left.cr) + (factor * right.cr),
                    ((1-factor) * left.cg) + (factor * right.cg),
                    ((1-factor) * left.cb) + (factor * right.cb),
                    ((1-factor) * left.ca) + (factor * right.ca)
                );
    }

    public static bool TryRepresentAsRGBAColor(in ColorMatrix matrix, [NotNullWhen(true)] out Color? maybeColor) {
        maybeColor = null;

        // The R G B A values need to be bounded [0,1] for a color conversion to work;
        // anything higher implies trying to render "superblue" or something.
        float diagonalSum = 0f;
        foreach (float diagonalValue in matrix.EnumerateDiagonal()) {
            if (diagonalValue < 0 || diagonalValue > 1)
                return false;
            diagonalSum += diagonalValue;
        }

        // and then all of the other values need to be zero, including the offset vector.
        float sum = 0f;
        foreach (float value in matrix.EnumerateValues()) {
            if (value < 0f) // To avoid situations like negatives and positives cancelling out this checksum.
                return false;
            sum += value;
        }

        if (sum - diagonalSum == 0) // PREEETTY sure I can trust the floating-point math here. Not 100% though
            maybeColor = new Color(matrix.rr, matrix.gg, matrix.bb, matrix.aa);
        return maybeColor is not null;
    }
}
