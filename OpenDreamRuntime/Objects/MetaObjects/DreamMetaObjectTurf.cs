using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Objects.MetaObjects {
    class DreamMetaObjectTurf : DreamMetaObjectAtom {
        public DreamMetaObjectTurf(DreamRuntime runtime)
            : base(runtime)
        {}

        public override void OnVariableSet(DreamObject dreamObject, string variableName, DreamValue variableValue, DreamValue oldVariableValue) {
            base.OnVariableSet(dreamObject, variableName, variableValue, oldVariableValue);

            if (variableName == "loc") {
                if (variableValue.TryGetValueAsDreamObjectOfType(DreamPath.Turf, out DreamObject replacedTurf)) {
                    DreamList contents = replacedTurf.GetVariable("contents").GetValueAsDreamList();
                    while (contents.GetLength() > 0) { //Transfer all the old turf's contents
                        contents.GetValues()[0].GetValueAsDreamObjectOfType(DreamPath.Atom).SetVariable("loc", new DreamValue(dreamObject));
                    }

                    int x = replacedTurf.GetVariable("x").GetValueAsInteger();
                    int y = replacedTurf.GetVariable("y").GetValueAsInteger();
                    int z = replacedTurf.GetVariable("z").GetValueAsInteger();
                    Runtime.Map.SetTurf(x, y, z, dreamObject);
                }
            }
        }

        public override DreamValue OnVariableGet(DreamObject dreamObject, string variableName, DreamValue variableValue) {
            if (variableName == "loc") {
                int x = dreamObject.GetVariable("x").GetValueAsInteger();
                int y = dreamObject.GetVariable("y").GetValueAsInteger();
                int z = dreamObject.GetVariable("z").GetValueAsInteger();

                return new DreamValue(Runtime.Map.GetAreaAt(x, y, z));
            } else {
                return base.OnVariableGet(dreamObject, variableName, variableValue);
            }
        }
    }
}
