using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using OpenDreamRuntime.Objects;
using OpenDreamShared.Dream.Procs;

namespace OpenDreamRuntime.Procs {
    public class AsyncNativeProc : DreamProc {

        public delegate Task<DreamValue> HandlerFn(State s);

        public class State : ProcState
        {
            public DreamObject Src;
            public DreamObject Usr;
            public DreamProcArguments Arguments;

            private AsyncNativeProc _proc;
            public override DreamProc Proc => _proc;

            private Func<State, Task<DreamValue>> _taskFunc;
            private Task _task;

            private ProcState _callProcNotify;
            private TaskCompletionSource<DreamValue> _callTcs;
            private DreamValue? _callResult;

            private bool _inResume;

            public State(AsyncNativeProc proc, Func<State, Task<DreamValue>> taskFunc, DreamThread thread, DreamObject src, DreamObject usr, DreamProcArguments arguments)
                : base(thread)
            {
                _proc = proc;
                _taskFunc = taskFunc;
                Src = src;
                Usr = usr;
                Arguments = arguments;
            }

            // Used to avoid reentrant resumptions in our proc
            protected void SafeResume() {
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

            public override void ReturnedInto(DreamValue value) {
                // We don't call `_callTcs.SetResult` here because we're about to be resumed and can do it there.
                _callResult = value;
            }

            private async Task InternalResumeAsync() {
                Result = await _taskFunc(this);
            }

            protected override ProcStatus InternalResume()
            {
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

                    // We have to resume now so that the execution context knows we have returned
                    // This should lead to `return ProcStatus.Returned` inside `InternalResume`.
                    _task.ContinueWith(
                        (_, inst) => ((State)inst).SafeResume(),
                        this,
                        TaskScheduler.FromCurrentSynchronizationContext());
                }

                // We need to call a proc.
                while (_callProcNotify != null || _callResult != null)
                {
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

            public override void AppendStackFrame(StringBuilder builder)
            {
                if (_proc == null) {
                    builder.Append("<anonymous async proc>");
                    return;
                }

                builder.Append($"{_proc.Name}(...)");
            }
        }

        private Dictionary<string, DreamValue> _defaultArgumentValues;
        private Func<State, Task<DreamValue>> _taskFunc;

        private AsyncNativeProc()
            : base("<anonymous async proc>", null, false, null, null)
        {}

        public AsyncNativeProc(string name, DreamProc superProc, List<String> argumentNames, List<DMValueType> argumentTypes, Dictionary<string, DreamValue> defaultArgumentValues, Func<State, Task<DreamValue>> taskFunc)
            : base(name, superProc, true, argumentNames, argumentTypes)
        {
            _defaultArgumentValues = defaultArgumentValues;
            _taskFunc = taskFunc;
        }

        public override ProcState CreateState(DreamThread thread, DreamObject src, DreamObject usr, DreamProcArguments arguments)
        {
            if (_defaultArgumentValues != null) {
                foreach (KeyValuePair<string, DreamValue> defaultArgumentValue in _defaultArgumentValues) {
                    int argumentIndex = ArgumentNames.IndexOf(defaultArgumentValue.Key);

                    if (arguments.GetArgument(argumentIndex, defaultArgumentValue.Key) == DreamValue.Null) {
                        arguments.NamedArguments.Add(defaultArgumentValue.Key, defaultArgumentValue.Value);
                    }
                }
            }

            return new State(this, _taskFunc, thread, src, usr, arguments);
        }

        public static ProcState CreateAnonymousState(DreamThread thread, Func<State, Task<DreamValue>> taskFunc) {
            return new State(null, taskFunc, thread, null, null, new DreamProcArguments(null));
        }
    }
}
