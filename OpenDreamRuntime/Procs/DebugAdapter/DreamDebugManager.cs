using System.IO;
using System.Linq;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Procs.DebugAdapter.Protocol;
using OpenDreamRuntime.Resources;
using Robust.Server;

namespace OpenDreamRuntime.Procs.DebugAdapter;

sealed class DreamDebugManager : IDreamDebugManager {
    [Dependency] private readonly IDreamManager _dreamManager = default!;
    [Dependency] private readonly DreamResourceManager _resourceManager = default!;
    [Dependency] private readonly IProcScheduler _procScheduler = default!;
    [Dependency] private readonly IBaseServer _server = default!;

    // Setup
    private DebugAdapter? _adapter;
    private string RootPath => _resourceManager.RootPath ?? throw new Exception("No RootPath yet!");

    // State
    private bool Stopped = false;
    private bool _terminated = false;

    // Breakpoint storage
    private struct FileBreakpointSlot {
        public List<ActiveBreakpoint> Breakpoints;
        public DMProc Proc;
        public int BytecodeOffset;
    }

    private struct FunctionBreakpointSlot {
        public List<ActiveBreakpoint> Breakpoints;
    }

    private struct ActiveBreakpoint {
        public int Id;
        public string? Condition;
        public string? HitCondition;
        public string? LogMessage;
    }

    private int _breakpointIdCounter = 1;
    private readonly Dictionary<string, List<FileBreakpointSlot>> _breakpoints = new();

    private Dictionary<string, Dictionary<int, FileBreakpointSlot>>? _possibleBreakpoints;
    //private Dictionary<(string Type, string Proc), FunctionBreakpointSlot>? _possibleFunctionBreakpoints;

    private ILookup<(string Type, string Proc), ActiveBreakpoint>? functionBreakpoints;

    // Temporary data for a given Stop
    private Exception? _exception;

    private Dictionary<int, WeakReference<ProcState>> stackFramesById = new();

    private int _variablesIdCounter = 0;
    private Dictionary<int, Func<RequestVariables, IEnumerable<Variable>>> variableReferences = new();
    private int AllocVariableRef(Func<RequestVariables, IEnumerable<Variable>> func) {
        int id = ++_variablesIdCounter;
        variableReferences[id] = func;
        return id;
    }

    // Lifecycle
    public void Initialize(int port) {
        _adapter = new DebugAdapter();

        _adapter.OnClientConnected += OnClientConnected;
        //_adapter.StartListening();
        _adapter.ConnectOut(port: port);
    }

    public void Update() {
        _adapter?.HandleMessages();
        if (!CanStop()) {
            Stopped = false;
        }
    }

    public void Shutdown() {
        _breakpoints.Clear();
        _breakpointIdCounter = 0;
        _adapter?.Shutdown();
    }

    // Callbacks from the runtime
    public void HandleOutput(LogLevel logLevel, string message) {
        string category = logLevel switch {
            LogLevel.Fatal or LogLevel.Error => OutputEvent.CategoryStderr,
            _ => OutputEvent.CategoryStdout,
        };
        Output(message, category);
    }

