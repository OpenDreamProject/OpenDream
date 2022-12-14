using System.Buffers;
using System.Text;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.MetaObjects;
using OpenDreamRuntime.Procs.DebugAdapter;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;

namespace OpenDreamRuntime.Procs {
    public sealed class DMProc : DreamProc {
        public byte[] Bytecode { get; }

        private readonly int _maxStackSize;

        public string? Source { get; set; }
        public int Line { get; set; }

        public DMProc(DreamPath owningType, string name, DreamProc superProc, List<String> argumentNames, List<DMValueType> argumentTypes, byte[] bytecode, int maxStackSize, ProcAttributes attributes, string? verbName, string? verbCategory, string? verbDesc, sbyte? invisibility)
            : base(owningType, name, superProc, attributes, argumentNames, argumentTypes, verbName, verbCategory, verbDesc, invisibility)
        {
            Bytecode = bytecode;
            _maxStackSize = maxStackSize;
        }

        public override DMProcState CreateState(DreamThread thread, DreamObject? src, DreamObject? usr, DreamProcArguments arguments)
        {
            return new DMProcState(this, thread, _maxStackSize, src, usr, arguments);
        }
    }

    public sealed class DMProcState : ProcState
    {
        delegate ProcStatus? OpcodeHandler(DMProcState state);

        // TODO: These pools are not returned to if the proc runtimes while _current is null
        private static ArrayPool<DreamValue> _dreamValuePool = ArrayPool<DreamValue>.Shared;
        private static ArrayPool<DreamValue> _stackPool = ArrayPool<DreamValue>.Shared;

        #region Opcode Handlers
        private static readonly Dictionary<DreamProcOpcode, OpcodeHandler?> OpcodeHandlers = new Dictionary<DreamProcOpcode, OpcodeHandler?>(){
            {DreamProcOpcode.BitShiftLeft, DMOpcodeHandlers.BitShiftLeft},
            {DreamProcOpcode.PushType, DMOpcodeHandlers.PushType},
            {DreamProcOpcode.PushString, DMOpcodeHandlers.PushString},
            {DreamProcOpcode.FormatString, DMOpcodeHandlers.FormatString},
            {DreamProcOpcode.SwitchCaseRange, DMOpcodeHandlers.SwitchCaseRange},
            {DreamProcOpcode.PushReferenceValue, DMOpcodeHandlers.PushReferenceValue},
            {DreamProcOpcode.PushPath, DMOpcodeHandlers.PushPath},
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
            {DreamProcOpcode.Divide, DMOpcodeHandlers.Divide},
            {DreamProcOpcode.Multiply, DMOpcodeHandlers.Multiply},
            {DreamProcOpcode.BitXorReference, DMOpcodeHandlers.BitXorReference},
            {DreamProcOpcode.BitXor, DMOpcodeHandlers.BitXor},
            {DreamProcOpcode.BitOr, DMOpcodeHandlers.BitOr},
            {DreamProcOpcode.BitNot, DMOpcodeHandlers.BitNot},
            {DreamProcOpcode.Combine, DMOpcodeHandlers.Combine},
            {DreamProcOpcode.CreateObject, DMOpcodeHandlers.CreateObject},
            {DreamProcOpcode.BooleanOr, DMOpcodeHandlers.BooleanOr},
            {DreamProcOpcode.PushArgumentList, DMOpcodeHandlers.PushArgumentList},
            {DreamProcOpcode.CompareGreaterThanOrEqual, DMOpcodeHandlers.CompareGreaterThanOrEqual},
            {DreamProcOpcode.SwitchCase, DMOpcodeHandlers.SwitchCase},
            {DreamProcOpcode.Mask, DMOpcodeHandlers.Mask},
            {DreamProcOpcode.Error, DMOpcodeHandlers.Error},
            {DreamProcOpcode.IsInList, DMOpcodeHandlers.IsInList},
            {DreamProcOpcode.PushArguments, DMOpcodeHandlers.PushArguments},
            {DreamProcOpcode.PushFloat, DMOpcodeHandlers.PushFloat},
            {DreamProcOpcode.ModulusReference, DMOpcodeHandlers.ModulusReference},
            {DreamProcOpcode.CreateListEnumerator, DMOpcodeHandlers.CreateListEnumerator},
            {DreamProcOpcode.Enumerate, DMOpcodeHandlers.Enumerate},
            {DreamProcOpcode.DestroyEnumerator, DMOpcodeHandlers.DestroyEnumerator},
            {DreamProcOpcode.Browse, DMOpcodeHandlers.Browse},
            {DreamProcOpcode.BrowseResource, DMOpcodeHandlers.BrowseResource},
            {DreamProcOpcode.OutputControl, DMOpcodeHandlers.OutputControl},
            {DreamProcOpcode.BitShiftRight, DMOpcodeHandlers.BitShiftRight},
            {DreamProcOpcode.Power, DMOpcodeHandlers.Power},
            {DreamProcOpcode.DebugSource, DMOpcodeHandlers.DebugSource},
            {DreamProcOpcode.DebugLine, DMOpcodeHandlers.DebugLine},
            {DreamProcOpcode.Prompt, DMOpcodeHandlers.Prompt},
            {DreamProcOpcode.PushProcArguments, DMOpcodeHandlers.PushProcArguments},
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
            {DreamProcOpcode.CreateFilteredListEnumerator,DMOpcodeHandlers.CreateFilteredListEnumerator},

        };
        #endregion
        private static OpcodeHandler?[] _opcodeHandlers = {};
        public readonly IDreamManager DreamManager = IoCManager.Resolve<IDreamManager>();
        public readonly IDreamDebugManager DebugManager = IoCManager.Resolve<IDreamDebugManager>();

