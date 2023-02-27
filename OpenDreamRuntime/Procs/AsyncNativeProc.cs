using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;

namespace OpenDreamRuntime.Procs {
    public sealed class AsyncNativeProc : DreamProc {
        public sealed class State : ProcState {
            public static readonly Stack<State> Pool = new();

            public DreamObject? Src;
            public DreamObject? Usr;
            public DreamProcArguments Arguments;

            private AsyncNativeProc? _proc;
            public override DreamProc? Proc => _proc;

            public IDreamManager DreamManager => _proc._dreamManager;
            public DreamResourceManager ResourceManager => _proc._resourceManager;
            public IDreamObjectTree ObjectTree => _proc._objectTree;

            private Func<State, Task<DreamValue>> _taskFunc;
            private Task? _task;
            private CancellationTokenSource? _scheduleCancellationToken;

            private ProcState? _callProcNotify;
            private TaskCompletionSource<DreamValue>? _callTcs;
            private DreamValue? _callResult;

            private bool _inResume;

            public void Initialize(AsyncNativeProc? proc, Func<State, Task<DreamValue>> taskFunc, DreamThread thread, DreamObject? src, DreamObject? usr, DreamProcArguments arguments) {
                base.Initialize(thread, true);

                _proc = proc;
                _taskFunc = taskFunc;
                Src = src;
                Usr = usr;
                Arguments = arguments;
            }

            // Used to avoid reentrant resumptions in our proc
            public void SafeResume() {
                if (_inResume) {
                    return;
                }

                Thread.Resume();
            }

            public Task<DreamValue> Call(DreamProc proc, DreamObject src, DreamObject usr, DreamProcArguments arguments) {
                _callTcs = new();
                _callProcNotify = proc.CreateState(Thread, src, usr, arguments);

                // The field may be mutated by SafeResume, so cache the task
                var callTcs = _callTcs;
                SafeResume();
                return callTcs.Task;
            }

            public Task<DreamValue> CallNoWait(DreamProc proc, DreamObject src, DreamObject usr, DreamProcArguments arguments) {
                _callTcs = new();
                _callProcNotify = proc.CreateState(Thread, src, usr, arguments);
                _callProcNotify.WaitFor = false;

                // The field may be mutated by SafeResume, so cache the task
                var callTcs = _callTcs;
                SafeResume();
                return callTcs.Task;
            }

            public override void ReturnedInto(DreamValue value) {
                // We don't call `_callTcs.SetResult` here because we're about to be resumed and can do it there.
                _callResult = value;
            }

            public override void Cancel() {
                _callTcs?.SetCanceled();
            }

            public override void Dispose() {
                base.Dispose();

                // Cancel the scheduled continuation of this state if there is one
                _scheduleCancellationToken?.Cancel();
                _scheduleCancellationToken?.Dispose();
                _scheduleCancellationToken = null;

                Src = null!;
                Usr = null!;
                Arguments = default;
                _proc = null!;
                _taskFunc = null!;
                _task = null;
                _callProcNotify = null;
                _callTcs = null!;
                _callResult = null;
                _inResume = false;

                Pool.Push(this);
            }

            private async Task InternalResumeAsync() {
                Result = await _taskFunc(this);
            }

            public override ProcStatus Resume() {
                _inResume = true;

                // We've just been created, start our task
                if (_task == null) {
                    // Pull execution of our task outside of StartNew to allow it to inline here
                    _task = InternalResumeAsync();

                    // Shortcut: If our proc was synchronous, we don't need to schedule
                    //           This also means we won't reach Resume on a finished proc through our continuation
                    if (_task.IsCompleted) {
                        if (_task.Exception != null) {
                            throw _task.Exception;
                        }

                        return ProcStatus.Returned;
                    }

                    IProcScheduler procScheduler = IoCManager.Resolve<IProcScheduler>();
                    _scheduleCancellationToken = procScheduler.Schedule(this, _task);
                }

                // We need to call a proc.
                if (_callProcNotify != null) {
                    var callProcNotify = _callProcNotify;
                    _callProcNotify = null;

                    Thread.PushProcState(callProcNotify);
                    return ProcStatus.Called;
                }

                // We've just finished calling a proc, notify our task
                if (_callResult != null) {
                    var callTcs = _callTcs;
                    var callResult = _callResult.Value;
                    _callTcs = null;
                    _callResult = null;

                    callTcs.SetResult(callResult);
                }

                // If the task is finished, we're all done
                if (_task.IsCompleted) {
                    if (_task.Exception != null) {
                        throw _task.Exception;
                    }

                    return ProcStatus.Returned;
                }

                // Otherwise, we are still pending
                _inResume = false;
                return Thread.HandleDefer();
            }

            public override void AppendStackFrame(StringBuilder builder) {
                if (_proc == null) {
                    builder.Append("<anonymous async proc>");
                    return;
                }

                builder.Append($"{_proc}");
            }
        }

        private readonly IDreamManager _dreamManager;
        private readonly DreamResourceManager _resourceManager;
        private readonly IDreamObjectTree _objectTree;

        private readonly Dictionary<string, DreamValue> _defaultArgumentValues;
        private readonly Func<State, Task<DreamValue>> _taskFunc;

        public AsyncNativeProc(DreamPath owningType, string name, List<String> argumentNames, Dictionary<string, DreamValue> defaultArgumentValues, Func<State, Task<DreamValue>> taskFunc, IDreamManager dreamManager, DreamResourceManager resourceManager, IDreamObjectTree objectTree)
            : base(owningType, name, null, ProcAttributes.None, argumentNames, null, null, null, null, null) {
            _defaultArgumentValues = defaultArgumentValues;
            _taskFunc = taskFunc;

            _dreamManager = dreamManager;
            _resourceManager = resourceManager;
            _objectTree = objectTree;
        }

        public override ProcState CreateState(DreamThread thread, DreamObject? src, DreamObject? usr, DreamProcArguments arguments) {
            if (_defaultArgumentValues != null) {
                var newNamedArguments = arguments.NamedArguments;
                foreach (KeyValuePair<string, DreamValue> defaultArgumentValue in _defaultArgumentValues) {
                    int argumentIndex = ArgumentNames.IndexOf(defaultArgumentValue.Key);

                    if (arguments.GetArgument(argumentIndex, defaultArgumentValue.Key) == DreamValue.Null) {
                        newNamedArguments ??= new();
                        newNamedArguments.Add(defaultArgumentValue.Key, defaultArgumentValue.Value);
                    }
                }
                arguments = new DreamProcArguments(arguments.OrderedArguments, newNamedArguments);
            }

            if (!State.Pool.TryPop(out var state)) {
                state = new State();
            }

            state.Initialize(this, _taskFunc, thread, src, usr, arguments);
            return state;
        }

        public static ProcState CreateAnonymousState(DreamThread thread, Func<State, Task<DreamValue>> taskFunc) {
            if (!State.Pool.TryPop(out var state)) {
                state = new State();
            }

            state.Initialize(null, taskFunc, thread, null, null, new DreamProcArguments(null));
            return state;
        }
    }
}
