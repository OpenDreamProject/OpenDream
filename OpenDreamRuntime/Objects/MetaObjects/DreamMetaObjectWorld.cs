using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using System;

namespace OpenDreamRuntime.Objects.MetaObjects {
    class DreamMetaObjectWorld : DreamMetaObjectRoot {
        private ViewRange _viewRange;

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

            DreamValue view = dreamObject.ObjectDefinition.Variables["view"];
            if (view.TryGetValueAsString(out string viewString)) {
                _viewRange = new ViewRange(viewString);
            } else {
                _viewRange = new ViewRange(view.GetValueAsInteger());
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
            switch (variableName) {
                case "fps":
                    return new DreamValue(10.0f / dreamObject.GetVariable("tick_lag").GetValueAsFloat());
                case "timeofday":
                    return new DreamValue((int)DateTime.UtcNow.TimeOfDay.TotalMilliseconds / 100);
                case "time":
                    return new DreamValue(dreamObject.GetVariable("tick_lag").GetValueAsFloat() * Runtime.TickCount);
                case "realtime":
                    return new DreamValue((DateTime.Now - new DateTime(2000, 1, 1)).Milliseconds / 100);
                case "tick_usage": {
                    long currentTime = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
                    long elapsedTime = (currentTime - Runtime.TickStartTime);
                    double tickLength = (dreamObject.GetVariable("tick_lag").GetValueAsFloat() * 100);
                    int tickUsage = (int)(elapsedTime / tickLength * 100);

                    return new DreamValue(tickUsage);
                }
                case "maxx":
                    return new DreamValue(Runtime.Map.Width);
                case "maxy":
                    return new DreamValue(Runtime.Map.Height);
                case "maxz":
                    return new DreamValue(Runtime.Map.Levels.Count);
                case "address":
                    return new(Runtime.Server.Address.ToString());
                case "port":
                    return new(Runtime.Server.Port);
                case "url":
                    return new("opendream://" + Runtime.Server.Address + ":" + Runtime.Server.Port);
                case "system_type": {
                    //system_type value should match the defines in Defines.dm
                    if (Environment.OSVersion.Platform is PlatformID.Unix or PlatformID.MacOSX or PlatformID.Other) {
                        return new DreamValue(0);
                    }
                    //Windows
                    return new DreamValue(1);
                }
                case "view": {
                    //Number if square & centerable, string representation otherwise
                    return new DreamValue((_viewRange.IsSquare && _viewRange.IsCenterable) ? _viewRange.Width : _viewRange.ToString());
                }
                default:
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