        /// <summary> This stores our 'src' value. May be null!</summary>
        public DreamObject? Instance;
        public readonly DreamObject? Usr;
        public readonly int ArgumentCount;
        public string? CurrentSource;
        public int CurrentLine;
        private Stack<IEnumerator<DreamValue>>? _enumeratorStack;
        public Stack<IEnumerator<DreamValue>> EnumeratorStack => _enumeratorStack ??= new Stack<IEnumerator<DreamValue>>(1);

        private int _pc = 0;
        private int? _subOpcode;
        private DMReference? _subOpcodeRef;
        // Contains both arguments (at index 0) and local vars (at index ArgumentCount)
        private readonly DreamValue[] _localVariables;

        private readonly DMProc _proc;
        public override DreamProc Proc => _proc;

        public override (string?, int?) SourceLine => (CurrentSource, CurrentLine);


        /// Static initialiser for maintainer friendly OpcodeHandlers to performance friendly _opcodeHandlers
        static DMProcState()
        {
            int maxOpcode = 0;
            foreach(DreamProcOpcode dpo in OpcodeHandlers.Keys)
            {
                if(maxOpcode < (int) dpo)
                    maxOpcode = (int) dpo;
            }
            _opcodeHandlers = new OpcodeHandler?[maxOpcode+1];
            foreach(DreamProcOpcode dpo in OpcodeHandlers.Keys)
            {
                _opcodeHandlers[(int) dpo] = OpcodeHandlers[dpo];
            }
        }

        /// <param name="instance">This is our 'src'.</param>
        /// <exception cref="Exception">Thrown, at time of writing, when an invalid named arg is given</exception>
        public DMProcState(DMProc proc, DreamThread thread, int maxStackSize, DreamObject? instance, DreamObject? usr, DreamProcArguments arguments)
            : base(thread)
        {
            _proc = proc;
            _stack = _stackPool.Rent(maxStackSize);
            Instance = instance;
            Usr = usr;
            ArgumentCount = Math.Max(arguments.ArgumentCount, proc.ArgumentNames?.Count ?? 0);
            _localVariables = _dreamValuePool.Rent(256);
            CurrentSource = proc.Source;
            CurrentLine = proc.Line;
            WaitFor = _proc != null ? (_proc.Attributes & ProcAttributes.DisableWaitfor) != ProcAttributes.DisableWaitfor : true;

            //TODO: Positional arguments must precede all named arguments, this needs to be enforced somehow
            //Positional arguments
            for (int i = 0; i < ArgumentCount; i++) {
                _localVariables[i] = (i < arguments.OrderedArguments.Count) ? arguments.OrderedArguments[i] : DreamValue.Null;
            }

            //Named arguments
            foreach ((string argumentName, DreamValue argumentValue) in arguments.NamedArguments) {
                int argumentIndex = proc.ArgumentNames?.IndexOf(argumentName) ?? -1;
                if (argumentIndex == -1) {
                    throw new Exception($"Invalid argument name \"{argumentName}\"");
                }

                _localVariables[argumentIndex] = argumentValue;
            }
        }

