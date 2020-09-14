using OpenDreamServer.Dream.Procs;
using OpenDreamServer.Net;
using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenDreamServer.Dream.Objects.MetaObjects {
    class DreamMetaObjectClient : DreamMetaObjectRoot {
        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            base.OnObjectCreated(dreamObject, creationArguments);

            //New() is not called here
        }

        public override void OnVariableSet(DreamObject dreamObject, string variableName, DreamValue variableValue, DreamValue oldVariableValue) {
            base.OnVariableSet(dreamObject, variableName, variableValue, oldVariableValue);

            if (variableName == "eye") {
                string ckey = dreamObject.GetVariable("ckey").GetValueAsString();
                DreamObject eye = variableValue.GetValueAsDreamObject();
                UInt16 eyeID = (eye != null) ? DreamMetaObjectAtom.AtomIDs[eye] : (UInt16)0xFFFF;

                Program.DreamStateManager.AddClientEyeIDDelta(ckey, eyeID);
            } else if (variableName == "mob") {
                DreamConnection connection = Program.ClientToConnection[dreamObject];

                connection.MobDreamObject = variableValue.GetValueAsDreamObject();
            }
        }

        public override DreamValue OnVariableGet(DreamObject dreamObject, string variableName, DreamValue variableValue) {
            if (variableName == "key" || variableName == "ckey") {
                return new DreamValue(Program.ClientToConnection[dreamObject].CKey);
            } else if (variableName == "mob") {
                return new DreamValue(Program.ClientToConnection[dreamObject].MobDreamObject);
            } else {
                return base.OnVariableGet(dreamObject, variableName, variableValue);
            }
        }

        public override DreamValue OperatorOutput(DreamValue a, DreamValue b) {
            DreamConnection connection = Program.ClientToConnection[a.GetValueAsDreamObjectOfType(DreamPath.Client)];

            connection.OutputDreamValue(b);
            return new DreamValue(0);
        }
    }
}
