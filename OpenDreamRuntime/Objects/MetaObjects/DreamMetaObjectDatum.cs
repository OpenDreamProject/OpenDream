using OpenDreamVM.Procs;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenDreamVM.Objects.MetaObjects {
    class DreamMetaObjectDatum : DreamMetaObjectRoot {
        public DreamMetaObjectDatum(DreamRuntime runtime)
            : base(runtime)
        {}

        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            base.OnObjectCreated(dreamObject, creationArguments);

            dreamObject.SpawnProc("New", creationArguments);
        }

        public override void OnObjectDeleted(DreamObject dreamObject) {
            base.OnObjectDeleted(dreamObject);

            dreamObject.SpawnProc("Del");
        }

        public override DreamValue OnVariableGet(DreamObject dreamObject, string variableName, DreamValue variableValue) {
            if (variableName == "type") {
                return new DreamValue(dreamObject.ObjectDefinition.Type);
            } else if (variableName == "parent_type") {
                return new DreamValue(Runtime.ObjectTree.GetTreeEntryFromPath(dreamObject.ObjectDefinition.Type).ParentEntry.ObjectDefinition.Type);
            } else if (variableName == "vars") {
                return new DreamValue(new DreamListVars(dreamObject));
            } else {
                return base.OnVariableGet(dreamObject, variableName, variableValue);
            }
        }
    }
}