        public DMProcState(DMProcState other, DreamThread thread)
            : base(thread)
        {
            if (other.EnumeratorStack.Count > 0) {
                throw new NotImplementedException();
            }

            _proc = other._proc;
            Instance = other.Instance;
            Usr = other.Usr;
            ArgumentCount = other.ArgumentCount;
            CurrentSource = other.CurrentSource;
            CurrentLine = other.CurrentLine;
            _pc = other._pc;

            _stack = _stackPool.Rent(other._stack.Length);
            Array.Copy(other._stack, _stack, _stack.Length);

            _localVariables = _dreamValuePool.Rent(256);
            Array.Copy(other._localVariables, _localVariables, 256);

            WaitFor = other.WaitFor;
        }

        protected override ProcStatus InternalResume() {
            if (Instance is not null && Instance.Deleted) {
                ReturnPools();
                return ProcStatus.Returned;
            }

            if (_pc == 0) {
                DebugManager.HandleProcStart(this);
            }

            while (_pc < _proc.Bytecode.Length) {
                int opcode = -1;
                if(_subOpcode == null)
                    opcode = _proc.Bytecode[_pc++];
                else
                {
                    opcode = _subOpcode.Value;
                    _subOpcode = null;
                }
                var handler = opcode < _opcodeHandlers.Length ? _opcodeHandlers[opcode] : null;
                if (handler is null)
                    throw new Exception($"Attempted to call non-existent Opcode method for opcode 0x{opcode:X2}");
                ProcStatus? status = handler.Invoke(this);
                if (status != null) {
                    if (status == ProcStatus.Returned || status == ProcStatus.Cancelled) {
                        // TODO: This should be automatic (dispose pattern?)
                        ReturnPools();
                    }

                    return status.Value;
                }
            }

            // TODO: This should be automatic (dispose pattern?)
            ReturnPools();
            return ProcStatus.Returned;
        }

        public void SetSubOpcode(DreamProcOpcode subOp, DMReference? subReference)
        {
            if(_subOpcode != null)
                throw new Exception("Attempted to set a subOpcode when one already exists. This is extremely likely to corrupt the stack.");
            _subOpcode = (int)subOp;
            _subOpcodeRef = subReference;
        }

        public override void ReturnedInto(DreamValue value)
        {
            Push(value);
        }

        public override void AppendStackFrame(StringBuilder builder)
        {
            if (Proc.OwningType != DreamPath.Root) {
                builder.Append(Proc.OwningType.ToString());
                builder.Append('/');
            }

            builder.Append(Proc.Name);
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
            var thread = new DreamThread(this.Proc.ToString());

            var state = new DMProcState(this, thread);
            thread.PushProcState(state);

            return thread;
        }

        public void ReturnPools()
        {
            _dreamValuePool.Return(_localVariables, true);
            _stackPool.Return(_stack);
        }

        public Span<DreamValue> GetArguments() {
            return _localVariables.AsSpan(0, ArgumentCount);
        }

        #region Stack
        private DreamValue[] _stack;
        private int _stackIndex = 0;

        public void Push(DreamValue value) {
            _stack[_stackIndex++] = value;
        }

