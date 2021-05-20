using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using OpenDreamShared.Dream.Procs;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Procs;

namespace OpenDreamRuntime {
    public enum ProcStatus {
        Returned,
        Deferred,
        Called,
    }

    public abstract class DreamProc {
        public string Name { get; }

        public DreamRuntime Runtime { get; }

        // This is currently publicly settable because the loading code doesn't know what our super is until after we are instantiated
        public DreamProc SuperProc { set; get; }

        public List<String> ArgumentNames { get; }
        public List<DMValueType> ArgumentTypes { get; }

        protected DreamProc(string name, DreamRuntime runtime, DreamProc superProc, List<String> argumentNames, List<DMValueType> argumentTypes) {
            Name = name;
            Runtime = runtime;
            SuperProc = superProc;
            ArgumentNames = argumentNames ?? new();
            ArgumentTypes = argumentTypes ?? new();
        }

        public abstract ProcState CreateState(DreamThread thread, DreamObject src, DreamObject usr, DreamProcArguments arguments);

        // Execute this proc. This will behave as if the proc has `set waitfor = 0`
        public DreamValue Spawn(DreamObject src, DreamProcArguments arguments, DreamObject usr = null) {
            var context = new DreamThread(Runtime);
            var state = CreateState(context, src, usr, arguments);
            context.PushProcState(state);
            return context.Resume();
        }
    }

    class ProcRuntime : Exception {
        public ProcRuntime(string message)
            : base(message)
        {}
    }

    public abstract class ProcState {
        public DreamRuntime Runtime => Thread.Runtime;
        public DreamThread Thread { get; }
        public DreamValue Result { set; get; } = DreamValue.Null;
        
        public ProcState(DreamThread thread) {
            Thread = thread;
        }
        
        public ProcStatus Resume() {
            try {
                return InternalResume();
            } catch (Exception exception) {
                Thread.HandleException(exception);
                return ProcStatus.Returned;
            }
        }

        // May be null
        public abstract DreamProc Proc { get; }

        protected abstract ProcStatus InternalResume();

        public abstract void AppendStackFrame(StringBuilder builder);

        // Most implementations won't require this, so give it a default
        public virtual void ReturnedInto(DreamValue value) {}
    }

    public class DreamThread {
        public DreamThread(DreamRuntime runtime) {
            Runtime = runtime;
        }

        public DreamRuntime Runtime { get; }

        private const int MaxStackDepth = 256;

        private ProcState _current; 
        private Stack<ProcState> _stack = new();

        public static DreamValue Run(DreamProc proc, DreamObject src, DreamObject usr, DreamProcArguments? arguments) {
            var context = new DreamThread(proc.Runtime);
            var state = proc.CreateState(context, src, usr, arguments ?? new DreamProcArguments(null));
            context.PushProcState(state);
            return context.Resume();
        }

        public static DreamValue Run(DreamRuntime runtime, Func<AsyncNativeProc.State, Task<DreamValue>> anonymousFunc) {
            var context = new DreamThread(runtime);
            var state = AsyncNativeProc.CreateAnonymousState(context, anonymousFunc);
            context.PushProcState(state);
            return context.Resume();
        }

        public DreamValue Resume() {
            if (System.Threading.Thread.CurrentThread != Runtime.MainThread) {
                throw new InvalidOperationException();
            }

            while (_current != null) {
                // _current.Resume may mutate our state!!!
                switch (_current.Resume()) {
                    // Our top-most proc just returned a value
                    case ProcStatus.Returned:
                        var returned = _current.Result;

                        // If our stack is empty, the context has finished execution
                        // so we can return the result to our native caller
                        if (!_stack.TryPop(out _current)) {
                            return returned;
                        }

                        // ... otherwise we just push the return value onto the dm caller's stack
                        _current.ReturnedInto(returned);
                        break;

                    // The context is done executing for now
                    case ProcStatus.Deferred:
                        // We return the current return value here even though it may not be the final result
                        return _current.Result;

                    // Our top-most proc just called a function
                    // This means _current has changed!
                    case ProcStatus.Called:
                        // Nothing to do. The loop will call into _current.Resume for us.
                        break;
                }
            }

            throw new InvalidOperationException();
        }

        public void PushProcState(ProcState state) {
            if (_stack.Count >= MaxStackDepth) {
                throw new ProcRuntime("stack depth limit reached");
            }

            if (_current != null) {
                _stack.Push(_current);
            }
            _current = state;
        }

        public void AppendStackTrace(StringBuilder builder) {
            builder.Append("   ");
            _current.AppendStackFrame(builder);
            builder.AppendLine();

            foreach (var frame in _stack) {
                builder.Append("   ");
                frame.AppendStackFrame(builder);
                builder.AppendLine();
            }
        }

        public void HandleException(Exception exception) {
            StringBuilder builder = new();
            builder.AppendLine($"Exception Occured: {exception.Message}");

            builder.AppendLine("=DM StackTrace=");
            AppendStackTrace(builder);
            builder.AppendLine();

            builder.AppendLine("=C# StackTrace=");
            builder.AppendLine(exception.ToString());
            builder.AppendLine();

            Console.WriteLine(builder.ToString());
        }
    }
}