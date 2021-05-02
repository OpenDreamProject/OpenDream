using OpenDreamServer.Dream.Procs;
using OpenDreamShared.Dream;
using System.Collections.Generic;

namespace OpenDreamServer.Dream.Objects.MetaObjects {
    class DreamMetaObjectArea : DreamMetaObjectAtom {
        public static Dictionary<DreamObject, DreamList> AreaContents = new();

        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            DreamList contents = new DreamList();

            contents.ValueAssigned += (DreamList list, DreamValue key, DreamValue value) => {
                if (value.TryGetValueAsDreamObjectOfType(DreamPath.Turf, out DreamObject turf)) {
                    int x = turf.GetVariable("x").GetValueAsInteger();
                    int y = turf.GetVariable("y").GetValueAsInteger();
                    int z = turf.GetVariable("z").GetValueAsInteger();

                    Program.DreamMap.SetArea(x, y, z, dreamObject);
                }
            };

            lock (AreaContents) {
                AreaContents.Add(dreamObject, contents);
            }

            base.OnObjectCreated(dreamObject, creationArguments);
        }

        public override void OnObjectDeleted(DreamObject dreamObject) {
            lock (AreaContents) {
                AreaContents.Remove(dreamObject);
            }

            base.OnObjectDeleted(dreamObject);
        }

        public override DreamValue OnVariableGet(DreamObject dreamObject, string variableName, DreamValue variableValue) {
            if (variableName == "contents") {
                return new DreamValue(AreaContents[dreamObject]);
            } else {
                return base.OnVariableGet(dreamObject, variableName, variableValue);
            }
        }
    }
}
