using System.IO;
using System.Linq;
using OpenDreamRuntime.Procs.DebugAdapter.Protocol;
using OpenDreamRuntime.Resources;
using Robust.Server;

namespace OpenDreamRuntime.Procs.DebugAdapter;

sealed class DreamDebugManager : IDreamDebugManager {
    [Dependency] private readonly IDreamManager _dreamManager = default!;
    [Dependency] private readonly DreamResourceManager _resourceManager = default!;
    [Dependency] private readonly IProcScheduler _procScheduler = default!;
    [Dependency] private readonly IBaseServer _server = default!;

    private DebugAdapter? _adapter;
    private readonly Dictionary<string, List<Breakpoint>> _breakpoints = new();
    private string? jsonPath;
    private int _breakpointIdCounter = 1;

    private ILookup<(string Type, string Proc), ActiveFunctionBreakpoint>? functionBreakpoints;
    private Dictionary<int, WeakReference<ProcState>> stackFramesById = new();

    private int _variablesIdCounter = 0;
    private Dictionary<int, Func<RequestVariables, IEnumerable<Variable>>> variableReferences = new();
    private int AllocVariableRef(Func<RequestVariables, IEnumerable<Variable>> func) {
        int id = ++_variablesIdCounter;
        variableReferences[id] = func;
        return id;
    }

    public bool Stopped { get; private set; }

    private string RootPath => _resourceManager.RootPath ?? Path.GetDirectoryName(jsonPath) ?? throw new Exception("No RootPath yet!");

    private struct Breakpoint {
        public int Id;
        public int Line;
    }

    private struct ActiveFunctionBreakpoint {
        public int Id;
        public string? Condition;
        public string? HitCondition;
    }

    public void Initialize(int port) {
        _adapter = new DebugAdapter();

        _adapter.OnClientConnected += OnClientConnected;
        //_adapter.StartListening();
        _adapter.ConnectOut(port: port);
    }

    public void Update() {
        _adapter?.HandleMessages();
        if (_adapter == null || !_adapter.AnyClientsConnected()) {
            Stopped = false;
        }
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

        var hit = new List<int>();
        foreach (Breakpoint breakpoint in breakpoints) {
            if (breakpoint.Line == line) {
                Logger.Info($"Breakpoint hit at {state.CurrentSource}:{line}");
                hit.Add(breakpoint.Id);
                return;
            }
        }
        if (hit.Any()) {
            Stop(new StoppedEvent {
                Reason = StoppedEvent.ReasonBreakpoint,
                ThreadId = state.Thread.Id,
                AllThreadsStopped = true,
                HitBreakpointIds = hit,
            });
        }
    }

    public void HandleProcStart(DMProcState state) {
        var hit = new List<int>();
        if (functionBreakpoints != null) {
            foreach (var bp in functionBreakpoints[(state.Proc.OwningType.PathString, state.Proc.Name)]) {
                if (bp.Condition is null && bp.HitCondition is null) {
                    hit.Add(bp.Id);
                    break;
                }
            }
        }
        if (hit.Any()) {
            Logger.Info($"Function breakpoint hit at {state.Proc.OwningType.PathString}::{state.Proc.Name}");
            Stop(new StoppedEvent {
                Reason = StoppedEvent.ReasonFunctionBreakpoint,
                ThreadId = state.Thread.Id,
                AllThreadsStopped = true,
                HitBreakpointIds = hit,
            });
        }
    }

