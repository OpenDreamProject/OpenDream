using System.Text;
using System.Threading.Tasks;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream.Procs;

namespace OpenDreamRuntime {
    public enum ProcStatus {
        Cancelled,
        Returned,
        Deferred,
        Called,
    }

    public abstract class DreamProc {
        public string Name { get; }

        // This is currently publicly settable because the loading code doesn't know what our super is until after we are instantiated
        public DreamProc SuperProc { set; get; }

        public ProcAttributes Attributes { get; }

        public List<String>? ArgumentNames { get; }
        public List<DMValueType>? ArgumentTypes { get; }

        public string? VerbName { get; }
        public string? VerbCategory { get; } = string.Empty;
        public string? VerbDesc { get; }
        public sbyte? Invisibility { get; }

        protected DreamProc(string name, DreamProc superProc, ProcAttributes attributes, List<String>? argumentNames, List<DMValueType>? argumentTypes, string? verbName, string? verbCategory, string? verbDesc, sbyte? invisibility) {
            Name = name;
            SuperProc = superProc;
            Attributes = attributes;
            ArgumentNames = argumentNames;
            ArgumentTypes = argumentTypes;

            VerbName = verbName;
            if (verbCategory is not null)
            {
                // (de)serialization meme to reduce JSON size
                // It's string.Empty by default but we invert it to null to prevent serialization
                // Explicit null becomes treated as string.Empty
                VerbCategory = verbCategory == string.Empty ? null : verbCategory;
            }
            VerbDesc = verbDesc;
            Invisibility = invisibility;
        }

        public abstract ProcState CreateState(DreamThread thread, DreamObject src, DreamObject? usr, DreamProcArguments arguments);

        // Execute this proc. This will behave as if the proc has `set waitfor = 0`
        public DreamValue Spawn(DreamObject src, DreamProcArguments arguments, DreamObject? usr = null) {
            var context = new DreamThread();
            var state = CreateState(context, src, usr, arguments);
            context.PushProcState(state);
            return context.Resume();
        }
    }

    sealed class CancellingRuntime : Exception {
        public CancellingRuntime(string message)
            : base(message)
        {}
    }

    sealed class PropagatingRuntime : Exception {
        public PropagatingRuntime(string message)
            : base(message)
        {}
    }

    public abstract class ProcState {
        public DreamThread Thread { get; set; }
        public DreamValue Result { set; get; } = DreamValue.Null;

        public bool WaitFor => Proc != null ? (Proc.Attributes & ProcAttributes.DisableWaitfor) != ProcAttributes.DisableWaitfor : true;

        public ProcState(DreamThread thread) {
            Thread = thread;
        }

        public ProcStatus Resume() {
            try {
                return InternalResume();
            } catch (CancellingRuntime exception) {
                Thread.HandleException(exception);
                return ProcStatus.Cancelled;
            } catch (PropagatingRuntime exception) {
                Thread.HandleException(exception);
                Thread.PopProcState();
                return ProcStatus.Returned;
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

    public sealed class DreamThread {
        private const int MaxStackDepth = 256;

        private ProcState? _current;
        private Stack<ProcState> _stack = new();

        // The amount of stack frames containing `WaitFor = false`
        private int _syncCount = 0;

        public static DreamValue Run(DreamProc proc, DreamObject src, DreamObject usr, DreamProcArguments? arguments) {
            var context = new DreamThread();
            var state = proc.CreateState(context, src, usr, arguments ?? new DreamProcArguments(null));
            context.PushProcState(state);
            return context.Resume();
        }

        public static DreamValue Run(Func<AsyncNativeProc.State, Task<DreamValue>> anonymousFunc) {
            var context = new DreamThread();
            var state = AsyncNativeProc.CreateAnonymousState(context, anonymousFunc);
            context.PushProcState(state);
            return context.Resume();
        }

        public DreamValue Resume() {
            while (_current != null) {
                // _current.Resume may mutate our state!!!
                switch (_current.Resume()) {
                    // The entire Thread is stopping
                    case ProcStatus.Cancelled:
                        var current = _current;
                        _current = null;
                        _stack.Clear();
                        return current.Result;

                    // Our top-most proc just returned a value
                    case ProcStatus.Returned:
                        var returned = _current.Result;
                        PopProcState();

                        // If our stack is empty, the context has finished execution
                        // so we can return the result to our native caller
                        if (_current == null) {
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
                throw new CancellingRuntime("stack depth limit reached");
            }

            if (state.WaitFor == false) {
                _syncCount++;
            }

            if (_current != null) {
                _stack.Push(_current);
            }
            _current = state;
        }

        public void PopProcState() {
            if (_current?.WaitFor == false) {
                _syncCount--;
            }

            if (!_stack.TryPop(out _current)) {
                _current = null;
            }
        }

        // Used by implementations of DreamProc::InternalContinue to defer execution to be resumed later.
        // This function may mutate `ProcState.Thread` on any of the states within this DreamThread's call stack
        public ProcStatus HandleDefer() {
            // When there are no `WaitFor = false` procs in our stack, just use the current thread
            if (_syncCount <= 0) {
                return ProcStatus.Deferred;
            }

            // Move over all stacks up to and including the first with `WaitFor = false` to a new DreamThread
            Stack<ProcState> newStackReversed = new();

            // `WaitFor = true` frames
            while (_current is not null && _current.WaitFor) {
                var frame = _current;
                PopProcState();
                newStackReversed.Push(frame);
            }

            // `WaitFor = false` frame
            if(_current == null) throw new InvalidOperationException();
            newStackReversed.Push(_current);
            PopProcState();

            DreamThread newThread = new DreamThread();
            foreach (var frame in newStackReversed) {
                frame.Thread = newThread;
                newThread.PushProcState(frame);
            }

            // Our returning proc state is expected to be on the stack at this point, so put it back
            // For this small moment, the proc state will be on both threads.
            PushProcState(newStackReversed.Peek());

            // The old thread was emptied?
            if (_current == null) {
                throw new InvalidOperationException();
            }

            return ProcStatus.Returned;
        }

        public void AppendStackTrace(StringBuilder builder) {
            builder.Append("   ");
            if(_current is null)
            {
                builder.Append("(init)...");
            }
            else
            {
                _current.AppendStackFrame(builder);
            }
            builder.AppendLine();

            foreach (var frame in _stack) {
                builder.Append("   ");
                frame.AppendStackFrame(builder);
                builder.AppendLine();
            }
        }

        public void HandleException(Exception exception)
        {
            if(_current is DMProcState state)
            {
                state.ReturnPools();
            }
            var dreamMan = IoCManager.Resolve<IDreamManager>();
            dreamMan.DMExceptionCount += 1;

            StringBuilder builder = new();
            builder.AppendLine($"Exception Occured: {exception.Message}");

            builder.AppendLine("=DM StackTrace=");
            AppendStackTrace(builder);
            builder.AppendLine();

            builder.AppendLine("=C# StackTrace=");
            builder.AppendLine(exception.ToString());
            builder.AppendLine();

           dreamMan.WriteWorldLog(builder.ToString(), LogLevel.Error);
        }
    }
}
