using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Rendering;
using OpenDreamShared.Dream;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace OpenDreamRuntime.Objects.MetaObjects {
    class DreamMetaObjectMatrix : DreamMetaObjectDatum {
        private readonly IDreamManager _dreamManager = IoCManager.Resolve<IDreamManager>();

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
