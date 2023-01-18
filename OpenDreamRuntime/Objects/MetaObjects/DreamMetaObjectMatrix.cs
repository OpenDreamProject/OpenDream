using OpenDreamShared.Dream;
using OpenDreamRuntime.Procs;

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

        public ProcStatus? OperatorMultiply(DreamValue a, DreamValue b, DMProcState state) {
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

                state.Push(new DreamValue(output));
                return null;
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

                state.Push(new DreamValue(output));
                return null;
            }
            DreamObject zeromatrix = new DreamObject(_objectTree.Matrix.ObjectDefinition);
            zeromatrix.SetVariable("a", new DreamValue(0.0f));
            zeromatrix.SetVariable("e", new DreamValue(0.0f));
            state.Push(new DreamValue(zeromatrix)); //if multiplication failed, return a 0 matrix, because byond does
            return null;
        }

        public ProcStatus? OperatorEquivalent(DreamValue a, DreamValue b, DMProcState state) {
            if (a.TryGetValueAsDreamObjectOfType(_objectTree.Matrix, out DreamObject? left) && b.TryGetValueAsDreamObjectOfType(_objectTree.Matrix, out DreamObject? right)) {
                const string elements = "abcdef";
                for (int i = 0; i < elements.Length; i++) {
                    left.GetVariable(elements[i].ToString()).TryGetValueAsFloat(out var leftValue); // sets leftValue to 0 if this isn't a float
                    right.GetVariable(elements[i].ToString()).TryGetValueAsFloat(out var rightValue); // ditto
                    if (leftValue != rightValue)
                    {
                        state.Push(DreamValue.False);
                        return null;
                    }
                }
                state.Push(DreamValue.True);
                return null;
            }
            state.Push(DreamValue.False); // This will never be true, because reaching this line means b is not a matrix, while a will always be.
            return null;
        }

        public ProcStatus? OperatorBitNot(DreamValue a, DMProcState state)
        {
            throw new NotImplementedException("/matrix does not support the '~' operator yet");
        }
    }
}
