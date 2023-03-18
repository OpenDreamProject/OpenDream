using System.IO;
using System.Linq;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Procs.DebugAdapter.Protocol;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream.Procs;
using Robust.Server;

namespace OpenDreamRuntime.Procs.DebugAdapter;

sealed class DreamDebugManager : IDreamDebugManager {
    [Dependency] private readonly IDreamManager _dreamManager = default!;
    [Dependency] private readonly IDreamObjectTree _objectTree = default!;
    [Dependency] private readonly DreamResourceManager _resourceManager = default!;
    [Dependency] private readonly IProcScheduler _procScheduler = default!;
    [Dependency] private readonly IBaseServer _server = default!;

    // Setup
    private DebugAdapter? _adapter;
    private string RootPath => _resourceManager.RootPath ?? throw new Exception("No RootPath yet!");
    private bool _stopOnEntry = false;

    // State
    private bool _stopped = false;
    private bool _terminated = false;
    private DreamThread? _stoppedThread;

    public enum StepMode {
        None,
        StepOver,  // aka "next"
        StepIn,
        StepOut,
    }
    public struct ThreadStepMode {
        public StepMode Mode;
        public int FrameId;
        public string? Granularity;
    }

    // Breakpoint storage
    private const string ExceptionFilterRuntimes = "runtimes";
    private bool _breakOnRuntimes = true;

    private sealed class FileBreakpointSlot {
        public List<ActiveBreakpoint> Breakpoints = new();
        //public DMProc Proc;
        //public int BytecodeOffset;

        public FileBreakpointSlot() {}
    }

    private sealed class FunctionBreakpointSlot {
        public List<ActiveBreakpoint> Breakpoints = new();
        public FunctionBreakpointSlot() {}
    }

    private struct ActiveBreakpoint {
        public int Id;
        public string? Condition;
        public string? HitCondition;
        public string? LogMessage;
    }

    private int _breakpointIdCounter = 1;

    private Dictionary<string, Dictionary<int, FileBreakpointSlot>>? _possibleBreakpoints;
    private Dictionary<(string Type, string Proc), FunctionBreakpointSlot>? _possibleFunctionBreakpoints;
    private Dictionary<int, DMProc> _disassemblyProcs = new();

    // Temporary data for a given Stop
    private Exception? _exception;

    private Dictionary<int, WeakReference<ProcState>> _stackFramesById = new();

