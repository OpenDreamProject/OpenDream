using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace OpenDreamShared.Dream;
/// <summary>
/// Holds the 5x4 matrix data necessary to encapsulate a color matrix: https://www.byond.com/docs/ref/#/{notes}/color-matrix
/// </summary>
/// <remarks>
/// This is going to be one of those structs that gets, absolutely destroyed by the fact <br/>
/// that fixed arrays are """"""unsafe"""""" in this language.
/// </remarks>
[Serializable, NetSerializable, StructLayout(LayoutKind.Sequential)]
public struct ColorMatrix {
    public float c11;
    public float c12;
    public float c13;
    public float c14;

    public float c21;
    public float c22;
    public float c23;
    public float c24;

    public float c31;
    public float c32;
    public float c33;
    public float c34;

    public float c41;
    public float c42;
    public float c43;
    public float c44;

    public float c51;
    public float c52;
    public float c53;
    public float c54;

    public ColorMatrix(float m11, float m12, float m13, float m14,
                        float m21, float m22, float m23, float m24,
                        float m31, float m32, float m33, float m34,
                        float m41, float m42, float m43, float m44,
                        float m51, float m52, float m53, float m54) {
        c11 = m11;
        c12 = m12;
        c13 = m13;
        c14 = m14;

        c21 = m21;
        c22 = m22;
        c23 = m23;
        c24 = m24;

        c31 = m31;
        c32 = m32;
        c33 = m33;
        c34 = m34;

        c41 = m41;
        c42 = m42;
        c43 = m43;
        c44 = m44;

        c51 = m51;
        c52 = m52;
        c53 = m53;
        c54 = m54;
    }

    public ColorMatrix(in ColorMatrix cloned) {
        //I have never, ever missed the "pointer to member access" goofball operator from C++
        //until this exact, debilitating moment
        c11 = cloned.c11;
        c12 = cloned.c12;
        c13 = cloned.c13;
        c14 = cloned.c14;

        c21 = cloned.c21;
        c22 = cloned.c22;
        c23 = cloned.c23;
        c24 = cloned.c24;

        c31 = cloned.c31;
        c32 = cloned.c32;
        c33 = cloned.c33;
        c34 = cloned.c34;

        c41 = cloned.c41;
        c42 = cloned.c42;
        c43 = cloned.c43;
        c44 = cloned.c44;

        c51 = cloned.c51;
        c52 = cloned.c52;
        c53 = cloned.c53;
        c54 = cloned.c54;
    }

    /// <summary>
    /// Constructs a ColorMatrix that is equivalent to the given color, during transformations.
    /// </summary>
    /// <remarks>Note: This constructor assumes that floats are zero-initialized.</remarks>
    /// <param name="basicColor"></param>
    public ColorMatrix(in Color basicColor) {
        c11 = basicColor.R;

        c22 = basicColor.G;

        c33 = basicColor.B;

        c44 = basicColor.A;
    }

    public static ColorMatrix Identity =>
        new ColorMatrix(1F, 0F, 0F, 0F,
                        0F, 1F, 0F, 0F,
                        0F, 0F, 1F, 0F,
                        0F, 0F, 0F, 1F,
                        0F, 0F, 0F, 0F);

    public void SetRow(int row, in Color color) {
        SetRow(row, color.R, color.G, color.B, color.A);
    }

    public void SetRow(int row, float r, float g, float b, float a) {
        switch(row) {
            case 0:
                c11 = r;
                c12 = g;
                c13 = b;
                c14 = a;
                break;
            case 1:
                c21 = r;
                c22 = g;
                c23 = b;
                c24 = a;
                break;
            case 2:
                c31 = r;
                c32 = g;
                c33 = b;
                c34 = a;
                break;
            case 3:
                c41 = r;
                c42 = g;
                c43 = b;
                c44 = a;
                break;
            case 4:
                c51 = r;
                c52 = g;
                c53 = b;
                c54 = a;
                break;
            default:
                //Should be an UnreachableException but it's verbotten or something by the sandboxer
                throw new Exception($"Cannot access {row}th row of a 5x4 matrix");
        }
    }

    /// <summary>
    /// Gets the diagonal values in this matrix. Used for detecting whether this matrix is convertible into a Color.
    /// </summary>
    /// <returns></returns>
    [Pure]
    public IEnumerable<float> GetDiagonal() {
        yield return c11;
        yield return c22;
        yield return c33;
        yield return c44;
    }

    /// <summary>
    /// Returns all of the values in this struct, in order.
    /// </summary>
    [Pure]
    public IEnumerable<float> GetValues() {
        yield return c11;
        yield return c12;
        yield return c13;
        yield return c14;

        yield return c21;
        yield return c22;
        yield return c23;
        yield return c24;

        yield return c31;
        yield return c32;
        yield return c33;
        yield return c34;

        yield return c41;
        yield return c42;
        yield return c43;
        yield return c44;

        yield return c51;
        yield return c52;
        yield return c53;
        yield return c54;
        yield break;
    }

    public Matrix4 GetMatrix4(){
        return new Matrix4(
            c11, c12, c13, c14,
            c21, c22, c23, c24,
            c31, c32, c33, c34,
            c41, c42, c43, c44
        );
    }

