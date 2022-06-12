using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Objects.MetaObjects {
    sealed class DreamMetaObjectTurf : DreamMetaObjectAtom {
        private IDreamMapManager _dreamMapManager = IoCManager.Resolve<IDreamMapManager>();

        public override void OnVariableSet(DreamObject dreamObject, string variableName, DreamValue variableValue, DreamValue oldVariableValue) {
            base.OnVariableSet(dreamObject, variableName, variableValue, oldVariableValue);

            if (variableName == "loc") {
                if (variableValue.TryGetValueAsDreamObjectOfType(DreamPath.Turf, out DreamObject replacedTurf)) {
                    //Transfer all the old turf's contents
                    DreamList contents = replacedTurf.GetVariable("contents").GetValueAsDreamList();
                    foreach (DreamValue child in contents.GetValues()) {
                        child.GetValueAsDreamObjectOfType(DreamPath.Movable).SetVariable("loc", new DreamValue(dreamObject));
                    }

                    int x = replacedTurf.GetVariable("x").GetValueAsInteger();
                    int y = replacedTurf.GetVariable("y").GetValueAsInteger();
                    int z = replacedTurf.GetVariable("z").GetValueAsInteger();
                    _dreamMapManager.SetTurf(x, y, z, dreamObject);
                }
            }
        }

        public override DreamValue OnVariableGet(DreamObject dreamObject, string variableName, DreamValue variableValue) {
            if (variableName == "loc") {
                int x = dreamObject.GetVariable("x").GetValueAsInteger();
                int y = dreamObject.GetVariable("y").GetValueAsInteger();
                int z = dreamObject.GetVariable("z").GetValueAsInteger();

                return new DreamValue(_dreamMapManager.GetAreaAt(x, y, z));
            } else {
                return base.OnVariableGet(dreamObject, variableName, variableValue);
            }
        }
    }
}
