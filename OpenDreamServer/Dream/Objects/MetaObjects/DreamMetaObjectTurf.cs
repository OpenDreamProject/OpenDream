using OpenDreamServer.Dream.Procs;
using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OpenDreamServer.Dream.Objects.MetaObjects {
    class DreamMetaObjectTurf : DreamMetaObjectAtom {
        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            base.OnObjectCreated(dreamObject, creationArguments);

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
            } else {
                return base.OnVariableGet(dreamObject, variableName, variableValue);
            }
        }
    }
}