    private void Stop(StoppedEvent stoppedEvent) {
        if (_adapter == null || !_adapter.AnyClientsConnected())
            return;

        _adapter.SendAll(stoppedEvent);

        Stopped = true;

        // We have to block the DM runtime no matter where we are in its call
        // stack. Unfortunately this also blocks the Robust engine.
        while (Stopped) {
            Update();
            System.Threading.Thread.Sleep(50);
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
            case RequestThreads reqThreads:
                HandleRequestThreads(client, reqThreads);
                break;
            case RequestContinue reqContinue:
                HandleRequestContinue(client, reqContinue);
                break;
            case RequestPause reqPause:
                HandleRequestPause(client, reqPause);
                break;
            case RequestStackTrace requestStackTrace:
                HandleRequestStackTrace(client, requestStackTrace);
                break;
            case RequestScopes requestScopes:
                HandleRequestScopes(client, requestScopes);
                break;
            case RequestVariables requestVariables:
                HandleRequestVariables(client, requestVariables);
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

        sourcePath = Path.GetRelativePath(RootPath, sourcePath);
        if (!_breakpoints.TryGetValue(sourcePath, out var breakpoints)) {
            breakpoints = new List<Breakpoint>();
            _breakpoints.Add(sourcePath, breakpoints);
        }

        breakpoints.Clear();

        var setBreakpoints = reqSetBreakpoints.Arguments.Breakpoints;
        var responseBreakpoints = new Protocol.Breakpoint[setBreakpoints?.Length ?? 0];
        if (setBreakpoints != null) {
            for (int i = 0; i < setBreakpoints.Length; i++) {
                int id = ++_breakpointIdCounter;
                SourceBreakpoint breakpoint = setBreakpoints[i];

                breakpoints.Add(new Breakpoint {
                    Id = id,
                    Line = breakpoint.Line,
                });

                responseBreakpoints[i] = new(id, source, breakpoint.Line, breakpoint.Column ?? 0);
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
            var toSave = new List<(string Type, string Proc, ActiveFunctionBreakpoint Breakpoint)>();

            for (int i = 0; i < input.Length; ++i) {
                var bp = input[i];

                string name = bp.Name.Replace("/proc/", "/").Replace("/verb/", "/");
                int last = name.LastIndexOf('/');
                string type = name[..last];
                string proc = name[(last + 1)..];
                if (type == "") {
                    type = "/";
                }

                int id = ++_breakpointIdCounter;
                output[i] = new(id, verified: true);
                toSave.Add((type, proc, new ActiveFunctionBreakpoint {
                    Id = id,
                    Condition = bp.Condition,
                    HitCondition = bp.HitCondition,
                }));
            }

            this.functionBreakpoints = toSave.ToLookup(triplet => (triplet.Type, triplet.Proc), triplet => triplet.Breakpoint);
        }
        reqFuncBreakpoints.Respond(client, output);
    }

    private IEnumerable<DreamThread> InspectThreads() {
        return DreamThread.InspectExecutingThreads().Concat(_procScheduler.InspectThreads());
    }

    private void HandleRequestThreads(DebugAdapterClient client, RequestThreads reqThreads) {
        var threads = new List<Thread>();
        foreach (var thread in InspectThreads().Distinct()) {
            threads.Add(new Thread(thread.Id, thread.Name));
        }
        if (!threads.Any()) {
            threads.Add(new Thread(0, "Nothing"));
        }
        reqThreads.Respond(client, threads);
    }

    private void HandleRequestContinue(DebugAdapterClient client, RequestContinue reqContinue) {
        Stopped = false;
        stackFramesById.Clear();
        variableReferences.Clear();
        reqContinue.Respond(client, allThreadsContinued: true);
    }

    private void HandleRequestPause(DebugAdapterClient client, RequestPause reqPause) {
        // "The debug adapter first sends the response and then a stopped event (with reason pause) after the thread has been paused successfully."
        reqPause.Respond(client);
        Stop(new StoppedEvent {
            Reason = StoppedEvent.ReasonPause,
            AllThreadsStopped = true,
            Description = "Paused by request",
        });
    }

    private void HandleRequestStackTrace(DebugAdapterClient client, RequestStackTrace reqStackTrace) {
        var thread = InspectThreads().FirstOrDefault(t => t.Id == reqStackTrace.Arguments.ThreadId);
        if (thread is null) {
            reqStackTrace.RespondError(client, $"No thread with ID {reqStackTrace.Arguments.ThreadId}");
            return;
        }

        var output = new List<StackFrame>();
        var nameBuilder = new System.Text.StringBuilder();
        foreach (var frame in thread.InspectStack()) {
            var (source, line) = frame.SourceLine;
            nameBuilder.Clear();
            frame.AppendStackFrame(nameBuilder);
            output.Add(new StackFrame {
                Id = frame.Id,
                Source = source is null ? null : new Source(Path.GetFileName(source), Path.Join(RootPath, source)),
                Line = line ?? 0,
                Name = nameBuilder.ToString(),
            });
            stackFramesById[frame.Id] = new WeakReference<ProcState>(frame);
        }
        reqStackTrace.Respond(client, output, output.Count);
    }

    private void HandleRequestScopes(DebugAdapterClient client, RequestScopes requestScopes) {
        if (!stackFramesById.TryGetValue(requestScopes.Arguments.FrameId, out var weak) || !weak.TryGetTarget(out var frame)) {
            requestScopes.RespondError(client, $"No frame with ID {requestScopes.Arguments.FrameId}");
            return;
        }

        if (frame is not DMProcState dmFrame) {
            requestScopes.RespondError(client, $"Cannot inspect native frame");
            return;
        }

        requestScopes.Respond(client, new[] {
            new Scope {
                Name = "Locals",
                VariablesReference = AllocVariableRef(req => ExpandLocals(req, dmFrame)),
            },
        });
    }

    private IEnumerable<Variable> ExpandLocals(RequestVariables req, DMProcState dmFrame) {
        if (dmFrame.Proc.OwningType != OpenDreamShared.Dream.DreamPath.Root) {
            yield return DescribeValue("src", new(dmFrame.Instance));
        }
        yield return DescribeValue("usr", new(dmFrame.Usr));
        foreach (var (name, value) in dmFrame.InspectLocals()) {
            yield return DescribeValue(name, value);
        }
    }

    private Variable DescribeValue(string name, DreamValue value) {
        var varDesc = new Variable { Name = name };
        varDesc.Value = value.ToString();
        if (value.TryGetValueAsDreamObject(out var obj)) {
            varDesc.VariablesReference = AllocVariableRef(req => ExpandObject(req, obj));
            varDesc.IndexedVariables = obj.GetVariableNames().Count;
        }
        return varDesc;
    }

    private IEnumerable<Variable> ExpandObject(RequestVariables req, Objects.DreamObject obj) {
        foreach (var (name, value) in obj.GetAllVariables()) {
            yield return DescribeValue(name, value);
        }
    }

    private void HandleRequestVariables(DebugAdapterClient client, RequestVariables requestVariables) {
        if (!variableReferences.TryGetValue(requestVariables.Arguments.VariablesReference, out var varFunc)) {
            requestVariables.RespondError(client, $"No variables reference with ID {requestVariables.Arguments.VariablesReference}");
            return;
        }

        requestVariables.Respond(client, varFunc(requestVariables));
    }
}

interface IDreamDebugManager {
    bool Stopped { get; }

    public void Initialize(int port);
    public void Update();
    public void Shutdown();

    public void HandleOutput(LogLevel logLevel, string message);
    public void HandleProcStart(DMProcState state);
    public void HandleLineChange(DMProcState state, int line);
}
