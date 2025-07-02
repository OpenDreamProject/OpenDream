using System.Net;
using System.Net.Sockets;
using OpenDreamRuntime.Procs.Native;
using OpenDreamRuntime.Resources;
using OpenDreamShared;
using OpenDreamShared.Dream;
using Robust.Server;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectWorld : DreamObject {
    public override bool ShouldCallNew => false; // Gets called manually later

    public readonly ViewRange DefaultView;
    public DreamResource? Log;

    // DM code may request this info a *lot* so we use the more performant Environment.TickCount64 over RT's stopwatches
    public float TickUsage =>
        (Environment.TickCount64 - DreamManager.CurrentTickStart) / (float)(_gameTiming.TickPeriod.TotalMilliseconds) * 100;

    public float Cpu { get; set; }
    public readonly int IconSize;

    [Dependency] private readonly IBaseServer _server = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private readonly ISawmill _sawmill = Logger.GetSawmill("opendream.world");

    private double TickLag {
        get => _gameTiming.TickPeriod.TotalMilliseconds / 100;
        set => _gameTiming.TickRate = (byte)(1000 / (value * 100));
    }

    private int Fps {
        get => _gameTiming.TickRate;
        set => _gameTiming.TickRate = (byte)value;
    }

    /// <summary> Determines whether we try to show IPv6 or IPv4 to the user during .address and .internet_address queries.</summary>
    private bool DisplayIPv6 {
        get {
            var binds = _cfg.GetCVar(CVars.NetBindTo).Split(',');

            foreach (var bindAddress in binds) {
                // EXTREMELY unlikely since RT does this same check on network startup
                if (!IPAddress.TryParse(bindAddress.Trim(), out var address)) {
                    continue;
                }

                if (address.AddressFamily == AddressFamily.InterNetworkV6) {
                    return true;
                }
            }

            return false;
        }
    }

    private DreamValue _params;

    /// <summary> Tries to return the address of the server, as it appears over the internet. May return null.</summary>
    private IPAddress? InternetAddress => null; //TODO: Implement this!

    public DreamObjectWorld(DreamObjectDefinition objectDefinition) :
        base(objectDefinition) {
        IoCManager.InjectDependencies(this);

        SetTicklag(objectDefinition.Variables["tick_lag"]);
        SetLog(objectDefinition.Variables["log"]);
        if(objectDefinition.Variables["fps"].TryGetValueAsInteger(out var fpsVal) && fpsVal != 10) // To not override tick_lag, only set if it isn't the default 10 FPS
            SetFps(objectDefinition.Variables["fps"]);
        SetSleepOffline(objectDefinition.Variables["sleep_offline"]);

        DreamValue iconSize = objectDefinition.Variables["icon_size"];
        if (!iconSize.TryGetValueAsInteger(out IconSize)) {
            _sawmill.Warning("world.icon_size did not contain a valid value. A default of 32 is being used.");
            IconSize = 32;
        }

        DreamValue view = objectDefinition.Variables["view"];
        if (view.TryGetValueAsString(out var viewString)) {
            DefaultView = new ViewRange(viewString);
        } else {
            if (!view.TryGetValueAsInteger(out var viewInt)) {
                _sawmill.Warning("world.view did not contain a valid value. A default of 5 is being used.");
                viewInt = 5;
            }

            DefaultView = new ViewRange(viewInt);
        }

        var worldParams = _cfg.GetCVar(OpenDreamCVars.WorldParams);
        _params = worldParams != string.Empty ?
            new DreamValue(DreamProcNativeRoot.params2list(ObjectTree, worldParams)) :
            new DreamValue(ObjectTree.CreateList());
    }

    protected override void HandleDeletion(bool possiblyThreaded) {
        // SAFETY: Server shutdown is, spoiler, not threadsafe.
        if (possiblyThreaded) {
            EnterIntoDelQueue();
            return;
        }

        base.HandleDeletion(possiblyThreaded);

        _server.Shutdown("world was deleted");
    }

    ~DreamObjectWorld() {
        if (this != DreamManager.WorldInstance) {
            Deleted = true;
            return;
        }

        Delete(true);
    }

    protected override bool TryGetVar(string varName, out DreamValue value) {
        switch (varName) {
            case "log":
                value = (Log != null) ? new(Log) : DreamValue.Null;
                return true;

            case "params":
                value = _params;
                return true;

            case "status":
            case "name":
                value = new(string.Empty); // TODO
                return true;

            case "contents":
                value = new(new WorldContentsList(ObjectTree.List.ObjectDefinition, AtomManager));
                return true;

            case "process":
                value = new(Environment.ProcessId);
                return true;

            case "tick_lag":
                value = new(TickLag);
                return true;

            case "fps":
                value = new DreamValue(Fps);
                return true;

            case "timeofday":
                value = new DreamValue((int)DateTime.UtcNow.TimeOfDay.TotalMilliseconds / 100);
                return true;

            case "timezone":
                value = new DreamValue((int)DateTimeOffset.Now.Offset.TotalHours);
                return true;

            case "time":
                value = new DreamValue((_gameTiming.CurTick.Value - DreamManager.InitializedTick.Value) * TickLag);
                return true;

            case "realtime":
                value = new DreamValue((DateTime.Now - new DateTime(2000, 1, 1)).Milliseconds / 100);
                return true;

            case "tick_usage":
                value = new DreamValue(TickUsage);
                return true;

            case "cpu":
                value = new DreamValue(Cpu);
                return true;

            case "maxx":
                value = new DreamValue(DreamMapManager.Size.X);
                return true;

            case "maxy":
                value = new DreamValue(DreamMapManager.Size.Y);
                return true;

            case "maxz":
                value = new DreamValue(DreamMapManager.Levels);
                return true;

            case "address": // By address they mean, the local address we have on the network, not on the internet.
                var host = Dns.GetHostEntry(Dns.GetHostName());
                var ipType = DisplayIPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;
                foreach (var ip in host.AddressList) {
                    if (ip.AddressFamily == ipType) {
                        value = new DreamValue(ip.ToString());

                        return true;
                    }
                }

                value = DreamValue.Null;
                return true;

            case "port":
                value = new(_netManager.Port);
                return true;

            case "url":
                if (InternetAddress == null)
                    value = DreamValue.Null;
                else
                    value = new(InternetAddress + ":" + _netManager.Port); // RIP "opendream://"

                return true;

            case "internet_address":
                IPAddress? address = InternetAddress;
                // We don't need to do any logic with DisplayIPv6 since whatever this address is,
                // ought to be the address that the boolean's getter is searching for anyways.
                if (address == null)
                    value = DreamValue.Null;
                else
                    value = new(address.ToString());

                return true;

            case "system_type":
                //system_type value should match the defines in Defines.dm
                if (Environment.OSVersion.Platform is PlatformID.Unix or PlatformID.MacOSX or PlatformID.Other)
                    value = new DreamValue("UNIX");
                else
                    value = new DreamValue("MS_WINDOWS"); //Windows

                return true;

            case "view":
                // Number if square & centerable, string representation otherwise
                if (DefaultView.IsSquare && DefaultView.IsCenterable) {
                    value = new DreamValue(DefaultView.Range);
                } else {
                    value = new DreamValue(DefaultView.ToString());
                }

                return true;

            // Remove OPENDREAM_TOPIC_PORT_EXISTS if this is ever removed
            case "opendream_topic_port":
                var topicPort = DreamManager.ActiveTopicPort;
                value = topicPort.HasValue ? new DreamValue((int)topicPort) : DreamValue.Null;
                return true;

            default:
                // Note that invalid vars on /world will give null and not error in BYOND
                // We don't replicate that
                return base.TryGetVar(varName, out value);
        }
    }

    protected override void SetVar(string varName, DreamValue value) {
        switch (varName) {
            // Unimplemented writeable vars
            case "game_state":
            case "hub":
            case "hub_password":
            case "mob":
            case "name":
            case "status":
            case "version":
            case "visibility":
                // Set it in the var dictionary, so reading at least gives the same value
                base.SetVar(varName, value);
                break;

            case "sleep_offline":
                SetSleepOffline(value);
                break;

            case "time": // Doesn't error, but doesn't affect its value either
                break;

            case "params":
                _params = value;
                break;

            case "tick_lag":
                SetTicklag(value);
                break;

            case "fps":
                SetFps(value);
                break;

            case "maxz":
                value.TryGetValueAsInteger(out var maxz);

                DreamMapManager.SetZLevels(maxz);
                break;

            case "log":
                SetLog(value);
                break;

            case "maxx":
                value.TryGetValueAsInteger(out var maxx);

                DreamMapManager.SetWorldSize(new Vector2i(maxx, DreamMapManager.Size.Y));
                break;

            case "maxy":
                value.TryGetValueAsInteger(out var maxy);

                DreamMapManager.SetWorldSize(new Vector2i(DreamMapManager.Size.X, maxy));
                break;

            default:
                throw new Exception($"Cannot set var \"{varName}\" on world");
        }
    }

    public override void OperatorOutput(DreamValue b) {
        foreach (DreamConnection connection in DreamManager.Connections) {
            connection.OutputDreamValue(b);
        }
    }

    private void SetLog(DreamValue log) {
        if (log.TryGetValueAsString(out var logStr)) {
            Log = DreamResourceManager.LoadResource(logStr);
        } else if (log.TryGetValueAsDreamResource(out var logRsc)) {
            Log = logRsc;
        } else {
            Log = new ConsoleOutputResource();
        }
    }

    private void SetFps(DreamValue fps) {
        if (!fps.TryGetValueAsFloat(out var fpsValue))
            fpsValue = 10f;

        Fps = (int)Math.Round(fpsValue);
    }

    private void SetTicklag(DreamValue value) {
        if (!value.TryGetValueAsFloat(out var tickLag))
            tickLag = 1; // An invalid tick_lag gets turned into 1

        TickLag = tickLag;
    }

    private void SetSleepOffline(DreamValue sleepOffline) {
        if (sleepOffline.IsTruthy()) {
            _cfg.OverrideDefault(CVars.GameAutoPauseEmpty, true);
            SetVariableValue("sleep_offline", DreamValue.True);
            return;
        }

        SetVariableValue("sleep_offline", DreamValue.False);
        _cfg.OverrideDefault(CVars.GameAutoPauseEmpty, false);
    }
}
