using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Objects.MetaObjects {
    sealed class DreamMetaObjectMatrix : IDreamMetaObject {
        public static readonly float[] IdentityMatrixArray = {1f, 0f, 0f, 0f, 1f, 0f};

        public bool ShouldCallNew => true;
        public IDreamMetaObject? ParentType { get; set; }

        private readonly IDreamManager _dreamManager = IoCManager.Resolve<IDreamManager>();

        /// <summary> Used to create a float array understandable by <see cref="IconAppearance.Transform"/> to be a transform. </summary>
        /// <returns>The matrix's values in an array, in [a,d,b,e,c,f] order.</returns>
        /// <exception cref="ArgumentException">Thrown when matrix is invalid.</exception>
        public static float[] MatrixToTransformFloatArray(DreamObject matrix) {
            if (!matrix.IsSubtypeOf(DreamPath.Matrix))
                throw new ArgumentException($"Invalid matrix {matrix}");

            float[] array = new float[6];
            matrix.GetVariable("a").TryGetValueAsFloat(out array[0]);
            matrix.GetVariable("d").TryGetValueAsFloat(out array[1]);
            matrix.GetVariable("b").TryGetValueAsFloat(out array[2]);
            matrix.GetVariable("e").TryGetValueAsFloat(out array[3]);
            matrix.GetVariable("c").TryGetValueAsFloat(out array[4]);
            matrix.GetVariable("f").TryGetValueAsFloat(out array[5]);
            return array;
        }

        /// <summary>
        /// Used when printing this matrix to enumerate its values in order.
        /// </summary>
        /// <returns>The matrix's values in [a,b,c,d,e,f] order.</returns>
        /// <exception cref="ArgumentException">Thrown when matrix is invalid.</exception>
        public static IEnumerable<float> EnumerateMatrix(DreamObject matrix) {
            if (!matrix.IsSubtypeOf(DreamPath.Matrix))
                throw new ArgumentException($"Invalid matrix {matrix}");

            float ret = 0f;
            matrix.GetVariable("a").TryGetValueAsFloat(out ret);
            yield return ret;
            matrix.GetVariable("b").TryGetValueAsFloat(out ret);
            yield return ret;
            matrix.GetVariable("c").TryGetValueAsFloat(out ret);
            yield return ret;
            matrix.GetVariable("d").TryGetValueAsFloat(out ret);
            yield return ret;
            matrix.GetVariable("e").TryGetValueAsFloat(out ret);
            yield return ret;
            matrix.GetVariable("f").TryGetValueAsFloat(out ret);
            yield return ret;
        }

        public DreamValue OperatorMultiply(DreamValue a, DreamValue b) {
            if (!a.TryGetValueAsDreamObjectOfType(DreamPath.Matrix, out DreamObject left))
                throw new ArgumentException($"Invalid matrix {a}");

            left.GetVariable("a").TryGetValueAsFloat(out float lA);
            left.GetVariable("b").TryGetValueAsFloat(out float lB);
            left.GetVariable("c").TryGetValueAsFloat(out float lC);
            left.GetVariable("d").TryGetValueAsFloat(out float lD);
            left.GetVariable("e").TryGetValueAsFloat(out float lE);
            left.GetVariable("f").TryGetValueAsFloat(out float lF);

            if (b.TryGetValueAsFloat(out float bFloat)) {
                DreamObject output = _dreamManager.ObjectTree.CreateObject(DreamPath.Matrix);
                output.SetVariable("a", new(lA * bFloat));
                output.SetVariable("b", new(lB * bFloat));
                output.SetVariable("c", new(lC * bFloat));
                output.SetVariable("d", new(lD * bFloat));
                output.SetVariable("e", new(lE * bFloat));
                output.SetVariable("f", new(lF * bFloat));

                return new(output);
            } else if (b.TryGetValueAsDreamObjectOfType(DreamPath.Matrix, out DreamObject right)) {
                right.GetVariable("a").TryGetValueAsFloat(out float rA);
                right.GetVariable("b").TryGetValueAsFloat(out float rB);
                right.GetVariable("c").TryGetValueAsFloat(out float rC);
                right.GetVariable("d").TryGetValueAsFloat(out float rD);
                right.GetVariable("e").TryGetValueAsFloat(out float rE);
                right.GetVariable("f").TryGetValueAsFloat(out float rF);

                DreamObject output = _dreamManager.ObjectTree.CreateObject(DreamPath.Matrix);
                output.SetVariable("a", new(rA * lA + rD * lB));
                output.SetVariable("b", new(rB * lA + rE * lB));
                output.SetVariable("c", new(rC * lA + rF * lB + lC));
                output.SetVariable("d", new(rA * lD + rD * lE));
                output.SetVariable("e", new(rB * lD + rE * lE));
                output.SetVariable("f", new(rC * lD + rF * lE + lF));

                return new(output);
            }

            if (ParentType == null)
                throw new InvalidOperationException($"Multiplication cannot be done between {a} and {b}");

            return ParentType.OperatorMultiply(a, b);
        }

        public DreamValue OperatorEquivalent(DreamValue a, DreamValue b) {
            if (a.TryGetValueAsDreamObjectOfType(DreamPath.Matrix, out DreamObject? left) && b.TryGetValueAsDreamObjectOfType(DreamPath.Matrix, out DreamObject? right)) {
                const string elements = "abcdef";
                for (int i = 0; i < elements.Length; i++) {
                    left.GetVariable(elements[i].ToString()).TryGetValueAsFloat(out var leftValue); // sets leftValue to 0 if this isn't a float
                    right.GetVariable(elements[i].ToString()).TryGetValueAsFloat(out var rightValue); // ditto
                    if (leftValue != rightValue)
                        return DreamValue.False;
                }
                return DreamValue.True;
            }
            return DreamValue.False; // This will never be true, because reaching this line means b is not a matrix, while a will always be.
        }
    }
}
