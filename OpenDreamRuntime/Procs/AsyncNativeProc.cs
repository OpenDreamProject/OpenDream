using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using DMCompiler.DM;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Resources;
using Dependency = Robust.Shared.IoC.DependencyAttribute;

namespace OpenDreamRuntime.Procs {
    public sealed class AsyncNativeProc : DreamProc {
        public sealed class State : ProcState {
            public static readonly Stack<State> Pool = new();

            // IoC dependencies instead of proc fields because _proc can be null
            [Dependency] public readonly DreamManager DreamManager = default!;
            [Dependency] public readonly DreamResourceManager ResourceManager = default!;
            [Dependency] public readonly DreamObjectTree ObjectTree = default!;
            [Dependency] public readonly ProcScheduler ProcScheduler = default!;

            public DreamObject? Src;
            public DreamObject? Usr;

            private readonly DreamValue[] _arguments = new DreamValue[128];
            private int _argumentCount;

            private AsyncNativeProc? _proc;
            public override DreamProc? Proc => _proc;

            public override (string SourceFile, int Line) TracyLocationId => ("Native Proc", 0);

            private Func<State, Task<DreamValue>> _taskFunc;
            private Task? _task;

            private ProcState? _callProcNotify;
            private TaskCompletionSource<DreamValue>? _callTcs;
            private DreamValue? _callResult;

            private bool _inResume;

            public State() {
                IoCManager.InjectDependencies(this);
            }

            public void Initialize(AsyncNativeProc? proc, Func<State, Task<DreamValue>> taskFunc, DreamThread thread, DreamObject? src, DreamObject? usr, DreamProcArguments arguments) {
                base.Initialize(thread, true);

                _proc = proc;
                _taskFunc = taskFunc;
                Src = src;
                Usr = usr;
                arguments.Values.CopyTo(_arguments);
                _argumentCount = arguments.Count;
            }

            // Used to avoid reentrant resumptions in our proc
            public void SafeResume() {
                if (_inResume) {
                    return;
                }

                Thread.Resume();
            }

            public Task<DreamValue> Call(DreamProc proc, DreamObject? src, DreamObject? usr, params DreamValue[] arguments) {
                _callTcs = new();
                _callProcNotify = proc.CreateState(Thread, src, usr, new DreamProcArguments(arguments));

                // The field may be mutated by SafeResume, so cache the task
                var callTcs = _callTcs;
                SafeResume();
                return callTcs.Task;
            }

            public Task<DreamValue> CallNoWait(DreamProc proc, DreamObject src, DreamObject? usr, params DreamValue[] arguments) {
                _callTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
                _callProcNotify = proc.CreateState(Thread, src, usr, new DreamProcArguments(arguments));
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

                Src = null!;
                Usr = null!;
                _argumentCount = 0;
                _proc = null!;
                _taskFunc = null!;
                _task = null;
                _callProcNotify = null;
                _callTcs = null!;
                _callResult = null;
                _inResume = false;

                Pool.Push(this);
            }

            public override ProcStatus Resume() {
                _inResume = true;

                // We've just been created, start our task
                if (_task == null) {
                    _task = ProcScheduler.Schedule(this, _taskFunc);
                }

                // If the task is finished, we're all done
                if (_task.IsCompleted) {
                    if (_task.Exception != null) {
                        throw _task.Exception;
                    }

                    return ProcStatus.Returned;
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public DreamValue GetArgument(int argumentPosition, string argumentName) {
                if (argumentPosition < _argumentCount && _arguments[argumentPosition] != DreamValue.Null)
                    return _arguments[argumentPosition];

                return _proc?._defaultArgumentValues?.TryGetValue(argumentName, out var argValue) == true ? argValue : DreamValue.Null;
            }
        }

        private readonly Dictionary<string, DreamValue>? _defaultArgumentValues;
        private readonly Func<State, Task<DreamValue>> _taskFunc;

        public AsyncNativeProc(int id, TreeEntry owningType, string name, List<string> argumentNames, Dictionary<string, DreamValue> defaultArgumentValues, Func<State, Task<DreamValue>> taskFunc)
            : base(id, owningType, name, null, ProcAttributes.None, argumentNames, null, null, null, null, null, 0) {
            _defaultArgumentValues = defaultArgumentValues;
            _taskFunc = taskFunc;
        }

        public override ProcState CreateState(DreamThread thread, DreamObject? src, DreamObject? usr, DreamProcArguments arguments) {
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

            state.Initialize(null, taskFunc, thread, null, null, new DreamProcArguments());
            return state;
        }
    }
}
