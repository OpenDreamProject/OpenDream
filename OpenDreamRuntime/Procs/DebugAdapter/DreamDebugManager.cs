using System.IO;
using System.Linq;
using OpenDreamRuntime.Procs.DebugAdapter.Protocol;
using OpenDreamRuntime.Resources;
using Robust.Server;

namespace OpenDreamRuntime.Procs.DebugAdapter;

sealed class DreamDebugManager : IDreamDebugManager {
    [Dependency] private readonly IDreamManager _dreamManager = default!;
    [Dependency] private readonly DreamResourceManager _resourceManager = default!;
    [Dependency] private readonly IBaseServer _server = default!;

    private DebugAdapter? _adapter;
    private readonly Dictionary<string, List<Breakpoint>> _breakpoints = new();
    private string? jsonPath;
    private int _breakpointIdCounter;

    private ILookup<(string Type, string Proc), FunctionBreakpoint>? functionBreakpoints;

    private struct Breakpoint {
        public int Line;
    }

    public void Initialize(int port) {
        _adapter = new DebugAdapter();

        _adapter.OnClientConnected += OnClientConnected;
        //_adapter.StartListening();
        _adapter.ConnectOut(port: port);
    }

    public void Update() {
        _adapter?.HandleMessages();
    }

    public void Shutdown() {
        _breakpoints.Clear();
        _breakpointIdCounter = 0;
        _adapter?.Shutdown();
    }

    public void HandleOutput(LogLevel logLevel, string message) {
        if (_adapter == null)
            return;

        string category = logLevel switch {
            LogLevel.Fatal => "stderr",
            LogLevel.Error => "stderr",
            _ => "stdout"
        };

        _adapter.SendAll(new OutputEvent(category, $"{message}\n"));
    }

    public void HandleLineChange(DMProcState state, int line) {
        if (!_breakpoints.TryGetValue(state.CurrentSource, out var breakpoints))
            return;

        foreach (Breakpoint breakpoint in breakpoints) {
            if (breakpoint.Line == line) {
                Logger.Info($"Breakpoint hit at {state.CurrentSource}:{line}");
                HandleOutput(LogLevel.Info, $"Breakpoint hit at {state.CurrentSource}:{line}");

                return;
            }
        }
    }

    public void HandleProcStart(DMProcState state) {
        bool breakHere = false;
        if (functionBreakpoints != null) {
            foreach (var bp in functionBreakpoints[(state.Proc.OwningType.PathString, state.Proc.Name)]) {
                if (true /* could implement Condition and HitCondition here */) {
                    breakHere = true;
                    break;
                }
            }
        }
        if (breakHere) {
            Logger.Info($"Function breakpoint hit at {state.Proc.OwningType.PathString}::{state.Proc.Name} -> {state.CurrentSource}");
        }
    }

    private void OnClientConnected(DebugAdapterClient client) {
        client.OnRequest += OnRequest;
    }

    private void OnRequest(DebugAdapterClient client, Request req) {
        // TODO: try/catch here?
        switch (req) {
            case RequestInitialize reqInit:
                HandleRequestInitialize(client, reqInit);
                break;
            case RequestLaunch reqLaunch:
                HandleRequestLaunch(client, reqLaunch);
                break;
            case RequestDisconnect reqDisconnect:
                HandleRequestDisconnect(client, reqDisconnect);
                break;
            case RequestSetBreakpoints reqSetBreakpoints:
                HandleRequestSetBreakpoints(client, reqSetBreakpoints);
                break;
            case RequestConfigurationDone reqConfigDone:
                HandleRequestConfigurationDone(client, reqConfigDone);
                break;
            case RequestSetFunctionBreakpoints reqFuncBreakpoints:
                HandleRequestSetFunctionBreakpoints(client, reqFuncBreakpoints);
                break;
            default:
                req.RespondError(client, $"Unknown request \"{req.Command}\"");
                break;
        }
    }

