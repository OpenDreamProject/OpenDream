using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace OpenDreamRuntime.Objects.MetaObjects {
    sealed class DreamMetaObjectWorld : DreamMetaObjectRoot {
        [Dependency] private IDreamManager _dreamManager = null;
        [Dependency] private DreamResourceManager _dreamRscMan = null;
        [Dependency] private IDreamMapManager _dreamMapManager = null;
        [Dependency] private IGameTiming _gameTiming = null;
        [Dependency] private IConfigurationManager _cfg = null;

        private ViewRange _viewRange;

        private double TickLag { get => _gameTiming.TickPeriod.TotalMilliseconds / 100; }

        public DreamMetaObjectWorld() {
            IoCManager.InjectDependencies(this);
        }

        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            base.OnObjectCreated(dreamObject, creationArguments);

            _dreamManager.WorldContentsList = dreamObject.GetVariable("contents").GetValueAsDreamList();

            DreamValue log = dreamObject.ObjectDefinition.Variables["log"];
            dreamObject.SetVariable("log", log);

            DreamValue fps = dreamObject.ObjectDefinition.Variables["fps"];
            if (fps.Value != null) {
                _cfg.SetCVar(CVars.NetTickrate, fps.GetValueAsInteger());
            }

            DreamValue view = dreamObject.ObjectDefinition.Variables["view"];
            if (view.TryGetValueAsString(out string viewString)) {
                _viewRange = new ViewRange(viewString);
            } else {
                _viewRange = new ViewRange(view.GetValueAsInteger());
            }
        }

        public override void OnVariableSet(DreamObject dreamObject, string variableName, DreamValue variableValue, DreamValue oldVariableValue) {
            base.OnVariableSet(dreamObject, variableName, variableValue, oldVariableValue);

            switch (variableName) {
                case "fps":
                    _cfg.SetCVar(CVars.NetTickrate, variableValue.GetValueAsInteger()); break;
                case "maxz":
                    _dreamMapManager.SetZLevels(variableValue.GetValueAsInteger()); break;
                case "log":
                    if (variableValue.TryGetValueAsString(out var logStr))
                    {
                        dreamObject.SetVariableValue("log", new DreamValue(_dreamRscMan.LoadResource(logStr)));
                    }
                    else if(!variableValue.TryGetValueAsDreamResource(out _))
                    {
                        dreamObject.SetVariableValue("log", new DreamValue(new ConsoleOutputResource()));
                    }
                    break;
            }
        }

        public override DreamValue OnVariableGet(DreamObject dreamObject, string variableName, DreamValue variableValue) {
            switch (variableName) {
                case "tick_lag":
                    return new DreamValue(TickLag);
                case "fps":
                    return new DreamValue(_gameTiming.TickRate);
                case "timeofday":
                    return new DreamValue((int)DateTime.UtcNow.TimeOfDay.TotalMilliseconds / 100);
                case "time":
                    return new DreamValue(_gameTiming.CurTick.Value * TickLag);
                case "realtime":
                    return new DreamValue((DateTime.Now - new DateTime(2000, 1, 1)).Milliseconds / 100);
                case "tick_usage": {
                    var tickUsage = (_gameTiming.RealTime - _gameTiming.LastTick) / _gameTiming.TickPeriod;
                    return new DreamValue(tickUsage * 100);
                }
                case "maxx":
                    return new DreamValue(_dreamMapManager.Size.X);
                case "maxy":
                    return new DreamValue(_dreamMapManager.Size.Y);
                case "maxz":
                    return new DreamValue(_dreamMapManager.Levels);
                //case "address":
                //    return new(Runtime.Server.Address.ToString());
                //case "port":
                //    return new(Runtime.Server.Port);
                //case "url":
                //    return new("opendream://" + Runtime.Server.Address + ":" + Runtime.Server.Port);
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
            foreach (DreamConnection connection in _dreamManager.Connections) {
                connection.OutputDreamValue(b);
            }

            return new DreamValue(0);
        }
    }
}
