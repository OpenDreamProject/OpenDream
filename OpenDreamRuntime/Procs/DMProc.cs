using System.Buffers;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using OpenDreamRuntime.Procs.DebugAdapter;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;
using OpenDreamShared.Json;

namespace OpenDreamRuntime.Procs {
    public sealed class DMProc : DreamProc {
        public readonly byte[] Bytecode;

        public string? Source { get; }
        public int Line { get; }
        public IReadOnlyList<LocalVariableJson> LocalNames { get; }

        public readonly IAtomManager AtomManager;
        public readonly IDreamManager DreamManager;
        public readonly IDreamMapManager DreamMapManager;
        public readonly IDreamDebugManager DreamDebugManager;
        public readonly DreamResourceManager DreamResourceManager;
        public readonly IDreamObjectTree ObjectTree;

        private readonly int _maxStackSize;

        public DMProc(int id, DreamPath owningType, ProcDefinitionJson json, string? name, IDreamManager dreamManager, IAtomManager atomManager, IDreamMapManager dreamMapManager, IDreamDebugManager dreamDebugManager, DreamResourceManager dreamResourceManager, IDreamObjectTree objectTree)
            : base(id, owningType, name ?? json.Name, null, json.Attributes, GetArgumentNames(json), GetArgumentTypes(json), json.VerbName, json.VerbCategory, json.VerbDesc, json.Invisibility, json.IsVerb) {
            Bytecode = json.Bytecode ?? Array.Empty<byte>();
            LocalNames = json.Locals;
            Source = json.Source;
            Line = json.Line;
            _maxStackSize = json.MaxStackSize;

            AtomManager = atomManager;
            DreamManager = dreamManager;
            DreamMapManager = dreamMapManager;
            DreamDebugManager = dreamDebugManager;
            DreamResourceManager = dreamResourceManager;
            ObjectTree = objectTree;
        }

        public override DMProcState CreateState(DreamThread thread, DreamObject? src, DreamObject? usr, DreamProcArguments arguments) {
            if (!DMProcState.Pool.TryPop(out var state)) {
                state = new DMProcState();
            }

            state.Initialize(this, thread, _maxStackSize, src, usr, arguments);
            return state;
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

        private static List<DMValueType>? GetArgumentTypes(ProcDefinitionJson json) {
            if (json.Arguments == null) {
                return new();
            } else {
                var argumentTypes = new List<DMValueType>(json.Arguments.Count);
                argumentTypes.AddRange(json.Arguments.Select(a => a.Type));
                return argumentTypes;
            }
        }
    }

    public sealed class DMProcState : ProcState {
        private delegate ProcStatus? OpcodeHandler(DMProcState state);

        public static readonly Stack<DMProcState> Pool = new();

        private static readonly ArrayPool<DreamValue> _dreamValuePool = ArrayPool<DreamValue>.Create();

        #region Opcode Handlers
        //Human readable friendly version, which will be converted to a more efficient lookup at runtime.
        private static readonly Dictionary<DreamProcOpcode, OpcodeHandler?> OpcodeHandlers = new Dictionary<DreamProcOpcode, OpcodeHandler?>(){
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
            {DreamProcOpcode.DebugSource, DMOpcodeHandlers.DebugSource},
            {DreamProcOpcode.DebugLine, DMOpcodeHandlers.DebugLine},
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
            {DreamProcOpcode.PushProcStub, DMOpcodeHandlers.PushProcStub},
            {DreamProcOpcode.PushVerbStub, DMOpcodeHandlers.PushVerbStub},
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
            {DreamProcOpcode.EnumerateNoAssign, DMOpcodeHandlers.EnumerateNoAssign}
        };

        private static readonly OpcodeHandler?[] _opcodeHandlers;
        #endregion

        public IDreamManager DreamManager => _proc.DreamManager;
        public IDreamDebugManager DebugManager => _proc.DreamDebugManager;

        /// <summary> This stores our 'src' value. May be null!</summary>
        public DreamObject? Instance;
        public DreamObject? Usr;
        public int ArgumentCount;
        public string? CurrentSource;
        public int CurrentLine;
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

