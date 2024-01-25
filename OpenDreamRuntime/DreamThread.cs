using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DMCompiler.DM;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Procs.DebugAdapter;
using OpenDreamShared.Dream;

namespace OpenDreamRuntime {
    public enum ProcStatus {
        Continue,
        Cancelled,
        Returned,
        Deferred,
        Called,
    }

    public abstract class DreamProc {
        public readonly int Id;
        public readonly TreeEntry OwningType;
        public readonly string Name;
        public readonly bool IsVerb;

        // This is currently publicly settable because the loading code doesn't know what our super is until after we are instantiated
        public DreamProc? SuperProc;

        public readonly ProcAttributes Attributes;

        public readonly List<string>? ArgumentNames;

        public readonly List<DreamValueType>? ArgumentTypes;

        public string VerbName => _verbName ?? Name;
        public readonly string? VerbCategory = string.Empty;
        public readonly sbyte Invisibility;

        private readonly string? _verbName;
        private readonly string? _verbDesc;

        protected DreamProc(int id, TreeEntry owningType, string name, DreamProc? superProc, ProcAttributes attributes, List<string>? argumentNames, List<DreamValueType>? argumentTypes, string? verbName, string? verbCategory, string? verbDesc, sbyte invisibility, bool isVerb = false) {
            Id = id;
            OwningType = owningType;
            Name = name;
            IsVerb = isVerb;
            SuperProc = superProc;
            Attributes = attributes;
            ArgumentNames = argumentNames;
            ArgumentTypes = argumentTypes;

            _verbName = verbName;
            if (verbCategory is not null) {
                // (de)serialization meme to reduce JSON size
                // It's string.Empty by default but we invert it to null to prevent serialization
                // Explicit null becomes treated as string.Empty
                VerbCategory = verbCategory == string.Empty ? null : verbCategory;
            }
            _verbDesc = verbDesc;
            Invisibility = invisibility;
        }

        public abstract ProcState CreateState(DreamThread thread, DreamObject? src, DreamObject? usr, DreamProcArguments arguments);

        // Execute this proc. This will behave as if the proc has `set waitfor = 0`
        public DreamValue Spawn(DreamObject src, DreamProcArguments arguments, DreamObject? usr = null) {
            var context = new DreamThread(this.ToString());
            var state = CreateState(context, src, usr, arguments);
            context.PushProcState(state);
            return context.Resume();
        }

        public DreamValue GetField(string field) {
            // TODO: Figure out what byond does when these are null
            switch (field) {
                case "name":
                    return new DreamValue(VerbName);
                case "category":
                    return (VerbCategory != null) ? new DreamValue(VerbCategory) : DreamValue.Null;
                case "desc":
                    return (_verbDesc != null) ? new DreamValue(_verbDesc) : DreamValue.Null;
                case "invisibility":
                    return new DreamValue(Invisibility);
                default:
                    throw new Exception($"Cannot get field \"{field}\" from {OwningType.ToString()}.{Name}()");
            }
        }

        public override string ToString() {
            var procElement = (SuperProc == null) ? (IsVerb ? "verb/" : "proc/") : String.Empty; // Has "proc/" only if it's not an override

            return $"{OwningType.Path}{(OwningType.Path.EndsWith('/') ? string.Empty : "/")}{procElement}{Name}";
        }
    }

    [Virtual]
    internal class DMThrowException : Exception {
        public readonly DreamValue Value;

        public DMThrowException(DreamValue value) : base(GetRuntimeMessage(value)) {
            Value = value;
        }

        private static string GetRuntimeMessage(DreamValue value) {
            string? name;

            value.TryGetValueAsDreamObject(out var dreamObject);
            if (dreamObject?.TryGetVariable("name", out var nameVar) == true) {
                name = nameVar.TryGetValueAsString(out name) ? name : string.Empty;
            } else {
                name = string.Empty;
            }

            return name;
        }
    }

    internal sealed class DMCrashRuntime : Exception {
        public DMCrashRuntime(string message) : base(message) { }
    }

    /// <summary>
    /// This exception instantly terminates the entire thread of the proc.
    /// </summary>
    internal sealed class DMError : Exception {
        public DMError(string message)
            : base(message) {
        }
    }

    public abstract class ProcState : IDisposable {
        private static int _idCounter = 0;
        public int Id { get; } = ++_idCounter;

        public DreamThread Thread { get; set; }

        [Access(typeof(ProcScheduler))]
        public DreamValue Result = DreamValue.Null;

        public bool WaitFor { get; set; } = true;

        public abstract DreamProc? Proc { get; }

        protected void Initialize(DreamThread thread, bool waitFor) {
            Thread = thread;
            WaitFor = waitFor;
        }

        public abstract ProcStatus Resume();

        /// <summary>
        /// Returns whether or not the proc is currently in a try catch block.
        /// </summary>
        public virtual bool IsCatching() => false;

        public virtual void CatchException(Exception exception) {
            throw new InvalidOperationException(
                $"Called {nameof(CatchException)} on a {nameof(ProcState)} that isn't catching!");
        }

        public abstract void AppendStackFrame(StringBuilder builder);

        // Most implementations won't require this, so give it a default
        public virtual void ReturnedInto(DreamValue value) {}

        public virtual void Cancel() {}

        public virtual void Dispose() {
            Thread = null!;
            Result = DreamValue.Null;
            WaitFor = true;
        }
    }