    private int _variablesIdCounter = 0;
    private Dictionary<int, Func<RequestVariables, IEnumerable<Variable>>> _variableReferences = new();
    private int AllocVariableRef(Func<RequestVariables, IEnumerable<Variable>> func) {
        int id = ++_variablesIdCounter;
        _variableReferences[id] = func;
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
            _stopped = false;
        }
    }

    public void Shutdown() {
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

    public void HandleFirstResume(DMProcState state) {
        if (_possibleFunctionBreakpoints == null)
            return;

        // Check for a function breakpoint
        List<int>? hit = null;
        if (_possibleFunctionBreakpoints.TryGetValue((state.Proc.OwningType.PathString, state.Proc.Name), out var slot)) {
            foreach (var bp in slot.Breakpoints) {
                if (TestBreakpoint(bp)) {
                    hit ??= new(1);
                    hit.Add(bp.Id);
                }
            }
        }

        if (hit != null) {
            Output($"Function breakpoint hit at {state.Proc.OwningType.PathString}::{state.Proc.Name}");
            Stop(state.Thread,
                new StoppedEvent {Reason = StoppedEvent.ReasonFunctionBreakpoint, HitBreakpointIds = hit});
            return;
        }

        // Otherwise check for an instruction breakpoint
        HandleInstruction(state);
    }

    public void HandleInstruction(DMProcState state) {
        // Stop if we're instruction stepping.
        bool stoppedOnStep = false;
        switch (state.Thread.StepMode) {
            case ThreadStepMode { Mode: StepMode.StepIn, FrameId: _, Granularity: SteppingGranularity.Instruction }:
                stoppedOnStep = true;
                break;
            case ThreadStepMode { Mode: StepMode.StepOut, FrameId: int whenNotInStack, Granularity: SteppingGranularity.Instruction }:
                stoppedOnStep = !state.Thread.InspectStack().Select(p => p.Id).Contains(whenNotInStack);
                break;
            case ThreadStepMode { Mode: StepMode.StepOver, FrameId: int whenTop, Granularity: SteppingGranularity.Instruction }:
                stoppedOnStep = state.Id == whenTop || !state.Thread.InspectStack().Select(p => p.Id).Contains(whenTop);
                break;
        }

        if (stoppedOnStep) {
            state.Thread.StepMode = null;
            Stop(state.Thread, new StoppedEvent {
                Reason = StoppedEvent.ReasonStep,
            });
        }
    }

    public void HandleLineChange(DMProcState state, int line) {
        if (_stopOnEntry) {
            _stopOnEntry = false;
            Stop(state.Thread, new StoppedEvent {
                Reason = StoppedEvent.ReasonEntry,
            });
            return;
        }

        bool stoppedOnStep = false;
        switch (state.Thread.StepMode) {
            case ThreadStepMode { Mode: StepMode.StepIn, FrameId: _, Granularity: null or SteppingGranularity.Line or SteppingGranularity.Statement }:
                stoppedOnStep = true;
                break;
            case ThreadStepMode { Mode: StepMode.StepOut, FrameId: int whenNotInStack, Granularity: null or SteppingGranularity.Line or SteppingGranularity.Statement }:
                stoppedOnStep = whenNotInStack == -1 || !state.Thread.InspectStack().Select(p => p.Id).Contains(whenNotInStack);
                break;
            case ThreadStepMode { Mode: StepMode.StepOver, FrameId: int whenTop, Granularity: null or SteppingGranularity.Line or SteppingGranularity.Statement }:
                stoppedOnStep = state.Id == whenTop || whenTop == -1 || !state.Thread.InspectStack().Select(p => p.Id).Contains(whenTop);
                break;
        }

        if (stoppedOnStep) {
            state.Thread.StepMode = null;
            Stop(state.Thread, new StoppedEvent {
                Reason = StoppedEvent.ReasonStep,
            });
            return;
        }

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
            Stop(state.Thread, new StoppedEvent {
                Reason = StoppedEvent.ReasonBreakpoint,
                HitBreakpointIds = hit,
            });
        }
    }

    public void HandleException(DreamThread thread, Exception exception) {
        if (_breakOnRuntimes) {
            _exception = exception;
            Output("Stopped on exception");
            Stop(thread, new StoppedEvent {
                Reason = StoppedEvent.ReasonException,
            });
        }
    }

    private bool TestBreakpoint(ActiveBreakpoint bp) => bp.Condition is null && bp.HitCondition is null;

    // Utilities
    private void Output(string message, string category = OutputEvent.CategoryConsole) {
        _adapter?.SendAll(new OutputEvent(category, $"{message}\n"));
    }

    private bool CanStop() => _adapter != null && _adapter.AnyClientsConnected() && !_terminated;

    private void Stop(DreamThread? thread, StoppedEvent stoppedEvent) {
        if (_adapter == null || !CanStop())
            return;

        _stoppedThread = thread;
        stoppedEvent.ThreadId = thread?.Id;
        stoppedEvent.AllThreadsStopped = true;
        _adapter.SendAll(stoppedEvent);

        _stopped = true;

        // We have to block the DM runtime no matter where we are in its call
        // stack. Unfortunately this also blocks the Robust engine.
        while (_stopped) {
            Update();
            System.Threading.Thread.Sleep(50);
        }
    }

    private void Resume() {
        _stopped = false;
        _stackFramesById.Clear();
        _variableReferences.Clear();
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
            case RequestSetFunctionBreakpoints reqFuncBreakpoints:
                HandleRequestSetFunctionBreakpoints(client, reqFuncBreakpoints);
                break;
            case RequestSetExceptionBreakpoints requestSetExceptionBreakpoints:
                HandleRequestSetExceptionBreakpoints(client, requestSetExceptionBreakpoints);
                break;
            case RequestConfigurationDone reqConfigDone:
                HandleRequestConfigurationDone(client, reqConfigDone);
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
            case RequestStepIn requestStepIn:
                HandleRequestStepIn(client, requestStepIn);
                break;
            case RequestNext requestNext:
                HandleRequestNext(client, requestNext);
                break;
            case RequestStepOut requestStepOut:
                HandleRequestStepOut(client, requestStepOut);
                break;
            case RequestDisassemble requestDisassemble:
                HandleRequestDisassemble(client, requestDisassemble);
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
            ExceptionBreakpointFilters = new[] {
                new ExceptionBreakpointsFilter(ExceptionFilterRuntimes, "Runtime errors") { Default = true },
            },
            SupportsDisassembleRequest = true,
            SupportsSteppingGranularity = true,
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
            .ToDictionary(group => group.Key, group => group.Distinct().ToDictionary(line => line, _ => new FileBreakpointSlot()));
        _possibleFunctionBreakpoints = IterateProcs()
            .Distinct()
            .ToDictionary(t => t, _ => new FunctionBreakpointSlot());

        _stopOnEntry = reqLaunch.Arguments.StopOnEntry is true;
        reqLaunch.Respond(client);
    }

    private IEnumerable<(string Source, int Line)> IteratePossibleBreakpoints() {
        foreach (var proc in _objectTree.Procs.Concat(new[] { _objectTree.GlobalInitProc }).OfType<DMProc>()) {
            string? source = proc.Source;
            if (source != null) {
                yield return (source, proc.Line);
            }
            foreach (var (_, instruction) in new ProcDecoder(_objectTree.Strings, proc.Bytecode).Disassemble()) {
                switch (instruction) {
                    case (DreamProcOpcode.DebugSource, string newSource):
                        source = newSource;
                        break;
                    case (DreamProcOpcode.DebugLine, int line):
                        yield return (source!, line);
                        break;
                }
            }
        }
    }

    private IEnumerable<(string Type, string Proc)> IterateProcs() {
        foreach (var proc in _objectTree.Procs) {
            yield return (proc.OwningType.PathString, proc.Name);
        }
    }

    private void HandleRequestConfigurationDone(DebugAdapterClient client, RequestConfigurationDone reqConfigDone) {
        _dreamManager.StartWorld();
        reqConfigDone.Respond(client);
        if (!_terminated) {
            client.SendMessage(new ODReadyEvent(IoCManager.Resolve<Robust.Shared.Network.IServerNetManager>().Port));
        }
    }

    private void HandleRequestDisconnect(DebugAdapterClient client, RequestDisconnect reqDisconnect) {
        // TODO: Don't terminate if launch type was "attach"
        reqDisconnect.Respond(client);
        _server.Shutdown("A shutdown was initiated by the debug adapter");
        _terminated = true;
        _stopped = false;
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

        sourcePath = Path.GetRelativePath(RootPath, sourcePath).Replace("\\", "/");
        if (_possibleBreakpoints is null || !_possibleBreakpoints.TryGetValue(sourcePath, out var fileSlots)) {
            // File isn't known - every breakpoint is invalid.
            for (int i = 0; i < responseBreakpoints.Length; ++i) {
                responseBreakpoints[i] = new Breakpoint(message: $"Unknown file \"{sourcePath}\"");
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
                    responseBreakpoints[i] = new(message: $"No code on line {setBreakpoint.Line}") { Line = setBreakpoint.Line };
                }
            }
        }

        reqSetBreakpoints.Respond(client, responseBreakpoints);
    }

    private void HandleRequestSetFunctionBreakpoints(DebugAdapterClient client, RequestSetFunctionBreakpoints reqFuncBreakpoints) {
        var input = reqFuncBreakpoints.Arguments.Breakpoints;
        var output = new Protocol.Breakpoint[input.Length];

        foreach (var v in _possibleFunctionBreakpoints!.Values) {
            v.Breakpoints.Clear();
        }

        for (int i = 0; i < input.Length; ++i) {
            var bp = input[i];

            string name = bp.Name.Replace("/proc/", "/").Replace("/verb/", "/");
            int last = name.LastIndexOf('/');
            string type = name[..last];
            string proc = name[(last + 1)..];
            if (type == "") {
                type = "/";
            }

            if (_possibleFunctionBreakpoints.GetValueOrDefault((type, proc)) is FunctionBreakpointSlot slot) {
                int id = ++_breakpointIdCounter;
                output[i] = new(id, verified: true);
                slot.Breakpoints.Add(new ActiveBreakpoint {
                    Id = id,
                    Condition = bp.Condition,
                    HitCondition = bp.HitCondition,
                });
            } else {
                output[i] = new(message: $"No proc {type}::{proc}");
            }
        }

        reqFuncBreakpoints.Respond(client, output);
    }

    private void HandleRequestSetExceptionBreakpoints(DebugAdapterClient client, RequestSetExceptionBreakpoints requestSetExceptionBreakpoints) {
        _breakOnRuntimes = requestSetExceptionBreakpoints.Arguments.Filters.Contains(ExceptionFilterRuntimes);
        requestSetExceptionBreakpoints.Respond(client, null);
    }

    private IEnumerable<DreamThread> InspectThreads() {
        return DreamThread.InspectExecutingThreads().Concat(_procScheduler.InspectThreads());
    }

    private void HandleRequestThreads(DebugAdapterClient client, RequestThreads reqThreads) {
        var threads = new List<Thread>();
        foreach (var thread in InspectThreads().Distinct().Where(x => x != null)) {
            threads.Add(new Thread(thread.Id, thread.Name));
        }
        if (!threads.Any()) {
            threads.Add(new Thread(0, "Nothing"));
        }
        reqThreads.Respond(client, threads);
    }

    private void HandleRequestPause(DebugAdapterClient client, RequestPause reqPause) {
        // "The debug adapter first sends the response and then a stopped event (with reason pause) after the thread has been paused successfully."
        reqPause.Respond(client);
        Stop(null, new StoppedEvent {
            Reason = StoppedEvent.ReasonPause,
            Description = "Paused by request",
        });
    }

    private void HandleRequestContinue(DebugAdapterClient client, RequestContinue reqContinue) {
        Resume();
        reqContinue.Respond(client, allThreadsContinued: true);
    }

    private void HandleRequestStepIn(DebugAdapterClient client, RequestStepIn requestStepIn) {
        var thread = InspectThreads().First(t => t.Id == requestStepIn.Arguments.ThreadId);
        thread.StepMode = new ThreadStepMode {
            Mode = StepMode.StepIn,
            Granularity = requestStepIn.Arguments.Granularity,
        };
        Resume();
        requestStepIn.Respond(client);
    }

    private void HandleRequestNext(DebugAdapterClient client, RequestNext requestNext) {
        var thread = InspectThreads().First(t => t.Id == requestNext.Arguments.ThreadId);
        thread.StepMode = new ThreadStepMode {
            Mode = StepMode.StepOver,
            FrameId = thread.InspectStack().FirstOrDefault()?.Id ?? -1,
            Granularity = requestNext.Arguments.Granularity,
        };
        Resume();
        requestNext.Respond(client);
    }

    private void HandleRequestStepOut(DebugAdapterClient client, RequestStepOut requestStepOut) {
        var thread = InspectThreads().First(t => t.Id == requestStepOut.Arguments.ThreadId);
        thread.StepMode = new ThreadStepMode {
            Mode = StepMode.StepOut,
            FrameId = thread.InspectStack().FirstOrDefault()?.Id ?? -1,
            Granularity = requestStepOut.Arguments.Granularity,
        };
        Resume();
        requestStepOut.Respond(client);
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
        var thread = InspectThreads().FirstOrDefault(t => t?.Id == reqStackTrace.Arguments.ThreadId);
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

            var outputFrame = new StackFrame {
                Id = frame.Id,
                Source = TranslateSource(source),
                Line = line ?? 0,
                Name = nameBuilder.ToString(),
            };
            if (frame is DMProcState dm) {
                outputFrame.InstructionPointerReference = EncodeInstructionPointer(dm.Proc, dm.ProgramCounter);
            }

            output.Add(outputFrame);
            _stackFramesById[frame.Id] = new WeakReference<ProcState>(frame);
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
        if (!_stackFramesById.TryGetValue(requestScopes.Arguments.FrameId, out var weak) || !weak.TryGetTarget(out var frame)) {
            requestScopes.RespondError(client, $"No frame with ID {requestScopes.Arguments.FrameId}");
            return;
        }

        if (frame is not DMProcState dmFrame) {
            requestScopes.RespondError(client, $"Cannot inspect native frame");
            return;
        }

        var scopes = new List<Scope>(4);
        var stack = dmFrame.DebugStack();
        if (stack.Length > 0) {
            // Only show the Stack as a scope if there is anything worth showing,
            // which will usually only be when stepping by instruction.
            scopes.Add(new Scope {
                Name = "Stack",
                VariablesReference = AllocVariableRef(req => ExpandStack(req, stack)),
                PresentationHint = Scope.PresentationHintRegisters,
            });
        }
        scopes.Add(new Scope {
            Name = "Arguments",
            PresentationHint = Scope.PresentationHintArguments,
            VariablesReference = AllocVariableRef(req => ExpandArguments(req, dmFrame)),
        });
        scopes.Add(new Scope {
            Name = "Locals",
            PresentationHint = Scope.PresentationHintLocals,
            VariablesReference = AllocVariableRef(req => ExpandLocals(req, dmFrame)),
        });
        scopes.Add(new Scope {
            Name = "Globals",
            VariablesReference = AllocVariableRef(req => ExpandGlobals(req)),
        });

        requestScopes.Respond(client, scopes);
    }

    private IEnumerable<Variable> ExpandArguments(RequestVariables req, DMProcState dmFrame) {
        if (dmFrame.Proc.OwningType != OpenDreamShared.Dream.DreamPath.Root) {
            yield return DescribeValue("src", new(dmFrame.Instance));
        }
        yield return DescribeValue("usr", new(dmFrame.Usr));
        foreach (var (name, value) in dmFrame.DebugArguments()) {
            yield return DescribeValue(name, value);
        }
    }

    private IEnumerable<Variable> ExpandLocals(RequestVariables req, DMProcState dmFrame) {
        foreach (var (name, value) in dmFrame.DebugLocals()) {
            yield return DescribeValue(name, value);
        }
    }

    private IEnumerable<Variable> ExpandGlobals(RequestVariables req) {
        foreach (var (name, value) in _dreamManager.GlobalNames.Zip(_dreamManager.Globals)) {
            yield return DescribeValue(name, value);
        }
    }

    private IEnumerable<Variable> ExpandStack(RequestVariables req, ReadOnlyMemory<DreamValue> stack) {
        for (int i = stack.Length - 1; i >= 0; --i) {
            yield return DescribeValue($"{i}", stack.Span[i]);
        }
    }

    private Variable DescribeValue(string name, DreamValue value) {
        var varDesc = new Variable { Name = name };
        varDesc.Value = value.ToString();
        if (value.TryGetValueAsDreamList(out var list) && list != null) {
            if (list.GetLength() > 0) {
                varDesc.VariablesReference = AllocVariableRef(req => ExpandList(req, list));
                varDesc.IndexedVariables = list.GetLength() * (list.IsAssociative ? 2 : 1);
            }
        } else if (value.TryGetValueAsDreamObject(out var obj) && obj != null) {
            varDesc.VariablesReference = AllocVariableRef(req => ExpandObject(req, obj));
            varDesc.NamedVariables = obj.ObjectDefinition?.Variables.Count;
        } else if (value.TryGetValueAsProcArguments(out var procArgs)) {
            varDesc.VariablesReference = AllocVariableRef(req => ExpandProcArguments(req, procArgs));
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
        foreach (var name in obj.GetVariableNames().OrderBy(k => k)) {
            Variable described;
            try {
                described = DescribeValue(name, obj.GetVariable(name));
            } catch (Exception ex) {
                Logger.ErrorS("debug", ex, $"Error in GetVariable({name})");
                described = new Variable {
                    Name = name,
                    Value = $"<error: {ex.Message}>",
                };
            }
            yield return described;
        }
    }

    private IEnumerable<Variable> ExpandProcArguments(RequestVariables req, DreamProcArguments procArgs) {
        if (procArgs.OrderedArguments != null) {
            foreach (var (i, arg) in procArgs.OrderedArguments.Select((x, i) => (i, x))) {
                yield return DescribeValue(i.ToString(), arg);
            }
        }
        if (procArgs.NamedArguments != null) {
            foreach (var (name, arg) in procArgs.NamedArguments) {
                yield return DescribeValue(name, arg);
            }
        }
    }

    private void HandleRequestVariables(DebugAdapterClient client, RequestVariables requestVariables) {
        if (!_variableReferences.TryGetValue(requestVariables.Arguments.VariablesReference, out var varFunc)) {
            // When stepping quickly, we may receive such requests for old scopes we've already dropped.
            // Fail silently instead of loudly to avoid spamming error messages.
            requestVariables.Respond(client, Enumerable.Empty<Variable>());
            return;
        }

        requestVariables.Respond(client, varFunc(requestVariables));
    }

    private void HandleRequestDisassemble(DebugAdapterClient client, RequestDisassemble requestDisassemble) {
        var (proc, pc) = DecodeInstructionPointer(requestDisassemble.Arguments.MemoryReference);
        // If the user scrolled really far up/down, serve nothing forever.
        if (proc == null) {
            if (pc == 0xffffffff) {
                requestDisassemble.Respond(client, Enumerable.Repeat(HighInstruction, requestDisassemble.Arguments.InstructionCount));
            } else {
                requestDisassemble.Respond(client, Enumerable.Repeat(LowInstruction, requestDisassemble.Arguments.InstructionCount));
            }
            return;
        }

        // Just disassemble the whole function...
        List<DisassembledInstruction> output = new();
        DisassembledInstruction? previousInstruction = null;
        int previousOffset = 0;
        foreach (var (offset, instruction) in new ProcDecoder(_objectTree.Strings, proc.Bytecode).Disassemble()) {
            if (previousInstruction != null) {
                previousInstruction.InstructionBytes = BitConverter.ToString(proc.Bytecode, previousOffset, offset - previousOffset).Replace("-", " ").ToLowerInvariant();
            }
            previousOffset = offset;
            previousInstruction = new DisassembledInstruction {
                Address = EncodeInstructionPointer(proc, offset),
                Instruction = ProcDecoder.Format(instruction, type => _objectTree.Types[type].Path.ToString()),
            };
            switch (instruction) {
                case (DreamProcOpcode.DebugSource, string source):
                    previousInstruction.Location = TranslateSource(source);
                    break;
                case (DreamProcOpcode.DebugLine, int line):
                    previousInstruction.Line = line;
                    break;
            }
            output.Add(previousInstruction);
        }
        if (previousInstruction != null) {
            previousInstruction.InstructionBytes = BitConverter.ToString(proc.Bytecode, previousOffset).Replace("-", " ").ToLowerInvariant();
        }
        if (output.Count > 0) {
            output[0].Symbol = proc.ToString();
            if (output[0].Location is null) {
                output[0].Location = TranslateSource(proc.Source);
            }
            if (output[0].Line is null) {
                output[0].Line = proc.Line;
            }
        }

        // ... and THEN strip everything outside the requested range.
        int requestedPoint = output.FindIndex(di => di.Address == requestDisassemble.Arguments.MemoryReference);
        requestDisassemble.Respond(client, DisassemblySkipTake(output, requestedPoint, requestDisassemble.Arguments.InstructionOffset ?? 0, requestDisassemble.Arguments.InstructionCount));
    }

    private IEnumerable<DisassembledInstruction> DisassemblySkipTake(List<DisassembledInstruction> list, int midpoint, int offset, int count) {
        for (int i = midpoint + offset; i < midpoint + offset + count; ++i) {
            if (i < 0) {
                yield return LowInstruction;
            } else if (i < list.Count) {
                yield return list[i];
            } else {
                yield return HighInstruction;
            }
        }
    }

    private static readonly DisassembledInstruction LowInstruction = new DisassembledInstruction { Address = "0x1", Instruction = "" };
    private static readonly DisassembledInstruction HighInstruction = new DisassembledInstruction { Address = "0xffffffffffffffff", Instruction = "" };

    private string EncodeInstructionPointer(DMProc proc, int pc) {
        // VSCode requires that the instruction pointer is parseable as a BigInt
        // so that it can use lt/gt/eq comparisons to avoid refetching memory.
        // Otherwise it will think they're all the same and never refetch.
        int procId = proc.GetHashCode();
        _disassemblyProcs[procId] = proc;
        ulong ip = ((ulong)(uint)procId << 32) | (ulong)(uint)pc;
        return "0x" + ip.ToString("x");
    }

    private (DMProc? proc, uint pc) DecodeInstructionPointer(string ip) {
        ulong ip2 = ulong.Parse(ip[2..], System.Globalization.NumberStyles.HexNumber);
        _disassemblyProcs.TryGetValue((int)((ip2 & 0xffffffff00000000) >> 32), out var proc);
        return (proc, (uint)(ip2 & 0xffffffff));
    }
}

internal interface IDreamDebugManager {
    public void Initialize(int port);
    public void Update();
    public void Shutdown();

    public void HandleOutput(LogLevel logLevel, string message);
    public void HandleFirstResume(DMProcState dMProcState);
    public void HandleInstruction(DMProcState dMProcState);
    public void HandleLineChange(DMProcState state, int line);
    public void HandleException(DreamThread dreamThread, Exception exception);
}
