using Robust.Shared.Maths;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenDreamShared.Dream {
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
        public IEnumerable<float> GetDiagonal() {
            yield return c11;
            yield return c22;
            yield return c33;
            yield return c44;
            yield break;
        }

        /// <summary>
        /// Returns all of the values in this struct, in order.
        /// </summary>
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
    }
}
