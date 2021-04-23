using OpenDreamServer.Dream.Procs;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenDreamServer.Dream.Objects.MetaObjects {
    class DreamMetaObjectDatum : DreamMetaObjectRoot {
        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            base.OnObjectCreated(dreamObject, creationArguments);

            dreamObject.CallProc("New", creationArguments);
        }

        public override void OnObjectDeleted(DreamObject dreamObject) {
            base.OnObjectDeleted(dreamObject);

            dreamObject.CallProc("Del");
        }

        public override DreamValue OnVariableGet(DreamObject dreamObject, string variableName, DreamValue variableValue) {
            if (variableName == "type") {
                return new DreamValue(dreamObject.ObjectDefinition.Type);
            } else if (variableName == "parent_type") {
                return new DreamValue(Program.DreamObjectTree.GetTreeEntryFromPath(dreamObject.ObjectDefinition.Type).ParentEntry.ObjectDefinition.Type);
            } else if (variableName == "vars") {
                return new DreamValue(new DreamListVars(dreamObject));
            } else {
                return base.OnVariableGet(dreamObject, variableName, variableValue);
            }
        }
    }
}
