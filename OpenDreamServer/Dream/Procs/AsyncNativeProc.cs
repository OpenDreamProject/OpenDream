using System;
using System.Buffers;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using OpenDreamServer.Dream.Objects;
using OpenDreamShared.Dream.Procs;

namespace OpenDreamServer.Dream.Procs {
    class AsyncNativeProc : DreamProc {
        private static AsyncResultProc Instance = new();

        public class State : ProcState
        {
            public State(ExecutionContext context, Func<State, Task<DreamValue>> taskFunc)
                : base(context)
            {
                _taskFunc = taskFunc;
            }      

            public override DreamProc Proc => AsyncNativeProc.Instance;

            private Func<State, Task<DreamValue>> _taskFunc;

            public override void AppendStackFrame(StringBuilder builder)
            {
                builder.AppendLine("<async proc>");
            }

            private ProcState _callProcNotify;
            private TaskCompletionSource<DreamValue> _callTcs;
            private DreamValue? _callResult;

            private Task<DreamValue> _task;

            private bool _inResume;

            // Used to avoid reentrant resumptions in our proc
            protected void SafeResume() {
                if (_inResume) {
                    return;
                }

                Context.Resume();
            }

            protected override ProcStatus InternalResume()
            {
                _inResume = true;

                // We've just been created, start our task
                if (_task == null) {                    
                    _task = Program.TaskFactory.StartNew(() => _taskFunc(this)).Unwrap();

                    // When the task finishes, fetch its result
                    _task.ContinueWith(task => {
                        Result = task.Result;

                        // We have to resume now so that the execution context knows we have returned
                        // This will immediately hit a `return ProcStatus.Returned`.
                        SafeResume();
                    }, Program.DreamTaskScheduler);
                }

                // We need to call a proc
                if (_callProcNotify != null) {
                    var callProcNotify = _callProcNotify;
                    _callProcNotify = null;

                    Context.PushProcState(callProcNotify);
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
                    return ProcStatus.Returned;
                }

                // Otherwise, we are still pending
                _inResume = false;
                return ProcStatus.Deferred;
            }

            public Task<DreamValue> Call(DreamProc proc, DreamObject src, DreamObject usr, DreamProcArguments arguments) {
                _callTcs = new();
                _callProcNotify = proc.CreateState(Context, src, usr, arguments);
                SafeResume();
                return _callTcs.Task;
            }

            public override void ReturnedInto(DreamValue value) {
                // We don't call `_callTcs.SetResult` here because we're about to be resumed and can do it there.
                _callResult = value;
            }
        }

        public AsyncNativeProc()
            : base("<async wrapper>", null, null, null)
        {}

        public override ProcState CreateState(ExecutionContext context, DreamObject src, DreamObject usr, DreamProcArguments arguments)
        {
            // This proc's state gets instantiated through a static overload. It shouldn't reach this path.
            throw new NotImplementedException();
        }

        public static void Run(Func<State, Task<DreamValue>> taskFunc) {
            var context = new ExecutionContext();
            var state = new State(context, taskFunc);
            context.PushProcState(state);
            context.Resume();
        }
    }
}