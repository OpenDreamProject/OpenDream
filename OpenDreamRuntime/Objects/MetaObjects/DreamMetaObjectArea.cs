using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Objects.MetaObjects {
    class DreamMetaObjectArea : DreamMetaObjectAtom {
        public DreamMetaObjectArea(DreamRuntime runtime)
            : base(runtime)
        {}

        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            DreamList contents = DreamList.Create(Runtime);

            contents.ValueAssigned += (DreamList list, DreamValue key, DreamValue value) => {
                if (value.TryGetValueAsDreamObjectOfType(DreamPath.Turf, out DreamObject turf)) {
                    int x = turf.GetVariable("x").GetValueAsInteger();
                    int y = turf.GetVariable("y").GetValueAsInteger();
                    int z = turf.GetVariable("z").GetValueAsInteger();

                    Runtime.Map.SetArea(x, y, z, dreamObject);
                }
            };

            Runtime.AreaContents.Add(dreamObject, contents);

            base.OnObjectCreated(dreamObject, creationArguments);
        }

        public override void OnObjectDeleted(DreamObject dreamObject) {
            Runtime.AreaContents.Remove(dreamObject);
            base.OnObjectDeleted(dreamObject);
        }

        public override DreamValue OnVariableGet(DreamObject dreamObject, string variableName, DreamValue variableValue) {
            if (variableName == "contents") {
                return new DreamValue(Runtime.AreaContents[dreamObject]);
            } else {
                return base.OnVariableGet(dreamObject, variableName, variableValue);
            }
        }
    }
}
