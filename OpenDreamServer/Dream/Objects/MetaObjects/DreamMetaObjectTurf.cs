using OpenDreamServer.Dream.Procs;
using OpenDreamShared.Dream;

namespace OpenDreamServer.Dream.Objects.MetaObjects {
    class DreamMetaObjectTurf : DreamMetaObjectAtom {
        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            DreamObject loc = DreamMetaObjectAtom.FindLocArgument(creationArguments);

            base.OnObjectCreated(dreamObject, creationArguments);

            if (loc != null && loc.IsSubtypeOf(DreamPath.Turf)) {
                DreamList contents = loc.GetVariable("contents").GetValueAsDreamList();
                while (contents.GetLength() > 0) { //Transfer all the old turf's contents
                    contents.GetValue(new DreamValue(1)).GetValueAsDreamObjectOfType(DreamPath.Atom).SetVariable("loc", new DreamValue(dreamObject));
                }

                Program.DreamMap.SetTurf(loc.GetVariable("x").GetValueAsInteger(), loc.GetVariable("y").GetValueAsInteger(), dreamObject);
            }
        }

        public override DreamValue OnVariableGet(DreamObject dreamObject, string variableName, DreamValue variableValue) {
            if (variableName == "loc") {
                int x = dreamObject.GetVariable("x").GetValueAsInteger();
                int y = dreamObject.GetVariable("y").GetValueAsInteger();
                int z = dreamObject.GetVariable("z").GetValueAsInteger();

                return new DreamValue(Program.DreamMap.GetAreaAt(x, y, z));
            } else {
                return base.OnVariableGet(dreamObject, variableName, variableValue);
            }
        }
    }
}
