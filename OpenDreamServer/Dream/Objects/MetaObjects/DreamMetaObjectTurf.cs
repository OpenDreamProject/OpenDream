using OpenDreamServer.Dream.Procs;
using OpenDreamShared.Dream;

namespace OpenDreamServer.Dream.Objects.MetaObjects {
    class DreamMetaObjectTurf : DreamMetaObjectAtom {
        private static DreamObject _area = null; //TODO: Actual areas

        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            base.OnObjectCreated(dreamObject, creationArguments);

            if (_area == null) {
                _area = Program.DreamObjectTree.CreateObject(DreamPath.Area);
            }

            DreamObject loc = dreamObject.GetVariable("loc").GetValueAsDreamObject();
            if (loc != null && loc.IsSubtypeOf(DreamPath.Turf)) {
                Program.DreamMap.SetTurf(loc.GetVariable("x").GetValueAsInteger(), loc.GetVariable("y").GetValueAsInteger(), dreamObject);
            }
        }

        public override DreamValue OnVariableGet(DreamObject dreamObject, string variableName, DreamValue variableValue) {
            if (variableName == "x") {
                if (Program.DreamMap.IsTurfOnMap(dreamObject)) {
                    return new DreamValue(Program.DreamMap.GetTurfLocation(dreamObject).X);
                } else {
                    return new DreamValue(0);
                }
            } else if (variableName == "y") {
                if (Program.DreamMap.IsTurfOnMap(dreamObject)) {
                    return new DreamValue(Program.DreamMap.GetTurfLocation(dreamObject).Y);
                } else {
                    return new DreamValue(0);
                }
            } else if (variableName == "loc") {
                return new DreamValue(_area);
            } else {
                return base.OnVariableGet(dreamObject, variableName, variableValue);
            }
        }
    }
}