        public override (string?, int?) SourceLine => (CurrentSource, CurrentLine);

        /// Static initializer for maintainer friendly OpcodeHandlers to performance friendly _opcodeHandlers
        static DMProcState() {
            int maxOpcode = (int)OpcodeHandlers.Keys.Max();

            _opcodeHandlers = new OpcodeHandler?[maxOpcode + 1];
            foreach (DreamProcOpcode dpo in OpcodeHandlers.Keys) {
                _opcodeHandlers[(int) dpo] = OpcodeHandlers[dpo];
            }
        }

        public DMProcState() { }

        private DMProcState(DMProcState other, DreamThread thread) {
            base.Initialize(thread, other.WaitFor);
            _proc = other._proc;
            Instance = other.Instance;
            Usr = other.Usr;
            ArgumentCount = other.ArgumentCount;
            CurrentSource = other.CurrentSource;
            CurrentLine = other.CurrentLine;
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
            CurrentSource = _proc.Source;
            CurrentLine = _proc.Line;
            _localVariables = _dreamValuePool.Rent(256);
            _stack = _dreamValuePool.Rent(maxStackSize);
            _firstResume = true;

            for (int i = 0; i < ArgumentCount; i++) {
                _localVariables[i] = arguments.GetArgument(i);
            }
        }

        public override ProcStatus Resume() {
            if (Instance?.Deleted == true) {
                return ProcStatus.Returned;
            }

            if (_firstResume) {
                DebugManager.HandleFirstResume(this);
            }

            bool stepping = Thread.StepMode != null;
            while (_pc < _proc.Bytecode.Length) {
                if (stepping && !_firstResume) // HandleFirstResume does this for us on the first resume
                    DebugManager.HandleInstruction(this);
                _firstResume = false;

                int opcode = _proc.Bytecode[_pc++];
                var handler = opcode < _opcodeHandlers.Length ? _opcodeHandlers[opcode] : null;
                if (handler is null)
                    throw new Exception($"Attempted to call non-existent Opcode method for opcode 0x{opcode:X2}");

                ProcStatus? status;
                try {
                    status = handler.Invoke(this);
                } catch (Exception e) {
                    if (!IsCatching())
                        throw;

                    CatchException(e);
                    continue;
                }

                if (status != null) {
                    return status.Value;
                }
            }

            return ProcStatus.Returned;
        }

        public override void ReturnedInto(DreamValue value) {
            Push(value);
        }

        public override void AppendStackFrame(StringBuilder builder) {
            if (Proc.OwningType != DreamPath.Root) {
                builder.Append(Proc.OwningType.ToString());
                builder.Append('/');
            }

            builder.Append(Proc.Name);
            builder.Append(':');
            builder.Append(CurrentLine);
        }

        public void Jump(int position) {
            _pc = position;
        }

        public void SetReturn(DreamValue value) {
            Result = value;
        }

        public void Call(DreamProc proc, DreamObject? src, DreamProcArguments arguments) {
            var state = proc.CreateState(Thread, src, Usr, arguments);
            Thread.PushProcState(state);
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
            CurrentSource = null;
            CurrentLine = 0;
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
            _stack[_stackIndex++] = value;
        }

