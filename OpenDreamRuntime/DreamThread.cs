using System.Linq;
using System.Text;
using System.Threading;
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

        public int? VerbId = null; // Null until registered as a verb in ServerVerbSystem
        public string VerbName => _verbName ?? Name;
        public readonly string? VerbDesc;
        public readonly string? VerbCategory = string.Empty;
        public readonly VerbSrc? VerbSrc;
        public readonly sbyte Invisibility;

        private readonly string? _verbName;

        protected DreamProc(int id, TreeEntry owningType, string name, DreamProc? superProc, ProcAttributes attributes, List<string>? argumentNames, List<DreamValueType>? argumentTypes, VerbSrc? verbSrc, string? verbName, string? verbCategory, string? verbDesc, sbyte invisibility, bool isVerb = false) {
            Id = id;
            OwningType = owningType;
            Name = name;
            IsVerb = isVerb;
            SuperProc = superProc;
            Attributes = attributes;
            ArgumentNames = argumentNames;
            ArgumentTypes = argumentTypes;

            VerbSrc = verbSrc;
            _verbName = verbName;
            if (verbCategory is not null) {
                // (de)serialization meme to reduce JSON size
                // It's string.Empty by default but we invert it to null to prevent serialization
                // Explicit null becomes treated as string.Empty
                VerbCategory = verbCategory == string.Empty ? null : verbCategory;
            }

            VerbDesc = verbDesc;
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
                    return (VerbDesc != null) ? new DreamValue(VerbDesc) : DreamValue.Null;
                case "invisibility":
                    return new DreamValue(Invisibility);
                case "hidden":
                    Logger.GetSawmill("opendream.dmproc").Warning("The 'hidden' field on verbs will always return null.");
                    return DreamValue.Null;
                default:
                    throw new Exception($"Cannot get field \"{field}\" from {OwningType}.{Name}()");
            }
        }

        public override string ToString() {
            var procElement = (SuperProc == null) ? (IsVerb ? "verb/" : "proc/") : string.Empty; // Has "proc/" only if it's not an override

            return $"{OwningType.Path}{(OwningType.Path.EndsWith('/') ? string.Empty : "/")}{procElement}{Name}";
        }
    }

    [Virtual]
    internal class DMThrowException(DreamValue value) : Exception(GetRuntimeMessage(value)) {
        public readonly DreamValue Value = value;

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

    internal sealed class DMCrashRuntime(string message) : Exception(message);

    /// <summary>
    /// This exception instantly terminates the entire thread of the proc.
    /// </summary>
    internal sealed class DMError(string message) : Exception(message);

    public abstract class ProcState : IDisposable {
        private static int _idCounter;

        public int Id { get; private set; }
        public DreamThread Thread { get; set; } = default!;
        #if TOOLS
        public abstract (string SourceFile, int Line) TracyLocationId { get; }
        public ProfilerZone? TracyZoneId { get; set; }
        #endif

        [Access(typeof(ProcScheduler))]
        public DreamValue Result = DreamValue.Null;

        public bool WaitFor { get; set; } = true;

        public abstract DreamProc? Proc { get; }

        protected void Initialize(DreamThread thread, bool waitFor) {
            Thread = thread;
            WaitFor = waitFor;
            Id = _idCounter++;
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
            Id = -1;
        }
    }

    public sealed class DreamThread(string name) {
        private static readonly ThreadLocal<Stack<DreamThread>> CurrentlyExecuting = new(() => new(), trackAllValues: true);
        private static readonly StringBuilder ErrorMessageBuilder = new();

        private static int _idCounter;
        public int Id { get; } = ++_idCounter;

        private const int MaxStackDepth = 400; // Same as BYOND but /world/loop_checks = 0 raises the limit

        private ProcState? _current;
        private readonly Stack<ProcState> _stack = new();

        // The amount of stack frames containing `WaitFor = false`
        private int _syncCount;

        /// <summary>
        /// Stores the last object that was animated, so that animate() can be called without the object parameter. Does not need to be passed to spawn calls, only current execution context.
        /// </summary>
        public DreamValue? LastAnimatedObject = null;

        public string Name { get; } = name;

        internal DreamDebugManager.ThreadStepMode? StepMode { get; set; }

        public static DreamValue Run(DreamProc proc, DreamObject src, DreamObject? usr, params DreamValue[] arguments) {
            var context = new DreamThread(proc.ToString());

            if (proc is NativeProc nativeProc) {
                // ReSharper disable ExplicitCallerInfoArgument
                using(Profiler.BeginZone(filePath:"Native Proc", lineNumber:0, memberName:nativeProc.Name))
                    return nativeProc.Call(context, src, usr, new(arguments));
                // ReSharper restore ExplicitCallerInfoArgument
            }

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
            return ReentrantResume(null, out _);
        }

        /// <summary>
        /// Resume this thread re-entrantly.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This function is suitable for executing from inside a running opcode handler if
        /// <paramref name="untilState"/> is provided.
        /// </para>
        /// </remarks>
        /// <param name="untilState">
        /// If not null, only continue running until this proc state gets returned into.
        /// Note that if used, the parent proc will not have its <see cref="ProcState.ReturnedInto"/> called.
        /// </param>
        /// <param name="resultStatus">
        /// The proc result status that caused this resume to return.
        /// </param>
        /// <returns>The return value of the last proc to return.</returns>
        public DreamValue ReentrantResume(ProcState? untilState, out ProcStatus resultStatus) {
            try {
                CurrentlyExecuting.Value!.Push(this);
                while (_current != null) {
                    ProcStatus status;
                    try {
                        #if TOOLS
                        if (_current.TracyZoneId is null && _current.Proc != null) {
                            var location =_current.TracyLocationId;
                            var procpath = (_current.Proc.OwningType.Path.Equals("/") ? "/proc/" : _current.Proc.OwningType.Path+"/") +_current.Proc.Name;
                            // ReSharper disable ExplicitCallerInfoArgument
                            _current.TracyZoneId = Profiler.BeginZone(filePath: location.SourceFile, lineNumber: location.Line, memberName: procpath);
                            // ReSharper restore ExplicitCallerInfoArgument
                        }
                        #endif
                        // _current.Resume may mutate our state!!!
                        status = _current.Resume();
                    } catch (DMError dmError) {
                        if (_current == null) {
                            // This happens if a ReentrantResume cancelled, it will have already torn down the stack.
                            // Just bail and do nothing else.
                            resultStatus = ProcStatus.Cancelled;
                            return default;
                        }

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
                            #if TOOLS
                            if (_current.TracyZoneId is not null) {
                                _current.TracyZoneId.Value.Dispose();
                                _current.TracyZoneId = null;
                            }
                            #endif

                            var current = _current;
                            _current = null;

                            #if TOOLS
                            foreach (var s in _stack) {
                                if (s.TracyZoneId is null)
                                    continue;
                                s.TracyZoneId.Value.Dispose();
                                s.TracyZoneId = null;
                            }
                            #endif

                            _stack.Clear();
                            resultStatus = status;
                            return current.Result;

                        // Our top-most proc just returned a value
                        case ProcStatus.Returned:
                            var returned = _current.Result;
                            PopProcState();

                            // If our stack is empty, the context has finished execution
                            // so we can return the result to our native caller
                            if (_current == null || _current == untilState) {
                                resultStatus = status;
                                return returned;
                            }

                            // ... otherwise we just push the return value onto the dm caller's stack
                            _current.ReturnedInto(returned);
                            break;

                        // The context is done executing for now
                        case ProcStatus.Deferred:
                            #if TOOLS
                            if (_current.TracyZoneId is not null) {
                                _current.TracyZoneId.Value.Dispose();
                                _current.TracyZoneId = null;
                            }

                            foreach (var s in _stack) {
                                if (s.TracyZoneId is null)
                                    continue;
                                s.TracyZoneId.Value.Dispose();
                                s.TracyZoneId = null;
                            }
                            #endif
                            // We return the current return value here even though it may not be the final result
                            resultStatus = status;
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
            #if TOOLS
            if (_current?.TracyZoneId is not null) {
                _current.TracyZoneId.Value.Dispose();
                _current.TracyZoneId = null;
            }
            #endif

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

        private void HandleException(Exception exception) {
            _current?.Cancel();

            var dreamMan = IoCManager.Resolve<DreamManager>();

            ErrorMessageBuilder.Clear();
            ErrorMessageBuilder.AppendLine($"Exception occurred: {exception.Message}");

            ErrorMessageBuilder.AppendLine("=DM StackTrace=");
            AppendStackTrace(ErrorMessageBuilder);
            ErrorMessageBuilder.AppendLine();

            ErrorMessageBuilder.AppendLine("=C# StackTrace=");
            ErrorMessageBuilder.AppendLine(exception.ToString());
            ErrorMessageBuilder.AppendLine();

            var msg = ErrorMessageBuilder.ToString();

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

        public static IEnumerable<DreamThread> InspectExecutingThreads() {
            return CurrentlyExecuting.Value!.Concat(CurrentlyExecuting.Values.SelectMany(x => x));
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