        public void Push(DreamProcArguments value) {
            _stack[_stackIndex++] = new DreamValue(value);
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

        public DreamProcArguments PopArguments() {
            return (DreamProcArguments)(Pop().Value);
        }
        #endregion

        #region Operands
        public int ReadByte() {
            return _proc.Bytecode[_pc++];
        }

        public int ReadInt() {
            int value = BitConverter.ToInt32(_proc.Bytecode, _pc);
            _pc += 4;

            return value;
        }

        public float ReadFloat() {
            float value = BitConverter.ToSingle(_proc.Bytecode, _pc);
            _pc += 4;

            return value;
        }

        public string ReadString() {
            int stringID = ReadInt();

            return DreamManager.ObjectTree.Strings[stringID];
        }

        public DMReference ReadReference() {
            if(_subOpcodeRef != null)
            {
                DMReference result = _subOpcodeRef.Value;
                _subOpcodeRef = null;
                return result;
            }

            DMReference.Type refType = (DMReference.Type)ReadByte();

            switch (refType) {
                case DMReference.Type.Argument: return DMReference.CreateArgument(ReadByte());
                case DMReference.Type.Local: return DMReference.CreateLocal(ReadByte());
                case DMReference.Type.Global: return DMReference.CreateGlobal(ReadInt());
                case DMReference.Type.GlobalProc: return DMReference.CreateGlobalProc(ReadInt());
                case DMReference.Type.Field: return DMReference.CreateField(ReadString());
                case DMReference.Type.SrcField: return DMReference.CreateSrcField(ReadString());
                case DMReference.Type.Proc: return DMReference.CreateProc(ReadString());
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
        #endregion

        #region References
        public bool IsNullDereference(DMReference reference) {
            switch (reference.RefType) {
                case DMReference.Type.Proc:
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

        public ProcStatus? AssignReference(DMReference reference, DreamValue value) {
            switch (reference.RefType) {
                case DMReference.Type.Self: Result = value; break;
                case DMReference.Type.Argument: _localVariables[reference.Index] = value; break;
                case DMReference.Type.Local: _localVariables[ArgumentCount + reference.Index] = value; break;
                case DMReference.Type.SrcField: Instance.SetVariable(reference.Name, value); break;
                case DMReference.Type.Global: DreamManager.Globals[reference.Index] = value; break;
                case DMReference.Type.Src:
                    //TODO: src can be assigned to non-DreamObject values
                    if (!value.TryGetValueAsDreamObject(out Instance)) {
                        throw new Exception($"Cannot assign src to {value}");
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
                    if (indexing.TryGetValueAsDreamObject(out var dreamObject)) {
                        IDreamMetaObject? metaObject = dreamObject?.ObjectDefinition?.MetaObject;
                        return metaObject!.OperatorIndexAssign(indexing, index, value, this);
                    } else {
                        throw new Exception($"Cannot assign to index {index} of {indexing}");
                    }
                }
                default: throw new Exception($"Cannot assign to reference type {reference.RefType}");
            }
            return null;
        }

        public DreamValue GetReferenceValue(DMReference reference, bool peek = false) {
            switch (reference.RefType) {
                case DMReference.Type.Src: return new(Instance);
                case DMReference.Type.Usr: return new(Usr);
                case DMReference.Type.Self: return Result;
                case DMReference.Type.Global: return DreamManager.Globals[reference.Index];
                case DMReference.Type.Argument: return _localVariables[reference.Index];
                case DMReference.Type.Local: return _localVariables[ArgumentCount + reference.Index];
                case DMReference.Type.Args: {
                    DreamList argsList = DreamList.Create(ArgumentCount);

                    for (int i = 0; i < ArgumentCount; i++) {
                        argsList.AddValue(_localVariables[i]);
                    }

                    argsList.ValueAssigned += (DreamList argsList, DreamValue key, DreamValue value) => {
                        if (!key.TryGetValueAsInteger(out int argIndex)) {
                            throw new Exception($"Cannot index args with {key}");
                        }

                        if (argIndex > ArgumentCount) {
                            throw new Exception($"Args index {argIndex} is too large");
                        }

                        _localVariables[argIndex - 1] = value;
                    };

                    return new(argsList);
                }
                case DMReference.Type.Field: {
                    DreamValue owner = peek ? Peek() : Pop();
                    if (!owner.TryGetValueAsDreamObject(out var ownerObj) || ownerObj == null)
                        throw new Exception($"Cannot get field \"{reference.Name}\" from {owner}");
                    if (!ownerObj.TryGetVariable(reference.Name, out var fieldValue))
                        throw new Exception($"Type {ownerObj.ObjectDefinition.Type} has no field called \"{reference.Name}\"");

                    return fieldValue;
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

                    if (indexing.TryGetValueAsString(out string? strValue)) {
                        if (!index.TryGetValueAsInteger(out int strIndex))
                            throw new Exception($"Attempted to index string with {index}");

                        char c = strValue[strIndex - 1];
                        return new DreamValue(Convert.ToString(c));
                    }

                    if (indexing.TryGetValueAsDreamObject(out var dreamObject)) {
                        IDreamMetaObject? metaObject = dreamObject?.ObjectDefinition?.MetaObject;
                        if (metaObject != null)
                        {
                            ProcStatus? opStatus = metaObject.OperatorIndex(indexing, index, this);
                            switch(opStatus){
                                case(null):
                                case(ProcStatus.Returned):
                                    return this.Pop();
                                case(ProcStatus.Called):
                                    return this.Thread.Resume();
                                case(ProcStatus.Deferred):
                                    throw new Exception("Using sleep() in an operator overload is not supported.");
                                case(ProcStatus.Cancelled):
                                    throw new Exception("Runtime occurred in operator");
                            }
                        }
                    }

                    throw new Exception($"Cannot get index {index} of {indexing}");
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
        #endregion References

        public IEnumerable<(string, DreamValue)> InspectLocals() {
            for (int i = 0; i < _localVariables.Length; ++i) {
                string name = i.ToString();
                if (Proc.ArgumentNames != null && i < Proc.ArgumentNames.Count) {
                    name = Proc.ArgumentNames[i];
                }
                DreamValue value = _localVariables[i];
                yield return (name, value);
            }
        }
    }
}
