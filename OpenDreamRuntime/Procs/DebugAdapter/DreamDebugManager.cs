using System.IO;
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
    private int _breakpointIdCounter;

    private struct Breakpoint {
        public int Line;
    }

    public void Initialize(int port) {
        Console.WriteLine("DreamDebugManager.Initialize");
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
                Logger.Debug($"Breakpoint hit at {state.CurrentSource}:{line}");
                HandleOutput(LogLevel.Info, $"Breakpoint hit at {state.CurrentSource}:{line}");

                return;
            }
        }
    }

    private void OnClientConnected(DebugAdapterClient client) {
        Console.WriteLine("OnClientConnected: " + client);
        client.OnRequest += OnRequest;
    }

    private void OnRequest(DebugAdapterClient client, Request req) {
        Console.WriteLine("OnRequest: " + req);
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

        reqInit.Respond(client);
        client.SendMessage(new InitializedEvent());
    }

    private void HandleRequestLaunch(DebugAdapterClient client, RequestLaunch reqLaunch) {
        if (reqLaunch.Arguments.JsonPath == null) {
            reqLaunch.RespondError(client, "No json_path was given");
            client.Close();
            return;
        }

        _dreamManager.Initialize(reqLaunch.Arguments.JsonPath);
        reqLaunch.Respond(client);
    }

    private void HandleRequestDisconnect(DebugAdapterClient client, RequestDisconnect reqDisconnect) {
        if (reqDisconnect.Arguments.Restart) {
            reqDisconnect.RespondError(client, "Restarts are not supported");
        }

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
}

interface IDreamDebugManager {
    public void Initialize(int port);
    public void Update();
    public void Shutdown();

    public void HandleOutput(LogLevel logLevel, string message);
    public void HandleLineChange(DMProcState state, int line);
}