    public void HandleLineChange(DMProcState state, int line) {
        if (state.CurrentSource is null || _possibleBreakpoints is null || !_possibleBreakpoints.TryGetValue(state.CurrentSource, out var fileSlots))
            return;

        var hit = new List<int>();
        foreach (var bp in fileSlots[line].Breakpoints ?? Enumerable.Empty<ActiveBreakpoint>()) {
            if (TestBreakpoint(bp)) {
                hit.Add(bp.Id);
            }
        }
        if (hit.Any()) {
            Output($"Breakpoint hit at {state.CurrentSource}:{line}");
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
                if (TestBreakpoint(bp)) {
                    hit.Add(bp.Id);
                }
            }
        }
        if (hit.Any()) {
            Output($"Function breakpoint hit at {state.Proc.OwningType.PathString}::{state.Proc.Name}");
            Stop(new StoppedEvent {
                Reason = StoppedEvent.ReasonFunctionBreakpoint,
                ThreadId = state.Thread.Id,
                AllThreadsStopped = true,
                HitBreakpointIds = hit,
            });
        }
    }

    public void HandleException(DreamThread thread, Exception exception) {
        _exception = exception;
        Output("Stopped on exception");
        Stop(new StoppedEvent {
            Reason = StoppedEvent.ReasonException,
            ThreadId = thread.Id,
            AllThreadsStopped = true,
        });
    }

    private bool TestBreakpoint(ActiveBreakpoint bp) => bp.Condition is null && bp.HitCondition is null;

    // Utilities
    private void Output(string message, string category = OutputEvent.CategoryConsole) {
        _adapter?.SendAll(new OutputEvent(category, $"{message}\n"));
    }

    private bool CanStop() => _adapter != null && _adapter.AnyClientsConnected() && !_terminated;

    private void Stop(StoppedEvent stoppedEvent) {
        if (_adapter == null || !CanStop())
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

    // DAP request handlers
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
            case RequestExceptionInfo requestExceptionInfo:
                HandleRequestExceptionInfo(client, requestExceptionInfo);
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
            SupportsExceptionInfoRequest = true,
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

        _dreamManager.PreInitialize(reqLaunch.Arguments.JsonPath);

        _possibleBreakpoints = IteratePossibleBreakpoints()
            .GroupBy(pair => pair.Source, pair => pair.Line)
            .ToDictionary(group => group.Key, group => group.ToDictionary(line => line, _ => new FileBreakpointSlot()));

        reqLaunch.Respond(client);
    }

    private IEnumerable<(string Source, int Line)> IteratePossibleBreakpoints() {
        foreach (var proc in _dreamManager.ObjectTree.Procs) {
        }
        yield break;
    }

    private void HandleRequestConfigurationDone(DebugAdapterClient client, RequestConfigurationDone reqConfigDone) {
        _dreamManager.StartWorld();
        reqConfigDone.Respond(client);
        client.SendMessage(new ODReadyEvent(IoCManager.Resolve<Robust.Shared.Network.IServerNetManager>().Port));
    }

    private void HandleRequestDisconnect(DebugAdapterClient client, RequestDisconnect reqDisconnect) {
        // TODO: Don't terminate if launch type was "attach"
        reqDisconnect.Respond(client);
        _server.Shutdown("A shutdown was initiated by the debug adapter");
        _terminated = true;
        Stopped = false;
    }

    private void HandleRequestSetBreakpoints(DebugAdapterClient client, RequestSetBreakpoints reqSetBreakpoints) {
        Source source = reqSetBreakpoints.Arguments.Source;
        string? sourcePath = source.Path ?? source.Name;
        if (sourcePath == null) {
            reqSetBreakpoints.RespondError(client, "A breakpoint source was not given");
            return;
        }

        var setBreakpoints = reqSetBreakpoints.Arguments.Breakpoints;
        var responseBreakpoints = new Protocol.Breakpoint[setBreakpoints?.Length ?? 0];

        sourcePath = Path.GetRelativePath(RootPath, sourcePath);
        if (_possibleBreakpoints is null || !_possibleBreakpoints.TryGetValue(sourcePath, out var fileSlots)) {
            // File isn't known - every breakpoint is invalid.
            for (int i = 0; i < responseBreakpoints.Length; ++i) {
                responseBreakpoints[i] = new Breakpoint($"Unknown file \"{sourcePath}\"");
            }
            reqSetBreakpoints.Respond(client, responseBreakpoints);
            return;
        }

        foreach (var slot in fileSlots.Values) {
            slot.Breakpoints.Clear();
        }

        // We've unset old breakpoints, so set new ones if needed.
        if (setBreakpoints != null) {
            for (int i = 0; i < setBreakpoints.Length; i++) {
                SourceBreakpoint setBreakpoint = setBreakpoints[i];
                if (fileSlots.TryGetValue(setBreakpoint.Line, out var slot)) {
                    int id = ++_breakpointIdCounter;

                    slot.Breakpoints.Add(new ActiveBreakpoint {
                        Id = id,
                        Condition = setBreakpoint.Condition,
                        HitCondition = setBreakpoint.HitCondition,
                        LogMessage = setBreakpoint.LogMessage,
                    });

                    responseBreakpoints[i] = new(id, source, setBreakpoint.Line);
                } else {
                    responseBreakpoints[i] = new($"No breakpoint slot on line {setBreakpoint.Line}");
                }
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
            var toSave = new List<(string Type, string Proc, ActiveBreakpoint Breakpoint)>();

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
                // TODO: refuse verification for breakpoints for procs that don't exist
                output[i] = new(id, verified: true);
                toSave.Add((type, proc, new ActiveBreakpoint {
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

    private void HandleRequestExceptionInfo(DebugAdapterClient client, RequestExceptionInfo requestExceptionInfo) {
        if (_exception is null) {
            requestExceptionInfo.RespondError(client, "No exception");
            return;
        }

        // VSC shows exceptionId, description, stackTrace in that order.
        requestExceptionInfo.Respond(client, new RequestExceptionInfo.ExceptionInfoResponse {
            ExceptionId = _exception.Message,
            BreakMode = RequestExceptionInfo.ExceptionInfoResponse.BreakModeAlways,
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
                Source = TranslateSource(source),
                Line = line ?? 0,
                Name = nameBuilder.ToString(),
            });
            stackFramesById[frame.Id] = new WeakReference<ProcState>(frame);
        }
        reqStackTrace.Respond(client, output, output.Count);
    }

    private Source? TranslateSource(string? source) {
        if (source is null)
            return null;

        var fname = Path.GetFileName(source);
        if (Path.IsPathRooted(source)) {
            return new Source(fname, source);
        } else {
            return new Source(fname, Path.Join(RootPath, source));
        }
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
        if (value.TryGetValueAsDreamList(out var list) && list != null) {
            varDesc.VariablesReference = AllocVariableRef(req => ExpandList(req, list));
            varDesc.IndexedVariables = list.GetLength() * (list.IsAssociative ? 2 : 1);
        } else if (value.TryGetValueAsDreamObject(out var obj) && obj != null) {
            varDesc.VariablesReference = AllocVariableRef(req => ExpandObject(req, obj));
            varDesc.NamedVariables = obj.ObjectDefinition?.Variables.Count;
        }
        return varDesc;
    }

    private IEnumerable<Variable> ExpandList(RequestVariables req, DreamList list) {
        if (list.IsAssociative) {
            var assoc = list.GetAssociativeValues();
            foreach (var (i, key) in list.GetValues().Select((v, i) => (i + 1, v)).Skip(req.Arguments.Start ?? 0).Take((req.Arguments.Count ?? int.MaxValue) / 2)) {
                assoc.TryGetValue(key, out var value);
                yield return DescribeValue($"keys[{i}]", key);
                yield return DescribeValue($"vals[{i}]", value);
            }
        } else {
            foreach (var (i, value) in list.GetValues().Select((v, i) => (i + 1, v)).Skip(req.Arguments.Start ?? 0).Take(req.Arguments.Count ?? int.MaxValue)) {
                yield return DescribeValue($"[{i}]", value);
            }
        }
    }

    private IEnumerable<Variable> ExpandObject(RequestVariables req, DreamObject obj) {
        foreach (var (name, value) in obj.GetAllVariables().OrderBy(kvp => kvp.Key)) {
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
    public void Initialize(int port);
    public void Update();
    public void Shutdown();

    public void HandleOutput(LogLevel logLevel, string message);
    public void HandleProcStart(DMProcState state);
    public void HandleLineChange(DMProcState state, int line);
    public void HandleException(DreamThread dreamThread, Exception exception);
}