    private void HandleRequestInitialize(DebugAdapterClient client, RequestInitialize reqInit) {
        var args = reqInit.Arguments;

        // Verify this is an OpenDream adapter
        if (args.AdapterId != "opendream") {
            reqInit.RespondError(client, "Expected an adapter id of \"opendream\"");
            client.Close();
            return;
        }

        reqInit.Respond(client, new Capabilities {
            SupportsConfigurationDoneRequest = true,
            SupportsFunctionBreakpoints = true,
        });
        // ... opportunity to do stuff that might take time here if needed ...
        client.SendMessage(new InitializedEvent());
    }

    private void HandleRequestLaunch(DebugAdapterClient client, RequestLaunch reqLaunch) {
        if (reqLaunch.Arguments.JsonPath == null) {
            reqLaunch.RespondError(client, "No json_path was given");
            client.Close();
            return;
        }

        jsonPath = reqLaunch.Arguments.JsonPath;
        reqLaunch.Respond(client);
    }

    private void HandleRequestConfigurationDone(DebugAdapterClient client, RequestConfigurationDone reqConfigDone) {
        _dreamManager.Initialize(jsonPath);
        reqConfigDone.Respond(client);
        client.SendMessage(new ODReadyEvent(IoCManager.Resolve<Robust.Shared.Network.IServerNetManager>().Port));
    }

    private void HandleRequestDisconnect(DebugAdapterClient client, RequestDisconnect reqDisconnect) {
        // TODO: Don't terminate if launch type was "attach"
        reqDisconnect.Respond(client);
        _server.Shutdown("A shutdown was initiated by the debug adapter");
    }

    private void HandleRequestSetBreakpoints(DebugAdapterClient client, RequestSetBreakpoints reqSetBreakpoints) {
        Source source = reqSetBreakpoints.Arguments.Source;
        string? sourcePath = source.Path ?? source.Name;
        if (sourcePath == null) {
            reqSetBreakpoints.RespondError(client, "A breakpoint source was not given");
            return;
        }

        sourcePath = Path.GetRelativePath(_resourceManager.RootPath, sourcePath);
        if (!_breakpoints.TryGetValue(sourcePath, out var breakpoints)) {
            breakpoints = new List<Breakpoint>();
            _breakpoints.Add(sourcePath, breakpoints);
        }

        breakpoints.Clear();

        var setBreakpoints = reqSetBreakpoints.Arguments.Breakpoints;
        var responseBreakpoints = new Protocol.Breakpoint[setBreakpoints?.Length ?? 0];
        if (setBreakpoints != null) {
            for (int i = 0; i < setBreakpoints.Length; i++) {
                SourceBreakpoint breakpoint = setBreakpoints[i];

                breakpoints.Add(new Breakpoint {
                    Line = breakpoint.Line
                });

                responseBreakpoints[i] = new(_breakpointIdCounter++, source, breakpoint.Line, breakpoint.Column ?? 0);
            }
        }

        reqSetBreakpoints.Respond(client, responseBreakpoints);
    }

    private void HandleRequestSetFunctionBreakpoints(DebugAdapterClient client, RequestSetFunctionBreakpoints reqFuncBreakpoints) {
        var input = reqFuncBreakpoints.Arguments.Breakpoints;
        var output = new Protocol.Breakpoint[input.Length];

        if (input.Length == 0) {
            this.functionBreakpoints = null;
        } else {
            var toSave = new List<(string Type, string Proc, FunctionBreakpoint Breakpoint)>();

            for (int i = 0; i < input.Length; ++i) {
                var bp = input[i];

                string name = bp.Name.Replace("/proc/", "/").Replace("/verb/", "/");
                int last = name.LastIndexOf('/');
                string type = name[..last];
                string proc = name[(last + 1)..];
                if (type == "") {
                    type = "/";
                }
                toSave.Add((type, proc, bp));

                output[i] = new(_breakpointIdCounter++, verified: true);
            }

            this.functionBreakpoints = toSave.ToLookup(triplet => (triplet.Type, triplet.Proc), triplet => triplet.Breakpoint);
        }
        reqFuncBreakpoints.Respond(client, output);
    }
}

interface IDreamDebugManager {
    public void Initialize(int port);
    public void Update();
    public void Shutdown();

    public void HandleOutput(LogLevel logLevel, string message);
    public void HandleProcStart(DMProcState state);
    public void HandleLineChange(DMProcState state, int line);
}
