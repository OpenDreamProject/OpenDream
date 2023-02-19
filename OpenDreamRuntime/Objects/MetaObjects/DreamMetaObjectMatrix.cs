using OpenDreamShared.Dream;
using System.Collections.Immutable;

namespace OpenDreamRuntime.Objects.MetaObjects {
    sealed class DreamMetaObjectMatrix : IDreamMetaObject {
        public static readonly float[] IdentityMatrixArray = {1f, 0f, 0f, 0f, 1f, 0f};

        public bool ShouldCallNew => true;
        public IDreamMetaObject? ParentType { get; set; }

        [Dependency] private readonly IDreamManager _dreamManager = default!;
        [Dependency] private readonly IDreamObjectTree _objectTree = default!;

        public DreamMetaObjectMatrix() {
            IoCManager.InjectDependencies(this);
        }

        /// <summary> Used to create a float array understandable by <see cref="IconAppearance.Transform"/> to be a transform. </summary>
        /// <returns>The matrix's values in an array, in [a,d,b,e,c,f] order.</returns>
        /// <remarks>This will not verify that this is a /matrix</remarks>
        public static float[] MatrixToTransformFloatArray(DreamObject matrix) {
            float[] array = new float[6];
            matrix.GetVariable("a").TryGetValueAsFloat(out array[0]);
            matrix.GetVariable("d").TryGetValueAsFloat(out array[1]);
            matrix.GetVariable("b").TryGetValueAsFloat(out array[2]);
            matrix.GetVariable("e").TryGetValueAsFloat(out array[3]);
            matrix.GetVariable("c").TryGetValueAsFloat(out array[4]);
            matrix.GetVariable("f").TryGetValueAsFloat(out array[5]);
            return array;
        }

        public static float Determinant(DreamObject matrix) {
            try {
                return matrix.GetVariable("a").MustGetValueAsFloat() *
                       matrix.GetVariable("e").MustGetValueAsFloat() -
                       matrix.GetVariable("d").MustGetValueAsFloat() *
                       matrix.GetVariable("b").MustGetValueAsFloat();
            } catch(InvalidCastException) {
                return 0f;
            } catch(KeyNotFoundException) {
                return 0f;
            }
        }

        /// <summary>Inverts the given matrix, in-place.</summary>
        /// <returns>true if inversion was possible, false if not.</returns>
        public static bool TryInvert(DreamObject matrix) {
            var determinant = Determinant(matrix);
            if (determinant == 0f)
                return false;
            var oldValues = EnumerateMatrix(matrix).ToImmutableArray();
            //Just going by what we used to have as DM code within DMStandard. No clue if the math is right, here
            matrix.SetVariableValue("a", new DreamValue( // a = e
                    oldValues[4] / determinant
            ));
            matrix.SetVariableValue("b", new DreamValue( // b = -b
                    -oldValues[1] / determinant
            ));
            matrix.SetVariableValue("c", new DreamValue( // c = b*f - e*c
                    (oldValues[1] * oldValues[5] - oldValues[4] * oldValues[2]) / determinant
            ));
            matrix.SetVariableValue("d", new DreamValue( // d = -d
                    -oldValues[3] / determinant
            ));
            matrix.SetVariableValue("e", new DreamValue( // e = a
                    oldValues[0] / determinant
            ));
            matrix.SetVariableValue("f", new DreamValue( // f = d*c - a*f
                    (oldValues[3] * oldValues[2] - oldValues[0] * oldValues[5]) / determinant
            ));
            return true;
        }

        /// <summary>
        /// Used when printing this matrix to enumerate its values in order.
        /// </summary>
        /// <returns>The matrix's values in [a,b,c,d,e,f] order.</returns>
        /// <remarks>This will not verify that this is a /matrix</remarks>
        public static IEnumerable<float> EnumerateMatrix(DreamObject matrix) {
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
            if (!a.TryGetValueAsDreamObjectOfType(_objectTree.Matrix, out DreamObject left))
                throw new ArgumentException($"Invalid matrix {a}");

            left.GetVariable("a").TryGetValueAsFloat(out float lA);
            left.GetVariable("b").TryGetValueAsFloat(out float lB);
            left.GetVariable("c").TryGetValueAsFloat(out float lC);
            left.GetVariable("d").TryGetValueAsFloat(out float lD);
            left.GetVariable("e").TryGetValueAsFloat(out float lE);
            left.GetVariable("f").TryGetValueAsFloat(out float lF);

            if (b.TryGetValueAsFloat(out float bFloat)) {
                DreamObject output = _objectTree.CreateObject(_objectTree.Matrix);
                output.SetVariable("a", new(lA * bFloat));
                output.SetVariable("b", new(lB * bFloat));
                output.SetVariable("c", new(lC * bFloat));
                output.SetVariable("d", new(lD * bFloat));
                output.SetVariable("e", new(lE * bFloat));
                output.SetVariable("f", new(lF * bFloat));

                return new(output);
            } else if (b.TryGetValueAsDreamObjectOfType(_objectTree.Matrix, out DreamObject right)) {
                right.GetVariable("a").TryGetValueAsFloat(out float rA);
                right.GetVariable("b").TryGetValueAsFloat(out float rB);
                right.GetVariable("c").TryGetValueAsFloat(out float rC);
                right.GetVariable("d").TryGetValueAsFloat(out float rD);
                right.GetVariable("e").TryGetValueAsFloat(out float rE);
                right.GetVariable("f").TryGetValueAsFloat(out float rF);

                DreamObject output = _objectTree.CreateObject(_objectTree.Matrix);
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
            if (a.TryGetValueAsDreamObjectOfType(_objectTree.Matrix, out DreamObject? left) && b.TryGetValueAsDreamObjectOfType(_objectTree.Matrix, out DreamObject? right)) {
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