    public Vector4 GetOffsetVector(){
        return new Vector4(c51, c52, c53, c54);
    }

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
    public bool Equals(in ColorMatrix other) {
        //there is currently no kosher, "safe" C# way
        //of doing a fast-path pointer compare here.
        //(ReferenceEquals actually boxes structs just like default Equals)
        //so this pretty much MUST be a long elementwise compare on all elements.
        return c11 == other.c11 &&
               c12 == other.c12 &&
               c13 == other.c13 &&
               c14 == other.c14 &&

               c21 == other.c21 &&
               c22 == other.c22 &&
               c23 == other.c23 &&
               c24 == other.c24 &&

               c31 == other.c31 &&
               c32 == other.c32 &&
               c33 == other.c33 &&
               c34 == other.c34 &&

               c41 == other.c41 &&
               c42 == other.c42 &&
               c43 == other.c43 &&
               c44 == other.c44 &&

               c51 == other.c51 &&
               c52 == other.c52 &&
               c53 == other.c53 &&
               c54 == other.c54;
    }

    /// <summary>
    /// Multiplies two instances.
    /// </summary>
    /// <param name="left">The left operand of the multiplication.</param>
    /// <param name="right">The right operand of the multiplication.</param>
    /// <param name="result">A new instance that is the result of the multiplication</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Multiply(ref ColorMatrix left, ref ColorMatrix right, out ColorMatrix result) {
        float lM11 = left.c11,
            lM12 = left.c12,
            lM13 = left.c13,
            lM14 = left.c14,
            lM21 = left.c21,
            lM22 = left.c22,
            lM23 = left.c23,
            lM24 = left.c24,
            lM31 = left.c31,
            lM32 = left.c32,
            lM33 = left.c33,
            lM34 = left.c34,
            lM41 = left.c41,
            lM42 = left.c42,
            lM43 = left.c43,
            lM44 = left.c44,
            rM11 = right.c11,
            rM12 = right.c12,
            rM13 = right.c13,
            rM14 = right.c14,
            rM21 = right.c21,
            rM22 = right.c22,
            rM23 = right.c23,
            rM24 = right.c24,
            rM31 = right.c31,
            rM32 = right.c32,
            rM33 = right.c33,
            rM34 = right.c34,
            rM41 = right.c41,
            rM42 = right.c42,
            rM43 = right.c43,
            rM44 = right.c44;

        result = new() {
            c11 = lM11 * rM11 + lM12 * rM21 + lM13 * rM31 + lM14 * rM41,
            c12 = lM11 * rM12 + lM12 * rM22 + lM13 * rM32 + lM14 * rM42,
            c13 = lM11 * rM13 + lM12 * rM23 + lM13 * rM33 + lM14 * rM43,
            c14 = lM11 * rM14 + lM12 * rM24 + lM13 * rM34 + lM14 * rM44,
            c21 = lM21 * rM11 + lM22 * rM21 + lM23 * rM31 + lM24 * rM41,
            c22 = lM21 * rM12 + lM22 * rM22 + lM23 * rM32 + lM24 * rM42,
            c23 = lM21 * rM13 + lM22 * rM23 + lM23 * rM33 + lM24 * rM43,
            c24 = lM21 * rM14 + lM22 * rM24 + lM23 * rM34 + lM24 * rM44,
            c31 = lM31 * rM11 + lM32 * rM21 + lM33 * rM31 + lM34 * rM41,
            c32 = lM31 * rM12 + lM32 * rM22 + lM33 * rM32 + lM34 * rM42,
            c33 = lM31 * rM13 + lM32 * rM23 + lM33 * rM33 + lM34 * rM43,
            c34 = lM31 * rM14 + lM32 * rM24 + lM33 * rM34 + lM34 * rM44,
            c41 = lM41 * rM11 + lM42 * rM21 + lM43 * rM31 + lM44 * rM41,
            c42 = lM41 * rM12 + lM42 * rM22 + lM43 * rM32 + lM44 * rM42,
            c43 = lM41 * rM13 + lM42 * rM23 + lM43 * rM33 + lM44 * rM43,
            c44 = lM41 * rM14 + lM42 * rM24 + lM43 * rM34 + lM44 * rM44
        };
    }

    /// <summary>
    /// Linearly interpolates between two instances.
    /// </summary>
    /// <param name="left">The left operand of the interpolation.</param>
    /// <param name="right">The right operand of the interpolation.</param>
    /// <param name="factor">The amount to interpolate between them. 0..1 is equivalent to left..right.</param>
    /// <param name="result">A new instance that is the result of the interpolation</param>
    public static void Interpolate(ref ColorMatrix left, ref ColorMatrix right, float factor, out ColorMatrix result) {
        result = new ColorMatrix(
                    ((1-factor) * left.c11) + (factor * right.c11),
                    ((1-factor) * left.c12) + (factor * right.c12),
                    ((1-factor) * left.c13) + (factor * right.c13),
                    ((1-factor) * left.c14) + (factor * right.c14),
                    ((1-factor) * left.c21) + (factor * right.c21),
                    ((1-factor) * left.c22) + (factor * right.c22),
                    ((1-factor) * left.c23) + (factor * right.c23),
                    ((1-factor) * left.c24) + (factor * right.c24),
                    ((1-factor) * left.c31) + (factor * right.c31),
                    ((1-factor) * left.c32) + (factor * right.c32),
                    ((1-factor) * left.c33) + (factor * right.c33),
                    ((1-factor) * left.c34) + (factor * right.c34),
                    ((1-factor) * left.c41) + (factor * right.c41),
                    ((1-factor) * left.c42) + (factor * right.c42),
                    ((1-factor) * left.c43) + (factor * right.c43),
                    ((1-factor) * left.c44) + (factor * right.c44),
                    ((1-factor) * left.c51) + (factor * right.c51),
                    ((1-factor) * left.c52) + (factor * right.c52),
                    ((1-factor) * left.c53) + (factor * right.c53),
                    ((1-factor) * left.c54) + (factor * right.c54)
                );
    }
}
