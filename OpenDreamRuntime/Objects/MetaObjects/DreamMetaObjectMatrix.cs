using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Objects.MetaObjects {
    sealed class DreamMetaObjectMatrix : DreamMetaObjectDatum {
        private readonly IDreamManager _dreamManager = IoCManager.Resolve<IDreamManager>();

        public static float[] MatrixToFloatArray(DreamObject matrix) {
            if (!matrix.IsSubtypeOf(DreamPath.Matrix))
                throw new ArgumentException($"Invalid matrix {matrix}");

            float[] array = new float[6];
            array[0] = matrix.GetVariable("a").GetValueAsFloat();
            array[1] = matrix.GetVariable("d").GetValueAsFloat();
            array[2] = matrix.GetVariable("b").GetValueAsFloat();
            array[3] = matrix.GetVariable("e").GetValueAsFloat();
            array[4] = matrix.GetVariable("c").GetValueAsFloat();
            array[5] = matrix.GetVariable("f").GetValueAsFloat();
            return array;
        }

        public override DreamValue OperatorMultiply(DreamValue a, DreamValue b) {
            DreamObject left = a.GetValueAsDreamObjectOfType(DreamPath.Matrix);
            float lA = left.GetVariable("a").GetValueAsFloat();
            float lB = left.GetVariable("b").GetValueAsFloat();
            float lC = left.GetVariable("c").GetValueAsFloat();
            float lD = left.GetVariable("d").GetValueAsFloat();
            float lE = left.GetVariable("e").GetValueAsFloat();
            float lF = left.GetVariable("f").GetValueAsFloat();

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
                float rA = right.GetVariable("a").GetValueAsFloat();
                float rB = right.GetVariable("b").GetValueAsFloat();
                float rC = right.GetVariable("c").GetValueAsFloat();
                float rD = right.GetVariable("d").GetValueAsFloat();
                float rE = right.GetVariable("e").GetValueAsFloat();
                float rF = right.GetVariable("f").GetValueAsFloat();

                DreamObject output = _dreamManager.ObjectTree.CreateObject(DreamPath.Matrix);
                output.SetVariable("a", new(rA * lA + rD * lB));
                output.SetVariable("b", new(rB * lA + rE * lB));
                output.SetVariable("c", new(rC * lA + rF * lB + lC));
                output.SetVariable("d", new(rA * lD + rD * lE));
                output.SetVariable("e", new(rB * lD + rE * lE));
                output.SetVariable("f", new(rC * lD + rF * lE + lF));

                return new(output);
            }

            return base.OperatorMultiply(a, b);
        }
    }
}
