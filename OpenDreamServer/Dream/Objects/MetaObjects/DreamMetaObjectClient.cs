using OpenDreamServer.Dream.Procs;
using OpenDreamServer.Net;
using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenDreamServer.Dream.Objects.MetaObjects {
    class DreamMetaObjectClient : DreamMetaObjectRoot {
        private Dictionary<DreamList, DreamObject> _screenListToClient = new();

        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            base.OnObjectCreated(dreamObject, creationArguments);

            //New() is not called here
        }

        public override void OnVariableSet(DreamObject dreamObject, string variableName, DreamValue variableValue, DreamValue oldVariableValue) {
            base.OnVariableSet(dreamObject, variableName, variableValue, oldVariableValue);

            if (variableName == "eye") {
                string ckey = dreamObject.GetVariable("ckey").GetValueAsString();
                DreamObject eye = variableValue.GetValueAsDreamObject();
                UInt32 eyeID = (eye != null) ? DreamMetaObjectAtom.AtomIDs[eye] : UInt32.MaxValue;

                Program.DreamStateManager.AddClientEyeIDDelta(ckey, eyeID);
            } else if (variableName == "mob") {
                DreamConnection connection = Program.ClientToConnection[dreamObject];

                connection.MobDreamObject = variableValue.GetValueAsDreamObject();
            } else if (variableName == "screen") {
                if (oldVariableValue.TryGetValueAsDreamList(out DreamList oldList)) {

                    oldList.Cut();
                    oldList.ValueAssigned -= ScreenValueAssigned;
                    oldList.BeforeValueRemoved -= ScreenBeforeValueRemoved;
                    _screenListToClient.Remove(oldList);
                }

                DreamList screenList;
                if (!variableValue.TryGetValueAsDreamList(out screenList)) {
                    screenList = new DreamList();
                }

                screenList.ValueAssigned += ScreenValueAssigned;
                screenList.BeforeValueRemoved += ScreenBeforeValueRemoved;
                _screenListToClient[screenList] = dreamObject;
            }
        }

        public override DreamValue OnVariableGet(DreamObject dreamObject, string variableName, DreamValue variableValue) {
            if (variableName == "key" || variableName == "ckey") {
                return new DreamValue(Program.ClientToConnection[dreamObject].CKey);
            } else if (variableName == "mob") {
                return new DreamValue(Program.ClientToConnection[dreamObject].MobDreamObject);
            } else if (variableName == "address") {
                return new DreamValue(Program.ClientToConnection[dreamObject].Address.ToString());
            } else if (variableName == "inactivity") {
                return new DreamValue(0);
            } else {
                return base.OnVariableGet(dreamObject, variableName, variableValue);
            }
        }

        public override DreamValue OperatorOutput(DreamValue a, DreamValue b) {
            DreamConnection connection = Program.ClientToConnection[a.GetValueAsDreamObjectOfType(DreamPath.Client)];

            connection.OutputDreamValue(b);
            return new DreamValue(0);
        }

        private void ScreenValueAssigned(DreamList screenList, DreamValue screenKey, DreamValue screenValue) {
            if (screenValue == DreamValue.Null) return;

            DreamObject atom = screenValue.GetValueAsDreamObjectOfType(DreamPath.Movable);
            Program.DreamStateManager.AddClientScreenObject(_screenListToClient[screenList].GetVariable("ckey").GetValueAsString(), atom);
        }

        private void ScreenBeforeValueRemoved(DreamList screenList, DreamValue screenKey, DreamValue screenValue) {
            if (screenValue == DreamValue.Null) return;

            DreamObject atom = screenValue.GetValueAsDreamObjectOfType(DreamPath.Movable);
            Program.DreamStateManager.RemoveClientScreenObject(_screenListToClient[screenList].GetVariable("ckey").GetValueAsString(), atom);
        }
    }
}
