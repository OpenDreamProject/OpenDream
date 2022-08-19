using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;
using Robust.Shared.Network;
using System.Net;
using System.Net.Sockets;

namespace OpenDreamRuntime.Objects.MetaObjects {
    sealed class DreamMetaObjectWorld : IDreamMetaObject {
        public bool ShouldCallNew => false; // Gets called manually later
        public IDreamMetaObject? ParentType { get; set; }

        [Dependency] private readonly IDreamManager _dreamManager = default!;
        [Dependency] private readonly IServerNetManager _netManager = default!;
        [Dependency] private readonly DreamResourceManager _dreamRscMan = default!;
        [Dependency] private readonly IDreamMapManager _dreamMapManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        private ViewRange _viewRange;

        private double TickLag => _gameTiming.TickPeriod.TotalMilliseconds / 100;
        /// <summary> Determines whether we try to show IPv6 or IPv4 to the user during .address and .internet_address queries.</summary>
        private bool DisplayIPv6
        {
            get
            {
                var binds = _cfg.GetCVar(CVars.NetBindTo).Split(',');
                foreach (var bindAddress in binds)
                {
                    if (!IPAddress.TryParse(bindAddress.Trim(), out var address)) // EXTREMELY unlikely since RT does this same check on network startup
                    {
                        continue;
                    }

                    if (address.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        /// <summary> Tries to return the address of the server, as it appears over the internet. May return null.</summary>
        private IPAddress? InternetAddress
        {
            get
            {
                NetManager? net = (NetManager?)_netManager;
                if(net == null) // This may be the case if we're on IntegrationNetManager instead of NetManager.
                { // If so, I don't really know how to force RT to fess up about what our IP is, since it's all hidden behind privates at time of writing.
                    return null;
                }
                return net.ServerChannel?.RemoteEndPoint.Address;
            }
        }

        public DreamMetaObjectWorld() {
            IoCManager.InjectDependencies(this);
        }

        public void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            ParentType?.OnObjectCreated(dreamObject, creationArguments);

            _dreamManager.WorldContentsList = dreamObject.GetVariable("contents").GetValueAsDreamList();

            DreamValue log = dreamObject.ObjectDefinition.Variables["log"];
            dreamObject.SetVariable("log", log);

            DreamValue fps = dreamObject.ObjectDefinition.Variables["fps"];
            if (fps.TryGetValueAsInteger(out var fpsValue)) {
                _cfg.SetCVar(CVars.NetTickrate, fpsValue);
            }

            DreamValue view = dreamObject.ObjectDefinition.Variables["view"];
            if (view.TryGetValueAsString(out string viewString)) {
                _viewRange = new ViewRange(viewString);
            } else {
                if (!view.TryGetValueAsInteger(out var viewInt)) {
                    Logger.Warning("world.view did not contain a valid value. A default of 5 is being used.");
                    viewInt = 5;
                }

                _viewRange = new ViewRange(viewInt);
            }
        }

        public void OnVariableSet(DreamObject dreamObject, string varName, DreamValue value, DreamValue oldValue) {
            ParentType?.OnVariableSet(dreamObject, varName, value, oldValue);

            switch (varName) {
                case "fps":
                    if (!value.TryGetValueAsInteger(out var fps))
                        fps = 10;

                    _cfg.SetCVar(CVars.NetTickrate, fps);
                    break;
                case "maxz":
                    value.TryGetValueAsInteger(out var maxz);

                    _dreamMapManager.SetZLevels(maxz);
                    break;
                case "log":
                    if (value.TryGetValueAsString(out var logStr))
                    {
                        dreamObject.SetVariableValue("log", new DreamValue(_dreamRscMan.LoadResource(logStr)));
                    }
                    else if(!value.TryGetValueAsDreamResource(out _))
                    {
                        dreamObject.SetVariableValue("log", new DreamValue(new ConsoleOutputResource()));
                    }
                    break;
            }
        }

        public DreamValue OnVariableGet(DreamObject dreamObject, string varName, DreamValue value) {
            switch (varName) {
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
                case "address": // By address they mean, the local address we have on the network, not on the internet.
                    var host = Dns.GetHostEntry(Dns.GetHostName());
                    var ipType = DisplayIPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;
                    foreach (var ip in host.AddressList)
                    {
                        if (ip.AddressFamily == ipType)
                        {
                            return new DreamValue(ip.ToString());
                        }
                    }
                    return DreamValue.Null;
                case "port":
                    return new DreamValue(_netManager.Port);
                case "url":
                    if (InternetAddress == null)
                        return DreamValue.Null;
                    return new(InternetAddress + ":" + _netManager.Port); // RIP "opendream://"
                case "internet_address":
                    IPAddress? address = InternetAddress;
                    // We don't need to do any logic with DisplayIPv6 since whatever this address is,
                    // ought to be the address that the boolean's getter is searching for anyways.
                    if (address == null)
                        return DreamValue.Null;
                    return new(address.ToString());
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
                    if (_viewRange.IsSquare && _viewRange.IsCenterable) {
                        return new DreamValue(_viewRange.Width);
                    } else {
                        return new DreamValue(_viewRange.ToString());
                    }
                }
                case "vars":
                    return new DreamValue(DreamListVars.Create(dreamObject));
                default:
                    return ParentType?.OnVariableGet(dreamObject, varName, value) ?? value;
            }
        }

        public DreamValue OperatorOutput(DreamValue a, DreamValue b) {
            foreach (DreamConnection connection in _dreamManager.Connections) {
                connection.OutputDreamValue(b);
            }

            return new DreamValue(0);
        }
    }
}
