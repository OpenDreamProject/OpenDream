using Robust.Shared.Maths;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
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

        public static ColorMatrix Identity =>
            new ColorMatrix(1F, 0F, 0F, 0F,
                            0F, 1F, 0F, 0F,
                            0F, 0F, 1F, 0F,
                            0F, 0F, 0F, 1F,
                            0F, 0F, 0F, 0F);

        public void SetRow(int row, in Color color) {
            switch(row) {
                case 0:
                    c11 = color.R;
                    c12 = color.G;
                    c13 = color.B;
                    c14 = color.A;
                    break;
                case 1:
                    c21 = color.R;
                    c22 = color.G;
                    c23 = color.B;
                    c24 = color.A;
                    break;
                case 2:
                    c31 = color.R;
                    c32 = color.G;
                    c33 = color.B;
                    c34 = color.A;
                    break;
                case 3:
                    c41 = color.R;
                    c42 = color.G;
                    c43 = color.B;
                    c44 = color.A;
                    break;
                case 4:
                    c51 = color.R;
                    c52 = color.G;
                    c53 = color.B;
                    c54 = color.A;
                    break;
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