    public sealed class DreamThread {
        private static readonly System.Threading.ThreadLocal<Stack<DreamThread>> CurrentlyExecuting = new(() => new(), trackAllValues: true);
        public static IEnumerable<DreamThread> InspectExecutingThreads() {
            return CurrentlyExecuting.Value!.Concat(CurrentlyExecuting.Values.SelectMany(x => x));
        }

        private static int _idCounter = 0;
        public int Id { get; } = ++_idCounter;

        private const int MaxStackDepth = 400; // Same as BYOND but /world/loop_checks = 0 raises the limit

        private ProcState? _current;
        private readonly Stack<ProcState> _stack = new();

        // The amount of stack frames containing `WaitFor = false`
        private int _syncCount = 0;

        public string Name { get; }

        internal DreamDebugManager.ThreadStepMode? StepMode { get; set; }

        public DreamThread(string name) {
            Name = name;
        }

        public static DreamValue Run(DreamProc proc, DreamObject src, DreamObject? usr, params DreamValue[] arguments) {
            var context = new DreamThread(proc.ToString());
            var state = proc.CreateState(context, src, usr, new DreamProcArguments(arguments));
            context.PushProcState(state);
            return context.Resume();
        }

        public static DreamValue Run(string name, Func<AsyncNativeProc.State, Task<DreamValue>> anonymousFunc) {
            var context = new DreamThread(name);
            var state = AsyncNativeProc.CreateAnonymousState(context, anonymousFunc);
            context.PushProcState(state);
            return context.Resume();
        }

        public DreamValue Resume() {
            try {
                CurrentlyExecuting.Value!.Push(this);
                while (_current != null) {
                    ProcStatus status;
                    try {
                        // _current.Resume may mutate our state!!!
                        status = _current.Resume();
                    } catch (DMError dmError) {
                        CancelAll();
                        HandleException(dmError);
                        status = ProcStatus.Cancelled;
                    } catch (Exception exception) {
                        if (TryCatchException(exception)) continue;
                        HandleException(exception);
                        status = ProcStatus.Returned;
                    }

                    switch (status) {
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
            } finally {
                if (CurrentlyExecuting.Value!.Pop() != this) {
                    throw new InvalidOperationException("DreamThread stack corrupted");
                }
            }

            throw new InvalidOperationException();
        }

        public void PushProcState(ProcState state) {
            if (_stack.Count >= MaxStackDepth) {
                throw new DMError("stack depth limit reached");
            }

            if (state.WaitFor == false) {
                _syncCount++;
            }

            if (_current != null) {
                _stack.Push(_current);
            }
            _current = state;
        }

        public void PopProcState(bool dispose = true) {
            if (_current?.WaitFor == false) {
                _syncCount--;
            }

            // Maybe a bit of a hack? If the state got deferred to another thread it shouldn't be disposed.
            if (dispose && _current.Thread == this) {
                _current.Dispose();
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
                newStackReversed.Push(_current);
                PopProcState(dispose: false); // Dont dispose; this proc state is just being moved to another thread.
            }

            // `WaitFor = false` frame
            if(_current == null) throw new InvalidOperationException();
            newStackReversed.Push(_current);
            PopProcState(dispose: false);

            DreamThread newThread = new DreamThread("deferred");
            foreach (var frame in newStackReversed) {
                frame.Thread = newThread;
                newThread.PushProcState(frame);
            }

            // Our returning proc state is expected to be on the stack at this point, so put it back
            // For this small moment, the proc state will be on both threads.
            PushProcState(newStackReversed.Peek());

            return ProcStatus.Returned;
        }

        public void AppendStackTrace(StringBuilder builder) {
            builder.Append("   ");
            if(_current is null)
            {
                builder.Append("(init)...");
            }
            else {
                _current.AppendStackFrame(builder);
            }
            builder.AppendLine();

            foreach (var frame in _stack) {
                builder.Append("   ");
                frame.AppendStackFrame(builder);
                builder.AppendLine();
            }
        }

        public void CancelAll() {
            _current?.Cancel();

            foreach (var state in _stack) {
                state.Cancel();
            }
        }

        public void HandleException(Exception exception) {
            _current?.Cancel();

            var dreamMan = IoCManager.Resolve<DreamManager>();


            StringBuilder builder = new();
            builder.AppendLine($"Exception occurred: {exception.Message}");

            builder.AppendLine("=DM StackTrace=");
            AppendStackTrace(builder);
            builder.AppendLine();

            builder.AppendLine("=C# StackTrace=");
            builder.AppendLine(exception.ToString());
            builder.AppendLine();

            var msg = builder.ToString();

            // TODO: Defining world.Error() causes byond to no longer print exceptions to the log unless ..() is called
            dreamMan.WriteWorldLog(msg, LogLevel.Error);

            // Instantiate an /exception and invoke world.Error()
            string file = string.Empty;
            int line = 0;
            if(_current is DMProcState dmProc) { // TODO: Cope with the other ProcStates
                var source = dmProc.GetCurrentSource();
                file = source.Item1;
                line = source.Item2;
            }
            dreamMan.HandleException(exception, msg, file, line);

            IoCManager.Resolve<IDreamDebugManager>().HandleException(this, exception);
        }

        public IEnumerable<ProcState> InspectStack() {
            if (_current is not null) {
                yield return _current;
            }
            foreach (var entry in _stack) {
                yield return entry;
            }
        }

        private bool TryCatchException(Exception exception) {
            if (!InspectStack().Any(x => x.IsCatching())) return false;

            while (!_current.IsCatching()) {
                PopProcState();
            }

            _current.CatchException(exception);
            return true;
        }
    }
}
