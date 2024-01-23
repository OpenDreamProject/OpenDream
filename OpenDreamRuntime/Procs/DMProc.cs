using System.Buffers;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using DMCompiler.Bytecode;
using DMCompiler.DM;
using DMCompiler.Json;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using OpenDreamRuntime.Procs.DebugAdapter;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Procs {
    public sealed class DMProc : DreamProc {
        public readonly byte[] Bytecode;

        public readonly bool IsNullProc;
        public IReadOnlyList<LocalVariableJson> LocalNames { get; }
        public readonly List<SourceInfoJson> SourceInfo;

        public readonly AtomManager AtomManager;
        public readonly DreamManager DreamManager;
        public readonly ProcScheduler ProcScheduler;
        public readonly IDreamMapManager DreamMapManager;
        public readonly IDreamDebugManager DreamDebugManager;
        public readonly DreamResourceManager DreamResourceManager;
        public readonly DreamObjectTree ObjectTree;

        private readonly int _maxStackSize;

        public DMProc(int id, TreeEntry owningType, ProcDefinitionJson json, string? name, DreamManager dreamManager, AtomManager atomManager, IDreamMapManager dreamMapManager, IDreamDebugManager dreamDebugManager, DreamResourceManager dreamResourceManager, DreamObjectTree objectTree, ProcScheduler procScheduler)
            : base(id, owningType, name ?? json.Name, null, json.Attributes, GetArgumentNames(json), GetArgumentTypes(json), json.VerbName, json.VerbCategory, json.VerbDesc, json.Invisibility, json.IsVerb) {
            Bytecode = json.Bytecode ?? Array.Empty<byte>();
            LocalNames = json.Locals;
            SourceInfo = json.SourceInfo;
            _maxStackSize = json.MaxStackSize;
            IsNullProc = CheckIfNullProc();

            AtomManager = atomManager;
            DreamManager = dreamManager;
            DreamMapManager = dreamMapManager;
            DreamDebugManager = dreamDebugManager;
            DreamResourceManager = dreamResourceManager;
            ObjectTree = objectTree;
            ProcScheduler = procScheduler;
        }

        public (string Source, int Line) GetSourceAtOffset(int offset) {
            SourceInfoJson current = SourceInfo[0];
            string source = ObjectTree.Strings[current.File!.Value];

            int i = 0;
            do {
                var next = SourceInfo[i++];
                if (next.Offset > offset)
                    break;

                current = next;
                if (current.File != null)
                    source = ObjectTree.Strings[current.File.Value];
            } while (i < SourceInfo.Count);

            return (source, current.Line);
        }

        /// <summary>
        /// Checks if the given bytecode offset is the first on a line of the source code
        /// </summary>
        public bool IsOnLineChange(int offset) {
            foreach (var sourceInfo in SourceInfo) {
                if (sourceInfo.Offset == offset)
                    return true;
            }

            return false;
        }

        public bool TryGetOffsetAtSource(string source, int line, out int offset) {
            string? currentSource = null;

            int i = 0;
            do {
                var current = SourceInfo[i++];

                if (current.File != null)
                    currentSource = ObjectTree.Strings[current.File.Value];

                if (currentSource == source && current.Line == line) {
                    offset = current.Offset;
                    return true;
                }
            } while (i < SourceInfo.Count);

            offset = 0;
            return false;
        }

        public override ProcState CreateState(DreamThread thread, DreamObject? src, DreamObject? usr, DreamProcArguments arguments) {
            if (IsNullProc) {
                if (!NullProcState.Pool.TryPop(out var nullState)) {
                    nullState = new NullProcState();
                }

                nullState.Initialize(this);
                return nullState;
            }

            if (!DMProcState.Pool.TryPop(out var state)) {
                state = new DMProcState();
            }

            state.Initialize(this, thread, _maxStackSize, src, usr, arguments);
            return state;
        }

        private bool CheckIfNullProc() {
            // We check for two possible patterns, entirely empty procs or pushing and returning self.
            if (Bytecode.Length == 0 || Bytecode is [(byte)DreamProcOpcode.PushReferenceValue, 0x01, (byte)DreamProcOpcode.Return])
                return true;

            return false;
        }

        private static List<string>? GetArgumentNames(ProcDefinitionJson json) {
            if (json.Arguments == null) {
                return new();
            } else {
                var argumentNames = new List<string>(json.Arguments.Count);
                argumentNames.AddRange(json.Arguments.Select(a => a.Name).ToArray());
                return argumentNames;
            }
        }

        private static List<DreamValueType> GetArgumentTypes(ProcDefinitionJson json) {
            if (json.Arguments == null) {
                return new();
            } else {
                var argumentTypes = new List<DreamValueType>(json.Arguments.Count);
                argumentTypes.AddRange(json.Arguments.Select(a => (DreamValueType)a.Type));
                return argumentTypes;
            }
        }
    }

    public sealed class NullProcState : ProcState {
        public static readonly Stack<NullProcState> Pool = new();

        public override DreamProc? Proc => _proc;

        private DreamProc? _proc;

        public override ProcStatus Resume() {
            return ProcStatus.Returned; // do nothing heehoo
        }

        public override void AppendStackFrame(StringBuilder builder) {
            throw new NotImplementedException();
        }

        public void Initialize(DMProc proc) {
            _proc = proc;
        }

        public override void Dispose() {
            base.Dispose();
            _proc = null;
            Pool.Push(this);
        }
    }

    public sealed class DMProcState : ProcState {
        private delegate ProcStatus OpcodeHandler(DMProcState state);

        public static readonly Stack<DMProcState> Pool = new();

        private static readonly ArrayPool<DreamValue> _dreamValuePool = ArrayPool<DreamValue>.Create();

        #region Opcode Handlers
        //Human readable friendly version, which will be converted to a more efficient lookup at runtime.
        private static readonly Dictionary<DreamProcOpcode, OpcodeHandler> _opcodeHandlers = new() {
            {DreamProcOpcode.BitShiftLeft, DMOpcodeHandlers.BitShiftLeft},
            {DreamProcOpcode.PushType, DMOpcodeHandlers.PushType},
            {DreamProcOpcode.PushString, DMOpcodeHandlers.PushString},
            {DreamProcOpcode.FormatString, DMOpcodeHandlers.FormatString},
            {DreamProcOpcode.SwitchCaseRange, DMOpcodeHandlers.SwitchCaseRange},
            {DreamProcOpcode.PushReferenceValue, DMOpcodeHandlers.PushReferenceValue},
            {DreamProcOpcode.Add, DMOpcodeHandlers.Add},
            {DreamProcOpcode.Assign, DMOpcodeHandlers.Assign},
            {DreamProcOpcode.Call, DMOpcodeHandlers.Call},
            {DreamProcOpcode.MultiplyReference, DMOpcodeHandlers.MultiplyReference},
            {DreamProcOpcode.JumpIfFalse, DMOpcodeHandlers.JumpIfFalse},
            {DreamProcOpcode.JumpIfTrue, DMOpcodeHandlers.JumpIfTrue},
            {DreamProcOpcode.Jump, DMOpcodeHandlers.Jump},
            {DreamProcOpcode.CompareEquals, DMOpcodeHandlers.CompareEquals},
            {DreamProcOpcode.Return, DMOpcodeHandlers.Return},
            {DreamProcOpcode.PushNull, DMOpcodeHandlers.PushNull},
            {DreamProcOpcode.Subtract, DMOpcodeHandlers.Subtract},
            {DreamProcOpcode.CompareLessThan, DMOpcodeHandlers.CompareLessThan},
            {DreamProcOpcode.CompareGreaterThan, DMOpcodeHandlers.CompareGreaterThan},
            {DreamProcOpcode.BooleanAnd, DMOpcodeHandlers.BooleanAnd},
            {DreamProcOpcode.BooleanNot, DMOpcodeHandlers.BooleanNot},
            {DreamProcOpcode.DivideReference, DMOpcodeHandlers.DivideReference},
            {DreamProcOpcode.Negate, DMOpcodeHandlers.Negate},
            {DreamProcOpcode.Modulus, DMOpcodeHandlers.Modulus},
            {DreamProcOpcode.Append, DMOpcodeHandlers.Append},
            {DreamProcOpcode.CreateRangeEnumerator, DMOpcodeHandlers.CreateRangeEnumerator},
            {DreamProcOpcode.Input, DMOpcodeHandlers.Input},
            {DreamProcOpcode.CompareLessThanOrEqual, DMOpcodeHandlers.CompareLessThanOrEqual},
            {DreamProcOpcode.CreateAssociativeList, DMOpcodeHandlers.CreateAssociativeList},
            {DreamProcOpcode.Remove, DMOpcodeHandlers.Remove},
            {DreamProcOpcode.DeleteObject, DMOpcodeHandlers.DeleteObject},
            {DreamProcOpcode.PushResource, DMOpcodeHandlers.PushResource},
            {DreamProcOpcode.CreateList, DMOpcodeHandlers.CreateList},
            {DreamProcOpcode.CallStatement, DMOpcodeHandlers.CallStatement},
            {DreamProcOpcode.BitAnd, DMOpcodeHandlers.BitAnd},
            {DreamProcOpcode.CompareNotEquals, DMOpcodeHandlers.CompareNotEquals},
            {DreamProcOpcode.PushProc, DMOpcodeHandlers.PushProc},
            {DreamProcOpcode.Divide, DMOpcodeHandlers.Divide},
            {DreamProcOpcode.Multiply, DMOpcodeHandlers.Multiply},
            {DreamProcOpcode.BitXorReference, DMOpcodeHandlers.BitXorReference},
            {DreamProcOpcode.BitXor, DMOpcodeHandlers.BitXor},
            {DreamProcOpcode.BitOr, DMOpcodeHandlers.BitOr},
            {DreamProcOpcode.BitNot, DMOpcodeHandlers.BitNot},
            {DreamProcOpcode.Combine, DMOpcodeHandlers.Combine},
            {DreamProcOpcode.CreateObject, DMOpcodeHandlers.CreateObject},
            {DreamProcOpcode.BooleanOr, DMOpcodeHandlers.BooleanOr},
            {DreamProcOpcode.CompareGreaterThanOrEqual, DMOpcodeHandlers.CompareGreaterThanOrEqual},
            {DreamProcOpcode.SwitchCase, DMOpcodeHandlers.SwitchCase},
            {DreamProcOpcode.Mask, DMOpcodeHandlers.Mask},
            {DreamProcOpcode.Error, DMOpcodeHandlers.Error},
            {DreamProcOpcode.IsInList, DMOpcodeHandlers.IsInList},
            {DreamProcOpcode.PushFloat, DMOpcodeHandlers.PushFloat},
            {DreamProcOpcode.ModulusReference, DMOpcodeHandlers.ModulusReference},
            {DreamProcOpcode.CreateListEnumerator, DMOpcodeHandlers.CreateListEnumerator},
            {DreamProcOpcode.Enumerate, DMOpcodeHandlers.Enumerate},
            {DreamProcOpcode.DestroyEnumerator, DMOpcodeHandlers.DestroyEnumerator},
            {DreamProcOpcode.Browse, DMOpcodeHandlers.Browse},
            {DreamProcOpcode.BrowseResource, DMOpcodeHandlers.BrowseResource},
            {DreamProcOpcode.OutputControl, DMOpcodeHandlers.OutputControl},
            {DreamProcOpcode.BitShiftRight, DMOpcodeHandlers.BitShiftRight},
            {DreamProcOpcode.CreateFilteredListEnumerator, DMOpcodeHandlers.CreateFilteredListEnumerator},
            {DreamProcOpcode.Power, DMOpcodeHandlers.Power},
            {DreamProcOpcode.Prompt, DMOpcodeHandlers.Prompt},
            {DreamProcOpcode.Ftp, DMOpcodeHandlers.Ftp},
            {DreamProcOpcode.Initial, DMOpcodeHandlers.Initial},
            {DreamProcOpcode.IsType, DMOpcodeHandlers.IsType},
            {DreamProcOpcode.LocateCoord, DMOpcodeHandlers.LocateCoord},
            {DreamProcOpcode.Locate, DMOpcodeHandlers.Locate},
            {DreamProcOpcode.IsNull, DMOpcodeHandlers.IsNull},
            {DreamProcOpcode.Spawn, DMOpcodeHandlers.Spawn},
            {DreamProcOpcode.OutputReference, DMOpcodeHandlers.OutputReference},
            {DreamProcOpcode.Output, DMOpcodeHandlers.Output},
            {DreamProcOpcode.JumpIfNullDereference, DMOpcodeHandlers.JumpIfNullDereference},
            {DreamProcOpcode.Pop, DMOpcodeHandlers.Pop},
            {DreamProcOpcode.Prob, DMOpcodeHandlers.Prob},
            {DreamProcOpcode.IsSaved, DMOpcodeHandlers.IsSaved},
            {DreamProcOpcode.PickUnweighted, DMOpcodeHandlers.PickUnweighted},
            {DreamProcOpcode.PickWeighted, DMOpcodeHandlers.PickWeighted},
            {DreamProcOpcode.Increment, DMOpcodeHandlers.Increment},
            {DreamProcOpcode.Decrement, DMOpcodeHandlers.Decrement},
            {DreamProcOpcode.CompareEquivalent, DMOpcodeHandlers.CompareEquivalent},
            {DreamProcOpcode.CompareNotEquivalent, DMOpcodeHandlers.CompareNotEquivalent},
            {DreamProcOpcode.Throw, DMOpcodeHandlers.Throw},
            {DreamProcOpcode.IsInRange, DMOpcodeHandlers.IsInRange},
            {DreamProcOpcode.MassConcatenation, DMOpcodeHandlers.MassConcatenation},
            {DreamProcOpcode.CreateTypeEnumerator, DMOpcodeHandlers.CreateTypeEnumerator},
            {DreamProcOpcode.PushGlobalVars, DMOpcodeHandlers.PushGlobalVars},
            {DreamProcOpcode.ModulusModulus, DMOpcodeHandlers.ModulusModulus},
            {DreamProcOpcode.ModulusModulusReference, DMOpcodeHandlers.ModulusModulusReference},
            {DreamProcOpcode.AssignInto, DMOpcodeHandlers.AssignInto},
            {DreamProcOpcode.JumpIfNull, DMOpcodeHandlers.JumpIfNull},
            {DreamProcOpcode.JumpIfNullNoPop, DMOpcodeHandlers.JumpIfNullNoPop},
            {DreamProcOpcode.JumpIfTrueReference, DMOpcodeHandlers.JumpIfTrueReference},
            {DreamProcOpcode.JumpIfFalseReference, DMOpcodeHandlers.JumpIfFalseReference},
            {DreamProcOpcode.DereferenceField, DMOpcodeHandlers.DereferenceField},
            {DreamProcOpcode.DereferenceIndex, DMOpcodeHandlers.DereferenceIndex},
            {DreamProcOpcode.DereferenceCall, DMOpcodeHandlers.DereferenceCall},
            {DreamProcOpcode.PopReference, DMOpcodeHandlers.PopReference},
            {DreamProcOpcode.BitShiftLeftReference,DMOpcodeHandlers.BitShiftLeftReference},
            {DreamProcOpcode.BitShiftRightReference, DMOpcodeHandlers.BitShiftRightReference},
            {DreamProcOpcode.Try, DMOpcodeHandlers.Try},
            {DreamProcOpcode.TryNoValue, DMOpcodeHandlers.TryNoValue},
            {DreamProcOpcode.EndTry, DMOpcodeHandlers.EndTry},
            {DreamProcOpcode.Gradient, DMOpcodeHandlers.Gradient},
            {DreamProcOpcode.Sin, DMOpcodeHandlers.Sin},
            {DreamProcOpcode.Cos, DMOpcodeHandlers.Cos},
            {DreamProcOpcode.Tan, DMOpcodeHandlers.Tan},
            {DreamProcOpcode.ArcSin, DMOpcodeHandlers.ArcSin},
            {DreamProcOpcode.ArcCos, DMOpcodeHandlers.ArcCos},
            {DreamProcOpcode.ArcTan, DMOpcodeHandlers.ArcTan},
            {DreamProcOpcode.ArcTan2, DMOpcodeHandlers.ArcTan2},
            {DreamProcOpcode.Sqrt, DMOpcodeHandlers.Sqrt},
            {DreamProcOpcode.Log, DMOpcodeHandlers.Log},
            {DreamProcOpcode.LogE, DMOpcodeHandlers.LogE},
            {DreamProcOpcode.Abs, DMOpcodeHandlers.Abs},
            {DreamProcOpcode.EnumerateNoAssign, DMOpcodeHandlers.EnumerateNoAssign},
            {DreamProcOpcode.GetStep, DMOpcodeHandlers.GetStep},
            {DreamProcOpcode.Length, DMOpcodeHandlers.Length},
            {DreamProcOpcode.GetDir, DMOpcodeHandlers.GetDir},
            {DreamProcOpcode.DebuggerBreakpoint, DMOpcodeHandlers.DebuggerBreakpoint},
            // Peephole optimizer opcode handlers
            {DreamProcOpcode.PushRefandJumpIfNotNull, DMOpcodeHandlers.PushReferenceAndJumpIfNotNull},
            {DreamProcOpcode.NullRef, DMOpcodeHandlers.NullRef},
            {DreamProcOpcode.AssignPop, DMOpcodeHandlers.AssignPop},
            {DreamProcOpcode.PushRefAndDereferenceField, DMOpcodeHandlers.PushReferenceAndDereferenceField},
            {DreamProcOpcode.PushNRefs, DMOpcodeHandlers.PushNRefs},
            {DreamProcOpcode.PushNFloats, DMOpcodeHandlers.PushNFloats},
            {DreamProcOpcode.PushNStrings, DMOpcodeHandlers.PushNStrings},
            {DreamProcOpcode.PushNResources, DMOpcodeHandlers.PushNResources},
            {DreamProcOpcode.PushStringFloat, DMOpcodeHandlers.PushStringFloat},
            {DreamProcOpcode.SwitchOnFloat, DMOpcodeHandlers.SwitchOnFloat},
            {DreamProcOpcode.SwitchOnString, DMOpcodeHandlers.SwitchOnString},
            {DreamProcOpcode.JumpIfReferenceFalse, DMOpcodeHandlers.JumpIfReferenceFalse},
            {DreamProcOpcode.PushNOfStringFloats, DMOpcodeHandlers.PushNOfStringFloat},
            {DreamProcOpcode.CreateListNFloats, DMOpcodeHandlers.CreateListNFloats},
            {DreamProcOpcode.CreateListNStrings, DMOpcodeHandlers.CreateListNStrings},
            {DreamProcOpcode.CreateListNRefs, DMOpcodeHandlers.CreateListNRefs},
            {DreamProcOpcode.CreateListNResources, DMOpcodeHandlers.CreateListNResources},
            {DreamProcOpcode.JumpIfNotNull, DMOpcodeHandlers.JumpIfNotNull},
            {DreamProcOpcode.IsTypeDirect, DMOpcodeHandlers.IsTypeDirect},
        };

        public static readonly unsafe delegate*<DMProcState, ProcStatus>[] OpcodeHandlers;
        #endregion

        public DreamManager DreamManager => _proc.DreamManager;
        public ProcScheduler ProcScheduler => _proc.ProcScheduler;
        public IDreamDebugManager DebugManager => _proc.DreamDebugManager;

        /// <summary> This stores our 'src' value. May be null!</summary>
        public DreamObject? Instance;
        public DreamObject? Usr;
        public int ArgumentCount;
        private readonly Stack<int> _catchPosition = new();
        private readonly Stack<int> _catchVarIndex = new();
        public const int NoTryCatchVar = -1;
        private Stack<IDreamValueEnumerator>? _enumeratorStack;
        public Stack<IDreamValueEnumerator> EnumeratorStack => _enumeratorStack ??= new(1);

        private int _pc = 0;
        public int ProgramCounter => _pc;

        private bool _firstResume = true;

        // Contains both arguments (at index 0) and local vars (at index ArgumentCount)
        private DreamValue[] _localVariables;

        private DMProc _proc;
        public override DMProc Proc => _proc;

        /// Static initializer for maintainer friendly OpcodeHandlers to performance friendly _opcodeHandlers
        static unsafe DMProcState() {
            int maxOpcode = (int)_opcodeHandlers.Keys.Max();

            OpcodeHandlers = new delegate*<DMProcState, ProcStatus>[256];
            foreach (var (dpo, handler) in _opcodeHandlers) {
                OpcodeHandlers[(int) dpo] = (delegate*<DMProcState, ProcStatus>) handler.Method.MethodHandle.GetFunctionPointer();
            }

            var invalid = DMOpcodeHandlers.Invalid;
            var invalidPtr = (delegate*<DMProcState, ProcStatus>)invalid.Method.MethodHandle.GetFunctionPointer();

            OpcodeHandlers[0] = invalidPtr;
            for (int i = maxOpcode + 1; i < 256; i++) {
                OpcodeHandlers[i] = invalidPtr;
            }
        }

        public DMProcState() { }

        private DMProcState(DMProcState other, DreamThread thread) {
            base.Initialize(thread, other.WaitFor);
            _proc = other._proc;
            Instance = other.Instance;
            Usr = other.Usr;
            ArgumentCount = other.ArgumentCount;
            _pc = other._pc;
            _firstResume = false;

            _stack = _dreamValuePool.Rent(other._stack.Length);
            _localVariables = _dreamValuePool.Rent(other._localVariables.Length);
            Array.Copy(other._localVariables, _localVariables, other._localVariables.Length);
        }

        public void Initialize(DMProc proc, DreamThread thread, int maxStackSize, DreamObject? instance, DreamObject? usr, DreamProcArguments arguments) {
            base.Initialize(thread, (proc.Attributes & ProcAttributes.DisableWaitfor) != ProcAttributes.DisableWaitfor);
            _proc = proc;
            Instance = instance;
            Usr = usr;
            ArgumentCount = Math.Max(arguments.Count, _proc.ArgumentNames?.Count ?? 0);
            _localVariables = _dreamValuePool.Rent(256);
            _stack = _dreamValuePool.Rent(maxStackSize);
            _firstResume = true;

            for (int i = 0; i < ArgumentCount; i++) {
                _localVariables[i] = arguments.GetArgument(i);
            }
        }

        public override unsafe ProcStatus Resume() {
            if (Instance?.Deleted == true) {
                return ProcStatus.Returned;
            }

#if TOOLS
            if (_firstResume) {
                DebugManager.HandleFirstResume(this);
                _firstResume = false;
            }
#endif

            var procBytecode = _proc.Bytecode;

            if (procBytecode.Length == 0)
                return ProcStatus.Returned;

            fixed (delegate*<DMProcState, ProcStatus>* handlers = &OpcodeHandlers[0]) {
                fixed (byte* bytecode = &procBytecode[0]) {
                    var l = procBytecode.Length; // The length never changes so we stick it in a register.

                    while (_pc < l) {
#if TOOLS
                        DebugManager.HandleInstruction(this);
#endif

                        int opcode = bytecode[_pc];
                        _pc += 1;

                        var handler = handlers[opcode];
                        var status = handler(this);

                        if (status != ProcStatus.Continue) {
                            return status;
                        }
                    }
                }
            }

            return ProcStatus.Returned;
        }

        public override void ReturnedInto(DreamValue value) {
            Push(value);
        }

        public override void AppendStackFrame(StringBuilder builder) {
            if (Proc.OwningType != Proc.ObjectTree.Root) {
                builder.Append(Proc.OwningType);
                builder.Append('/');
            }

            builder.Append(Proc.Name);
            builder.Append(':');

            // Subtract 1 because _pc may have been advanced to the next line
            builder.Append(Proc.GetSourceAtOffset(_pc - 1).Line);
        }

        public (string, int) GetCurrentSource() {
            return Proc.GetSourceAtOffset(_pc - 1);
        }

        public void Jump(int position) {
            _pc = position;
        }

        public void SetReturn(DreamValue value) {
            Result = value;
        }

        public ProcStatus Call(DreamProc proc, DreamObject? src, DreamProcArguments arguments) {
            if (proc is NativeProc p) {
                // Skip a whole song and dance.
                Push(p.Call(Thread, src, Usr, arguments));
                return ProcStatus.Continue;
            }

            var state = proc.CreateState(Thread, src, Usr, arguments);
            Thread.PushProcState(state);
            return ProcStatus.Called;
        }

        public DreamThread Spawn() {
            var thread = new DreamThread(Proc.ToString());

            var state = new DMProcState(this, thread);
            thread.PushProcState(state);

            return thread;
        }

        public void StartTryBlock(int catchPosition, int catchVarIndex = NoTryCatchVar) {
            _catchPosition.Push(catchPosition);
            _catchVarIndex.Push(catchVarIndex);
        }

        public void EndTryBlock() {
            _catchPosition.Pop();
            _catchVarIndex.Pop();
        }

        public override bool IsCatching() => _catchPosition.Count > 0;

        public override void CatchException(Exception exception) {
            if (!IsCatching())
                base.CatchException(exception);

            Jump(_catchPosition.Pop());
            var varIdx = _catchVarIndex.Pop();
            if (varIdx != NoTryCatchVar) {
                DreamValue value;

                if (exception is DMThrowException throwException)
                    value = throwException.Value;
                else
                    value = new DreamValue(exception.Message); // TODO: Probably need to create an /exception

                _localVariables[varIdx] = value;
            }
        }

        public override void Dispose() {
            base.Dispose();

            Instance = null;
            Usr = null;
            ArgumentCount = 0;
            _enumeratorStack = null;
            _pc = 0;
            _proc = null;

            _dreamValuePool.Return(_stack);
            _stackIndex = 0;
            _stack = null;

            _dreamValuePool.Return(_localVariables);
            _localVariables = null;

            _catchPosition.Clear();
            _catchVarIndex.Clear();

            Pool.Push(this);
        }

        public ReadOnlySpan<DreamValue> GetArguments() {
            return _localVariables.AsSpan(0, ArgumentCount);
        }

        public void SetArgument(int id, DreamValue value) {
            if (id < 0 || id >= ArgumentCount)
                throw new IndexOutOfRangeException($"Given argument id ({id}) was out of range");

            _localVariables[id] = value;
        }

        #region Stack
        private DreamValue[] _stack;
        private int _stackIndex = 0;
        public ReadOnlyMemory<DreamValue> DebugStack() => _stack.AsMemory(0, _stackIndex);

        public void Push(DreamValue value) {
            _stack[_stackIndex] = value;
            // ++ sucks for the compiler
            _stackIndex += 1;
        }

        public DreamValue Pop() {
            // -- sucks for the compiler
            _stackIndex -= 1;
            return _stack[_stackIndex];
        }

        public void PopDrop() {
            _stackIndex -= 1;
        }

        /// <summary>
        /// Pops multiple values off the stack
        /// </summary>
        /// <param name="count">Amount of values to pop</param>
        /// <returns>A ReadOnlySpan of the popped values, in FIFO order</returns>
        public ReadOnlySpan<DreamValue> PopCount(int count) {
            _stackIndex -= count;

            return _stack.AsSpan(_stackIndex, count);
        }

        public DreamValue Peek() {
            return _stack[_stackIndex - 1];
        }

        /// <summary>
        /// Pops arguments off the stack and returns them in DreamProcArguments
        /// </summary>
        /// <param name="proc">The target proc we're calling. If null, named args or arglist() cannot be used.</param>
        /// <param name="argumentsType">The source of the arguments</param>
        /// <param name="argumentStackSize">The amount of items the arguments have on the stack</param>
        /// <returns>The arguments in a DreamProcArguments struct</returns>
        public DreamProcArguments PopProcArguments(DreamProc? proc, DMCallArgumentsType argumentsType, int argumentStackSize) {
            var values = PopCount(argumentStackSize);

            return CreateProcArguments(values, proc, argumentsType, argumentStackSize);
        }
        #endregion

        #region Operands
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadByte() {
            var r = _proc.Bytecode[_pc];
            _pc += 1;
            return r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt() {
            int value = BitConverter.ToInt32(_proc.Bytecode, _pc);
            _pc += 4;

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ReadFloat() {
            float value = BitConverter.ToSingle(_proc.Bytecode, _pc);
            _pc += 4;

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadString() {
            int stringId = ReadInt();

            return ResolveString(stringId);
        }

        public string ResolveString(int stringId) {
            return Proc.ObjectTree.Strings[stringId];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DreamReference ReadReference() {
            DMReference.Type refType = (DMReference.Type)ReadByte();

            switch (refType) {
                case DMReference.Type.Src:
                case DMReference.Type.Self:
                case DMReference.Type.Usr:
                case DMReference.Type.Args:
                case DMReference.Type.SuperProc:
                case DMReference.Type.ListIndex:
                    return new DreamReference(refType, 0);
                case DMReference.Type.Argument:
                case DMReference.Type.Local:
                    return new DreamReference(refType, ReadByte());
                case DMReference.Type.Global:
                case DMReference.Type.GlobalProc:
                case DMReference.Type.Field:
                case DMReference.Type.SrcField:
                case DMReference.Type.SrcProc:
                    return new DreamReference(refType, ReadInt());
                default: {
                    ThrowInvalidReferenceType(refType);
                    return default;
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidReferenceType(DMReference.Type type) {
            throw new Exception($"Invalid reference type {type}");
        }

        public (DMCallArgumentsType Type, int StackSize) ReadProcArguments() {
            return ((DMCallArgumentsType) ReadByte(), ReadInt());
        }
        #endregion

        #region References
        public bool IsNullDereference(DreamReference reference) {
            switch (reference.Type) {
                case DMReference.Type.Field: {
                    if (Peek().IsNull) {
                        PopDrop();
                        return true;
                    }

                    return false;
                }
                case DMReference.Type.ListIndex: {
                    DreamValue list = _stack[_stackIndex - 2];
                    if (list.IsNull) {
                        PopDrop();
                        PopDrop();
                        return true;
                    }

                    return false;
                }
                default: throw new Exception($"Invalid dereference type {reference.Type}");
            }
        }

        /// <summary>
        /// Takes a DMReference with a <see cref="DMReference.Type.ListIndex"/> type and returns the value being indexed
        /// as well as what it's being indexed with.
        /// </summary>
        /// <param name="reference">A ListIndex DMReference</param>
        public void GetIndexReferenceValues(DreamReference reference, out DreamValue index, out DreamValue indexing, bool peek = false) {
            if (reference.Type != DMReference.Type.ListIndex)
                ThrowReferenceNotListIndex();

            index = _stack[_stackIndex - 1];
            indexing = _stack[_stackIndex - 2];
            if (!peek)
                _stackIndex -= 2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowReferenceNotListIndex() {
            throw new ArgumentException("Reference was not a ListIndex type");
        }

        public void AssignReference(DreamReference reference, DreamValue value) {
            switch (reference.Type) {
                case DMReference.Type.Self: Result = value; break;
                case DMReference.Type.Argument: SetArgument(reference.Value, value); break;
                case DMReference.Type.Local: _localVariables[ArgumentCount + reference.Value] = value; break;
                case DMReference.Type.SrcField: Instance.SetVariable(ResolveString(reference.Value), value); break;
                case DMReference.Type.Global: DreamManager.Globals[reference.Value] = value; break;
                case DMReference.Type.Src:
                    //TODO: src can be assigned to non-DreamObject values
                    if (!value.TryGetValueAsDreamObject(out Instance)) {
                        ThrowCannotAssignSrcTo(value);
                    }

                    break;
                case DMReference.Type.Usr:
                    //TODO: usr can be assigned to non-DreamObject values
                    if (!value.TryGetValueAsDreamObject(out Usr)) {
                        ThrowCannotAssignUsrTo(value);
                    }
                    break;
                case DMReference.Type.Field: {
                    DreamValue owner = Pop();
                    if (!owner.TryGetValueAsDreamObject(out var ownerObj) || ownerObj == null)
                        ThrowCannotAssignFieldOn(reference, owner);

                    ownerObj!.SetVariable(ResolveString(reference.Value), value);
                    break;
                }
                case DMReference.Type.ListIndex: {
                    GetIndexReferenceValues(reference, out var index, out var indexing);

                    if (indexing.TryGetValueAsDreamObject(out var dreamObject) && dreamObject != null) {
                        dreamObject.OperatorIndexAssign(index, value);
                    } else {
                        ThrowCannotAssignListIndex(index, indexing);
                    }

                    break;
                }
                default:
                    ThrowCannotAssignReferenceType(reference);
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowCannotAssignReferenceType(DreamReference reference) {
            throw new Exception($"Cannot assign to reference type {reference.Type}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowCannotAssignListIndex(DreamValue index, DreamValue indexing) {
            throw new Exception($"Cannot assign to index {index} of {indexing}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ThrowCannotAssignFieldOn(DreamReference reference, DreamValue owner) {
            throw new Exception($"Cannot assign field \"{ResolveString(reference.Value)}\" on {owner}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowCannotAssignSrcTo(DreamValue value) {
            throw new Exception($"Cannot assign src to {value}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowCannotAssignUsrTo(DreamValue value) {
            throw new Exception($"Cannot assign usr to {value}");
        }

        public DreamValue GetReferenceValue(DreamReference reference, bool peek = false) {
            switch (reference.Type) {
                case DMReference.Type.Src: return new(Instance);
                case DMReference.Type.Usr: return new(Usr);
                case DMReference.Type.Self: return Result;
                case DMReference.Type.Global: return DreamManager.Globals[reference.Value];
                case DMReference.Type.Argument: return _localVariables[reference.Value];
                case DMReference.Type.Local: return _localVariables[ArgumentCount + reference.Value];
                case DMReference.Type.Args: return new(new ProcArgsList(Proc.ObjectTree.List.ObjectDefinition, this));
                case DMReference.Type.Field: {
                    DreamValue owner = peek ? Peek() : Pop();

                    return DereferenceField(owner, ResolveString(reference.Value));
                }
                case DMReference.Type.SrcField: {
                    var fieldName = ResolveString(reference.Value);
                    if (Instance == null)
                        ThrowCannotGetFieldSrcGlobalProc(fieldName);
                    if (!Instance!.TryGetVariable(fieldName, out var fieldValue))
                        ThrowTypeHasNoField(fieldName);

                    return fieldValue;
                }
                case DMReference.Type.ListIndex: {
                    GetIndexReferenceValues(reference, out var index, out var indexing, peek);

                    return GetIndex(indexing, index);
                }
                default:
                    ThrowCannotGetValueOfReferenceType(reference);
                    return DreamValue.Null;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowCannotGetValueOfReferenceType(DreamReference reference) {
            throw new Exception($"Cannot get value of reference type {reference.Type}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowCannotGetFieldSrcGlobalProc(string fieldName) {
            throw new Exception($"Cannot get field src.{fieldName} in global proc");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ThrowTypeHasNoField(string fieldName) {
            throw new Exception($"Type {Instance!.ObjectDefinition!.Type} has no field called \"{fieldName}\"");
        }

        public void PopReference(DreamReference reference) {
            switch (reference.Type) {
                case DMReference.Type.Src:
                case DMReference.Type.Usr:
                case DMReference.Type.Self:
                case DMReference.Type.Global:
                case DMReference.Type.GlobalProc:
                case DMReference.Type.Argument:
                case DMReference.Type.Local:
                case DMReference.Type.Args:
                case DMReference.Type.SrcField:
                    return;
                case DMReference.Type.ListIndex:
                    PopDrop();

                    // Fallthrough to the below case ends up with more performant generated code
                    goto case DMReference.Type.Field;
                case DMReference.Type.Field:
                    PopDrop();
                    return;
                default: ThrowPopInvalidType(reference.Type);
                    return;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowPopInvalidType(DMReference.Type type) {
            throw new Exception($"Cannot pop stack values of reference type {type}");
        }

        public DreamValue DereferenceField(DreamValue owner, string field) {
            if (owner.TryGetValueAsDreamObject(out var ownerObj) && ownerObj != null) {
                if (!ownerObj.TryGetVariable(field, out var fieldValue))
                    ThrowTypeHasNoField(field, ownerObj);

                return fieldValue;
            } else if (owner.TryGetValueAsProc(out var ownerProc)) {
                return ownerProc.GetField(field);
            } else if (owner.TryGetValueAsAppearance(out var appearance)) {
                if (!Proc.AtomManager.IsValidAppearanceVar(field))
                    ThrowInvalidAppearanceVar(field);

                return Proc.AtomManager.GetAppearanceVar(appearance, field);
            }

            ThrowCannotGetFieldFromOwner(owner, field);
            return DreamValue.Null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowCannotGetFieldFromOwner(DreamValue owner, string field) {
            throw new Exception($"Cannot get field \"{field}\" from {owner}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidAppearanceVar(string field) {
            throw new Exception($"Invalid appearance var \"{field}\"");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowTypeHasNoField(string field, DreamObject? ownerObj) {
            throw new Exception($"Type {ownerObj.ObjectDefinition.Type} has no field called \"{field}\"");
        }

        public DreamValue GetIndex(DreamValue indexing, DreamValue index) {
            if (indexing.TryGetValueAsDreamList(out var listObj)) {
                return listObj.GetValue(index);
            }

            if (indexing.TryGetValueAsString(out string? strValue)) {
                if (!index.TryGetValueAsInteger(out int strIndex))
                    ThrowAttemptedToIndexString(index);

                char c = strValue[strIndex - 1];
                return new DreamValue(Convert.ToString(c));
            }

            if (indexing.TryGetValueAsDreamObject(out var dreamObject)) {
                if (dreamObject != null)
                    return dreamObject.OperatorIndex(index);
            }

            ThrowCannotGetIndex(indexing, index);
            return DreamValue.Null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowCannotGetIndex(DreamValue indexing, DreamValue index) {
            throw new Exception($"Cannot get index {index} of {indexing}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowAttemptedToIndexString(DreamValue index) {
            throw new Exception($"Attempted to index string with {index}");
        }

        #endregion References

        public IEnumerable<(string, DreamValue)> DebugArguments() {
            int i = 0;
            if (_proc.ArgumentNames != null) {
                while (i < _proc.ArgumentNames.Count) {
                    yield return (_proc.ArgumentNames[i], _localVariables[i]);
                    ++i;
                }
            }
            // If the caller supplied excess positional arguments, they have no
            // name, but the debugger should report them anyways.
            while (i < ArgumentCount) {
                yield return (i.ToString(), _localVariables[i]);
                ++i;
            }
        }

        public IEnumerable<(string, DreamValue)> DebugLocals() {
            if (_proc.LocalNames is null) {
                yield break;
            }

            string[] names = new string[_localVariables.Length - ArgumentCount];
            int count = 0;
            foreach (var info in _proc.LocalNames) {
                if (info.Offset > _pc) {
                    break;
                }
                if (info.Remove is int remove) {
                    count -= remove;
                }
                if (info.Add is string add) {
                    names[count++] = add;
                }
            }

            int i = 0, j = ArgumentCount;
            while (i < count && j < _localVariables.Length) {
                yield return (names[i], _localVariables[j]);
                ++i;
                ++j;
            }
            // _localVariables.Length is pool-allocated so its length may go up
            // to some round power of two or similar without anything actually
            // being there, so just stop after the named locals.
        }

        public DreamProcArguments CreateProcArguments(ReadOnlySpan<DreamValue> values, DreamProc? proc, DMCallArgumentsType argumentsType, int argumentStackSize) {
            switch (argumentsType) {
                case DMCallArgumentsType.None:
                    return new DreamProcArguments();
                case DMCallArgumentsType.FromStack:
                    return new DreamProcArguments(values);
                case DMCallArgumentsType.FromProcArguments:
                    return new DreamProcArguments(GetArguments());
                case DMCallArgumentsType.FromStackKeyed: {
                    if (argumentStackSize % 2 != 0)
                        throw new ArgumentException("Argument stack size must be even", nameof(argumentStackSize));
                    if (proc == null)
                        throw new Exception("Cannot use named arguments here");

                    var argumentCount = argumentStackSize / 2;
                    var arguments = new DreamValue[Math.Max(argumentCount, proc.ArgumentNames.Count)];
                    var skippingArg = false;
                    var isImageConstructor = proc == Proc.DreamManager.ImageConstructor ||
                                             proc == Proc.DreamManager.ImageFactoryProc;

                    Array.Fill(arguments, DreamValue.Null);
                    for (int i = 0; i < argumentCount; i++) {
                        var key = values[i * 2];
                        var value = values[i * 2 + 1];

                        if (key.IsNull) {
                            // image() or new /image() will skip the loc arg if the second arg is a string
                            // Really don't like this but it's BYOND behavior
                            // Note that the way we're doing it leads to different argument placement when there are no named args
                            // Hopefully nothing depends on that though
                            // TODO: We aim to do sanity improvements in the future, yea? Big one here
                            if (isImageConstructor && i == 1 && value.Type == DreamValue.DreamValueType.String)
                                skippingArg = true;

                            arguments[skippingArg ? i + 1 : i] = value;
                        } else {
                            string argumentName = key.MustGetValueAsString();
                            int argumentIndex = proc.ArgumentNames.IndexOf(argumentName);
                            if (argumentIndex == -1)
                                throw new Exception($"{proc} has no argument named {argumentName}");

                            arguments[argumentIndex] = value;
                        }
                    }

                    return new DreamProcArguments(arguments);
                }
                case DMCallArgumentsType.FromArgumentList: {
                    if (proc == null)
                        throw new Exception("Cannot use an arglist here");
                    if (!values[0].TryGetValueAsDreamList(out var argList))
                        return new DreamProcArguments(); // Using a non-list gives you no arguments

                    var listValues = argList.GetValues();
                    var arguments = new DreamValue[Math.Max(listValues.Count, proc.ArgumentNames.Count)];
                    var skippingArg = false;
                    var isImageConstructor = proc == Proc.DreamManager.ImageConstructor ||
                                             proc == Proc.DreamManager.ImageFactoryProc;

                    Array.Fill(arguments, DreamValue.Null);
                    for (int i = 0; i < listValues.Count; i++) {
                        var value = listValues[i];

                        if (argList.ContainsKey(value)) { //Named argument
                            if (!value.TryGetValueAsString(out var argumentName))
                                throw new Exception("List contains a non-string key, and cannot be used as an arglist");

                            int argumentIndex = proc.ArgumentNames.IndexOf(argumentName);
                            if (argumentIndex == -1)
                                throw new Exception($"{proc} has no argument named {argumentName}");

                            arguments[argumentIndex] = argList.GetValue(value);
                        } else { //Ordered argument
                            // image() or new /image() will skip the loc arg if the second arg is a string
                            // Really don't like this but it's BYOND behavior
                            // Note that the way we're doing it leads to different argument placement when there are no named args
                            // Hopefully nothing depends on that though
                            if (isImageConstructor && i == 1 && value.Type == DreamValue.DreamValueType.String)
                                skippingArg = true;

                            // TODO: Verify ordered args precede all named args
                            arguments[skippingArg ? i + 1 : i] = value;
                        }
                    }

                    return new DreamProcArguments(arguments);
                }
                default:
                    throw new Exception($"Invalid arguments type {argumentsType}");
            }
        }
    }
}
