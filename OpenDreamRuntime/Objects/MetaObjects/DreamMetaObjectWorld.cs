﻿using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Resources;
using System;

namespace OpenDreamRuntime.Objects.MetaObjects {
    class DreamMetaObjectWorld : DreamMetaObjectRoot {
        public DreamMetaObjectWorld(DreamRuntime runtime)
            : base(runtime)

        {}

        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            base.OnObjectCreated(dreamObject, creationArguments);

            Runtime.WorldContentsList = dreamObject.GetVariable("contents").GetValueAsDreamList();

            dreamObject.SetVariable("log", new DreamValue(new ConsoleOutputResource(Runtime)));

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

                if (newMaxZ < Runtime.Map.Levels.Count) {
                    while (Runtime.Map.Levels.Count > newMaxZ) {
                        Runtime.Map.RemoveLevel();
                    }
                } else {
                    while (Runtime.Map.Levels.Count < newMaxZ) {
                        Runtime.Map.AddLevel();
                    }
                }
            }
        }

        public override DreamValue OnVariableGet(DreamObject dreamObject, string variableName, DreamValue variableValue) {
            if (variableName == "fps") {
                return new DreamValue(10.0f / dreamObject.GetVariable("tick_lag").GetValueAsFloat());
            } else if (variableName == "timeofday") {
                return new DreamValue((int)DateTime.UtcNow.TimeOfDay.TotalMilliseconds / 100);
            } else if (variableName == "time") {
                return new DreamValue(dreamObject.GetVariable("tick_lag").GetValueAsFloat() * Runtime.TickCount);
            } else if (variableName == "realtime") {
                return new DreamValue((DateTime.Now - new DateTime(2000, 1, 1)).Milliseconds / 100);
            } else if (variableName == "tick_usage") {
                long currentTime = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
                long elapsedTime = (currentTime - Runtime.TickStartTime);
                double tickLength = (dreamObject.GetVariable("tick_lag").GetValueAsFloat() * 100);
                int tickUsage = (int)(elapsedTime / tickLength * 100);

                return new DreamValue(tickUsage);
            } else if (variableName == "maxx") {
                return new DreamValue(Runtime.Map.Width);
            } else if (variableName == "maxy") {
                return new DreamValue(Runtime.Map.Height);
            } else if (variableName == "maxz") {
                return new DreamValue(Runtime.Map.Levels.Count);
            } else if (variableName == "address") {
                return new(Runtime.Server.Address.ToString());
            } else if (variableName == "port") {
                return new(Runtime.Server.Port);
            } else if (variableName == "url") {
                return new("opendream://" + Runtime.Server.Address + ":" + Runtime.Server.Port);
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
            foreach (DreamConnection connection in Runtime.Server.Connections) {
                connection.OutputDreamValue(b);
            }

            return new DreamValue(0);
        }
    }
}
