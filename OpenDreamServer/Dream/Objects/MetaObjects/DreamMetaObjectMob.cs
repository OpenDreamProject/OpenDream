using OpenDreamServer.Dream.Procs;
using OpenDreamServer.Net;
using OpenDreamShared.Dream;
using System.Collections.Generic;

namespace OpenDreamServer.Dream.Objects.MetaObjects {
    class DreamMetaObjectMob : DreamMetaObjectMovable {
        public static List<DreamObject> Mobs = new List<DreamObject>();

        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            base.OnObjectCreated(dreamObject, creationArguments);

            lock (Mobs) {
                Mobs.Add(dreamObject);
            }
        }

        public override void OnObjectDeleted(DreamObject dreamObject) {
            base.OnObjectDeleted(dreamObject);

            lock (Mobs) {
                Mobs.Remove(dreamObject);
            }
        }

        public override void OnVariableSet(DreamObject dreamObject, string variableName, DreamValue variableValue, DreamValue oldVariableValue) {
            base.OnVariableSet(dreamObject, variableName, variableValue, oldVariableValue);
            
            if (variableName == "key" || variableName == "ckey") {
                DreamConnection newClientConnection = Program.DreamServer.GetConnectionFromCKey(variableValue.GetValueAsString());

                newClientConnection.MobDreamObject = dreamObject;
            } else if (variableName == "client" && variableValue != oldVariableValue) {
                DreamObject newClient = variableValue.GetValueAsDreamObject();
                DreamObject oldClient = oldVariableValue.GetValueAsDreamObject();

                if (newClient != null) {
                    Program.ClientToConnection[newClient].MobDreamObject = dreamObject;
                } else if (oldClient != null) {
                    Program.ClientToConnection[oldClient].MobDreamObject = null;
                }
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
            } else if (variableName == "client") {
                DreamConnection connection = Program.DreamServer.GetConnectionFromMob(dreamObject);

                if (connection != null && connection.ClientDreamObject != null) {
                    return new DreamValue(connection.ClientDreamObject);
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
    }
}