        public DreamValue Pop() {
            return _stack[--_stackIndex];
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
            return _proc.Bytecode[_pc++];
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

            return Proc.ObjectTree.Strings[stringId];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DMReference ReadReference() {
            DMReference.Type refType = (DMReference.Type)ReadByte();

            switch (refType) {
                case DMReference.Type.Argument: return DMReference.CreateArgument(ReadByte());
                case DMReference.Type.Local: return DMReference.CreateLocal(ReadByte());
                case DMReference.Type.Global: return DMReference.CreateGlobal(ReadInt());
                case DMReference.Type.GlobalProc: return DMReference.CreateGlobalProc(ReadInt());
                case DMReference.Type.Field: return DMReference.CreateField(ReadString());
                case DMReference.Type.SrcField: return DMReference.CreateSrcField(ReadString());
                case DMReference.Type.SrcProc: return DMReference.CreateSrcProc(ReadString());
                case DMReference.Type.Src: return DMReference.Src;
                case DMReference.Type.Self: return DMReference.Self;
                case DMReference.Type.Usr: return DMReference.Usr;
                case DMReference.Type.Args: return DMReference.Args;
                case DMReference.Type.SuperProc: return DMReference.SuperProc;
                case DMReference.Type.ListIndex: return DMReference.ListIndex;
                default: throw new Exception($"Invalid reference type {refType}");
            }
        }

        public (DMCallArgumentsType Type, int StackSize) ReadProcArguments() {
            return ((DMCallArgumentsType) ReadByte(), ReadInt());
        }
        #endregion

        #region References
        public bool IsNullDereference(DMReference reference) {
            switch (reference.RefType) {
                case DMReference.Type.Field: {
                    if (Peek() == DreamValue.Null) {
                        Pop();
                        return true;
                    }

                    return false;
                }
                case DMReference.Type.ListIndex: {
                    DreamValue list = _stack[_stackIndex - 2];
                    if (list == DreamValue.Null) {
                        Pop();
                        Pop();
                        return true;
                    }

                    return false;
                }
                default: throw new Exception($"Invalid dereference type {reference.RefType}");
            }
        }

        /// <summary>
        /// Takes a DMReference with a <see cref="DMReference.Type.ListIndex"/> type and returns the value being indexed
        /// as well as what it's being indexed with.
        /// </summary>
        /// <param name="reference">A ListIndex DMReference</param>
        public (DreamValue indexing, DreamValue index) GetIndexReferenceValues(DMReference reference, bool peek = false) {
            if (reference.RefType != DMReference.Type.ListIndex)
                throw new ArgumentException("Reference was not a ListIndex type");

            DreamValue index = peek ? _stack[_stackIndex - 1] : Pop();
            DreamValue indexing = peek ? _stack[_stackIndex - 2] : Pop();
            return (indexing, index);
        }

        public void AssignReference(DMReference reference, DreamValue value) {
            switch (reference.RefType) {
                case DMReference.Type.Self: Result = value; break;
                case DMReference.Type.Argument: SetArgument(reference.Index, value); break;
                case DMReference.Type.Local: _localVariables[ArgumentCount + reference.Index] = value; break;
                case DMReference.Type.SrcField: Instance.SetVariable(reference.Name, value); break;
                case DMReference.Type.Global: DreamManager.Globals[reference.Index] = value; break;
                case DMReference.Type.Src:
                    //TODO: src can be assigned to non-DreamObject values
                    if (!value.TryGetValueAsDreamObject(out Instance)) {
                        throw new Exception($"Cannot assign src to {value}");
                    }

                    break;
                case DMReference.Type.Usr:
                    //TODO: usr can be assigned to non-DreamObject values
                    if (!value.TryGetValueAsDreamObject(out Usr)) {
                        throw new Exception($"Cannot assign usr to {value}");
                    }
                    break;
                case DMReference.Type.Field: {
                    DreamValue owner = Pop();
                    if (!owner.TryGetValueAsDreamObject(out var ownerObj) || ownerObj == null)
                        throw new Exception($"Cannot assign field \"{reference.Name}\" on {owner}");

                    ownerObj.SetVariable(reference.Name, value);
                    break;
                }
                case DMReference.Type.ListIndex: {
                    (DreamValue indexing, DreamValue index) = GetIndexReferenceValues(reference);

                    if (indexing.TryGetValueAsDreamObject(out var dreamObject) && dreamObject != null) {
                        dreamObject.OperatorIndexAssign(index, value);
                    } else {
                        throw new Exception($"Cannot assign to index {index} of {indexing}");
                    }

                    break;
                }
                default: throw new Exception($"Cannot assign to reference type {reference.RefType}");
            }
        }

        public DreamValue GetReferenceValue(DMReference reference, bool peek = false) {
            switch (reference.RefType) {
                case DMReference.Type.Src: return new(Instance);
                case DMReference.Type.Usr: return new(Usr);
                case DMReference.Type.Self: return Result;
                case DMReference.Type.Global: return DreamManager.Globals[reference.Index];
                case DMReference.Type.Argument: return _localVariables[reference.Index];
                case DMReference.Type.Local: return _localVariables[ArgumentCount + reference.Index];
                case DMReference.Type.Args: return new(new ProcArgsList(Proc.ObjectTree.List.ObjectDefinition, this));
                case DMReference.Type.Field: {
                    DreamValue owner = peek ? Peek() : Pop();

                    return DereferenceField(owner, reference.Name);
                }
                case DMReference.Type.SrcField: {
                    if (Instance == null)
                        throw new Exception($"Cannot get field src.{reference.Name} in global proc");
                    if (!Instance.TryGetVariable(reference.Name, out var fieldValue))
                        throw new Exception($"Type {Instance.ObjectDefinition!.Type} has no field called \"{reference.Name}\"");

                    return fieldValue;
                }
                case DMReference.Type.ListIndex: {
                    (DreamValue indexing, DreamValue index) = GetIndexReferenceValues(reference, peek);

                    return GetIndex(indexing, index);
                }
                default: throw new Exception($"Cannot get value of reference type {reference.RefType}");
            }
        }

        public void PopReference(DMReference reference) {
            switch (reference.RefType) {
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
                case DMReference.Type.Field:
                    Pop();
                    return;
                case DMReference.Type.ListIndex:
                    Pop();
                    Pop();
                    return;
                default: throw new Exception($"Cannot pop stack values of reference type {reference.RefType}");
            }
        }

        public DreamValue DereferenceField(DreamValue owner, string field) {
            if (owner.TryGetValueAsDreamObject(out var ownerObj) && ownerObj != null) {
                if (!ownerObj.TryGetVariable(field, out var fieldValue))
                    throw new Exception($"Type {ownerObj.ObjectDefinition.Type} has no field called \"{field}\"");

                return fieldValue;
            } else if (owner.TryGetValueAsProc(out var ownerProc)) {
                return ownerProc.GetField(field);
            } else if (owner.TryGetValueAsAppearance(out var appearance)) {
                if (!Proc.AtomManager.IsValidAppearanceVar(field))
                    throw new Exception($"Invalid appearance var \"{field}\"");

                return Proc.AtomManager.GetAppearanceVar(appearance, field);
            }

            throw new Exception($"Cannot get field \"{field}\" from {owner}");
        }

        public DreamValue GetIndex(DreamValue indexing, DreamValue index) {
            if (indexing.TryGetValueAsDreamList(out var listObj)) {
                return listObj.GetValue(index);
            }

            if (indexing.TryGetValueAsString(out string? strValue)) {
                if (!index.TryGetValueAsInteger(out int strIndex))
                    throw new Exception($"Attempted to index string with {index}");

                char c = strValue[strIndex - 1];
                return new DreamValue(Convert.ToString(c));
            }

            if (indexing.TryGetValueAsDreamObject(out var dreamObject)) {
                if (dreamObject != null)
                    return dreamObject.OperatorIndex(index);
            }

            throw new Exception($"Cannot get index {index} of {indexing}");
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
                case DMCallArgumentsType.FromProcArguments:
                    return new DreamProcArguments(GetArguments());
                case DMCallArgumentsType.FromStack:
                    return new DreamProcArguments(values);
                case DMCallArgumentsType.FromStackKeyed: {
                    if (argumentStackSize % 2 != 0)
                        throw new ArgumentException("Argument stack size must be even", nameof(argumentStackSize));
                    if (proc == null)
                        throw new Exception("Cannot use named arguments here");

                    var argumentCount = argumentStackSize / 2;
                    var arguments = new DreamValue[Math.Max(argumentCount, proc.ArgumentNames.Count)];

                    Array.Fill(arguments, DreamValue.Null);
                    for (int i = 0; i < argumentCount; i++) {
                        var key = values[i*2];
                        var value = values[i*2+1];

                        if (key == DreamValue.Null) {
                            arguments[i] = value;
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
                            // TODO: Verify ordered args precede all named args
                            arguments[i] = value;
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
