using OpenDreamServer.Dream.Procs;
using OpenDreamServer.Net;
using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OpenDreamServer.Dream.Objects.MetaObjects {
    class DreamMetaObjectWorld : DreamMetaObjectRoot {
        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            base.OnObjectCreated(dreamObject, creationArguments);

            //New() is not called here
        }

        public override void OnVariableSet(DreamObject dreamObject, string variableName, DreamValue variableValue, DreamValue oldVariableValue) {
            base.OnVariableSet(dreamObject, variableName, variableValue, oldVariableValue);

            if (variableName == "fps") {
                dreamObject.SetVariable("tick_lag", new DreamValue(10.0 / variableValue.GetValueAsInteger()));
            }
        }

        public override DreamValue OnVariableGet(DreamObject dreamObject, string variableName, DreamValue variableValue) {
            if (variableName == "fps") {
                return new DreamValue(10.0 / dreamObject.GetVariable("tick_lag").GetValueAsInteger());
            } else if (variableName == "timeofday") {
                return new DreamValue((int)DateTime.UtcNow.TimeOfDay.TotalMilliseconds / 100);
            } else if (variableName == "time") {
                return new DreamValue(dreamObject.GetVariable("tick_lag").GetValueAsNumber() * Program.TickCount);
            } else {
                return base.OnVariableGet(dreamObject, variableName, variableValue);
            }
        }

        public override DreamValue OperatorOutput(DreamValue a, DreamValue b) {
            Console.WriteLine("WORLD OUTPUT: " + b);
            if (b.Type == DreamValue.DreamValueType.DreamObject && b.Value != null && ((DreamObject)b.Value).IsSubtypeOf(DreamPath.List)) {
                DreamList c = DreamMetaObjectList.DreamLists[b.GetValueAsDreamObject()];
            }

            foreach (DreamConnection connection in Program.DreamServer.DreamConnections) {
                connection.OutputDreamValue(b);
            }

            return new DreamValue(0);
        }
    }
}
