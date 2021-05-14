using OpenDreamServer.Dream.Procs;
using OpenDreamServer.Net;
using OpenDreamServer.Resources;
using System;

namespace OpenDreamServer.Dream.Objects.MetaObjects {
    class DreamMetaObjectWorld : DreamMetaObjectRoot {
        public static DreamList ContentsList;

        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            base.OnObjectCreated(dreamObject, creationArguments);

            ContentsList = dreamObject.GetVariable("contents").GetValueAsDreamList();

            dreamObject.SetVariable("log", new DreamValue(new ConsoleOutputResource()));

            DreamValue fps = dreamObject.ObjectDefinition.Variables["fps"];
            if (fps.Value != null) {
                dreamObject.SetVariable("tick_lag", new DreamValue(10.0f / fps.GetValueAsInteger()));
            }

            //New() is not called here
        }

        public override void OnVariableSet(DreamObject dreamObject, string variableName, DreamValue variableValue, DreamValue oldVariableValue) {
            base.OnVariableSet(dreamObject, variableName, variableValue, oldVariableValue);

            if (variableName == "fps") {
                dreamObject.SetVariable("tick_lag", new DreamValue(10.0f / variableValue.GetValueAsInteger()));
            } else if (variableName == "maxz") {
                int newMaxZ = variableValue.GetValueAsInteger();

                if (newMaxZ < oldVariableValue.GetValueAsInteger()) throw new NotImplementedException("Cannot set maxz lower than previous value");

                while (Program.DreamMap.Levels.Count < newMaxZ) Program.DreamMap.AddLevel();
            }
        }

        public override DreamValue OnVariableGet(DreamObject dreamObject, string variableName, DreamValue variableValue) {
            if (variableName == "fps") {
                return new DreamValue((float)10.0 / dreamObject.GetVariable("tick_lag").GetValueAsNumber());
            } else if (variableName == "timeofday") {
                return new DreamValue((int)DateTime.UtcNow.TimeOfDay.TotalMilliseconds / 100);
            } else if (variableName == "time") {
                return new DreamValue(dreamObject.GetVariable("tick_lag").GetValueAsNumber() * Program.TickCount);
            } else if (variableName == "realtime") {
                return new DreamValue((DateTime.Now - new DateTime(2000, 1, 1)).Milliseconds / 100);
            } else if (variableName == "tick_usage") {
                long currentTime = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
                long elapsedTime = (currentTime - Program.TickStartTime);
                double tickLength = (dreamObject.GetVariable("tick_lag").GetValueAsNumber() * 100);
                int tickUsage = (int)(elapsedTime / tickLength * 100);

                return new DreamValue(tickUsage);
            } else if (variableName == "maxx") {
                return new DreamValue(Program.DreamMap.Width);
            } else if (variableName == "maxy") {
                return new DreamValue(Program.DreamMap.Height);
            } else if (variableName == "maxz") {
                return new DreamValue(Program.DreamMap.Levels.Count);
            } else if (variableName == "address") {
                return new(Program.DreamServer.Address.ToString());
            } else if (variableName == "port") {
                return new(Program.DreamServer.Port.ToString());
            } else if (variableName == "url") {
                return new("byond://" + Program.DreamServer.Address + ":" + Program.DreamServer.Port);
            } else if (variableName == "system_type") {
                //system_type value should match the defines in Defines.dm
                if (Environment.OSVersion.Platform is PlatformID.Unix or PlatformID.MacOSX or PlatformID.Other) {
                    return new DreamValue(0);
                }
                //Windows
                return new DreamValue(1);
                
            } else {
                return base.OnVariableGet(dreamObject, variableName, variableValue);
            }
        }

        public override DreamValue OperatorOutput(DreamValue a, DreamValue b) {
            foreach (DreamConnection connection in Program.DreamServer.DreamConnections) {
                connection.OutputDreamValue(b);
            }

            return new DreamValue(0);
        }
    }
}
