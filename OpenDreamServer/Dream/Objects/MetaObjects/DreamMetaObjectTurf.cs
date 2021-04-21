using OpenDreamServer.Dream.Procs;
using OpenDreamShared.Dream;

namespace OpenDreamServer.Dream.Objects.MetaObjects {
    class DreamMetaObjectTurf : DreamMetaObjectAtom {
        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            base.OnObjectCreated(dreamObject, creationArguments);

            if (creationArguments.GetArgument(0, "loc").TryGetValueAsDreamObjectOfType(DreamPath.Turf, out DreamObject replacedTurf)) {
                DreamList contents = replacedTurf.GetVariable("contents").GetValueAsDreamList();
                while (contents.GetLength() > 0) { //Transfer all the old turf's contents
                    contents.GetValue(new DreamValue(1)).GetValueAsDreamObjectOfType(DreamPath.Atom).SetVariable("loc", new DreamValue(dreamObject));
                }

                int x = replacedTurf.GetVariable("x").GetValueAsInteger();
                int y = replacedTurf.GetVariable("y").GetValueAsInteger();
                int z = replacedTurf.GetVariable("z").GetValueAsInteger();
                Program.DreamMap.SetTurf(x, y, z, dreamObject);
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
