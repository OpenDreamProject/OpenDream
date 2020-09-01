using OpenDreamServer.Net;
using OpenDreamShared.Dream;
using OpenDreamShared.Net.Packets;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenDreamServer.Dream.Objects.MetaObjects {
    class DreamMetaObjectMob : DreamMetaObjectMovable {
        public override void OnVariableSet(DreamObject dreamObject, string variableName, DreamValue variableValue, DreamValue oldVariableValue) {
            if (variableName == "key" || variableName == "ckey") {
                DreamConnection newClientConnection = Program.DreamServer.GetConnectionFromCKey(variableValue.GetValueAsString());

                TransferClient(dreamObject, newClientConnection.ClientDreamObject);
            } else if (variableName == "client" && variableValue != oldVariableValue) {
                TransferClient(dreamObject, variableValue.GetValueAsDreamObjectOfType(DreamPath.Client));
            } else {
                base.OnVariableSet(dreamObject, variableName, variableValue, oldVariableValue);
            }
        }

        public override DreamValue OnVariableGet(DreamObject dreamObject, string variableName, DreamValue variableValue) {
            if (variableName == "key" || variableName == "ckey") {
                DreamObject clientObject = dreamObject.GetVariable("client").GetValueAsDreamObject();

                if (clientObject != null && clientObject.IsSubtypeOf(DreamPath.Client)) {
                    return clientObject.GetVariable(variableName);
                } else {
                    return new DreamValue((DreamObject)null);
                }
            } else {
                return base.OnVariableGet(dreamObject, variableName, variableValue);
            }
        }

        public override DreamValue OperatorOutput(DreamValue a, DreamValue b) {
            DreamObject client = a.GetValueAsDreamObjectOfType(DreamPath.Mob).GetVariable("client").GetValueAsDreamObjectOfType(DreamPath.Client);
            DreamConnection connection = Program.ClientToConnection[client];

            connection.OutputDreamValue(b);
            return new DreamValue(0);
        }

        private void TransferClient(DreamObject mobObject, DreamObject clientObject) {
            if (mobObject.GetVariable("client").Value != null) {
                mobObject.CallProc("Logout");
            }

            mobObject.SetVariable("client", new DreamValue(clientObject));
            clientObject.SetVariable("mob", new DreamValue(mobObject));
            mobObject.CallProc("Login");
        }
    }
}
