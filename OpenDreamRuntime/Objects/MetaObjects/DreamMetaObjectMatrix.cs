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

        /// <remarks>
        /// This does elect to actually call /matrix/New() with the old matrix's values, <br/>
        /// if anything, for the sake of support for having derived classes of /matrix.
        /// </remarks>
        /// <param name="matrix">The matrix to clone.</param>
        /// <returns>A clone of the given matrix.</returns>
        public static DreamObject MatrixClone(IDreamObjectTree ObjectTree, DreamObject matrix) {
            var newMatrix = ObjectTree.CreateObject(matrix.ObjectDefinition.TreeEntry);
            var args = new List<DreamValue>(6);
            foreach(float f in EnumerateMatrix(matrix)) {
                args.Add(new DreamValue(f));
            }
            newMatrix.InitSpawn(new Procs.DreamProcArguments(args));
            return newMatrix;
        }

        /// <summary>
        /// Simple helper for quickly making a basic matrix given its six values, in a-to-f order.
        /// </summary>
        /// <remarks>
        /// Note that this skips over making a New() call, so hopefully you're not doing anything meaningful in there, DM-side. <br/>
        /// <see langword="FIXME:"/> actually call /New(), if necessary, when creating a matrix in this way.
        /// </remarks>
        /// <returns>A matrix created with a to f manually set to the floats given.</returns>
        public static DreamObject MakeMatrix(IDreamObjectTree ObjectTree, float a, float b, float c, float d, float e, float f) {
            var newMatrix = ObjectTree.CreateObject(ObjectTree.Matrix);
            newMatrix.SetVariableValue("a", new(a));
            newMatrix.SetVariableValue("b", new(b));
            newMatrix.SetVariableValue("c", new(c));
            newMatrix.SetVariableValue("d", new(d));
            newMatrix.SetVariableValue("e", new(e));
            newMatrix.SetVariableValue("f", new(f));
            return newMatrix;
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

        /// <summary> Scales a given matrix by the two scaling factors given.</summary>
        /// <remarks> Note that this does use <see cref="DreamObject.SetVariableValue"/>.</remarks>
        /// <exception cref="InvalidOperationException">Thrown if the matrix has non-float members.</exception>
        public static void ScaleMatrix(DreamObject matrix, float x, float y) {
            try {
                matrix.SetVariableValue("a", new DreamValue(matrix.GetVariable("a").MustGetValueAsFloat() * x));
                matrix.SetVariableValue("b", new DreamValue(matrix.GetVariable("b").MustGetValueAsFloat() * x));
                matrix.SetVariableValue("c", new DreamValue(matrix.GetVariable("c").MustGetValueAsFloat() * x));

                matrix.SetVariableValue("d", new DreamValue(matrix.GetVariable("d").MustGetValueAsFloat() * y));
                matrix.SetVariableValue("e", new DreamValue(matrix.GetVariable("e").MustGetValueAsFloat() * y));
                matrix.SetVariableValue("f", new DreamValue(matrix.GetVariable("f").MustGetValueAsFloat() * y));
            } catch(InvalidCastException) { // If any of these MustGet()s fail, try to give a more descriptive runtime
                throw new InvalidOperationException($"Invalid matrix '{matrix}' cannot be scaled");
            }
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
                DreamObject output = MakeMatrix(_objectTree,
                        lA * bFloat,lB * bFloat,lC * bFloat,
                        lD * bFloat,lE * bFloat,lF * bFloat
                    );
                return new(output);
            } else if (b.TryGetValueAsDreamObjectOfType(_objectTree.Matrix, out DreamObject right)) {
                right.GetVariable("a").TryGetValueAsFloat(out float rA);
                right.GetVariable("b").TryGetValueAsFloat(out float rB);
                right.GetVariable("c").TryGetValueAsFloat(out float rC);
                right.GetVariable("d").TryGetValueAsFloat(out float rD);
                right.GetVariable("e").TryGetValueAsFloat(out float rE);
                right.GetVariable("f").TryGetValueAsFloat(out float rF);

                DreamObject output = MakeMatrix(_objectTree,
                    rA * lA + rD * lB, // a
                    rB * lA + rE * lB, // b
                    rC * lA + rF * lB + lC, // c
                    rA * lD + rD * lE, // d
                    rB * lD + rE * lE, // e
                    rC * lD + rF * lE + lF // f
                );

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
