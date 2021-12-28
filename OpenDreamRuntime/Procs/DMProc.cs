using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using OpenDreamRuntime.Objects;
using OpenDreamShared.Dream.Procs;
using Robust.Shared.IoC;

namespace OpenDreamRuntime.Procs {
    class DMProc : DreamProc {
        public byte[] Bytecode { get; }

        private readonly int _maxStackSize;

        public DMProc(string name, DreamProc superProc, List<String> argumentNames, List<DMValueType> argumentTypes, byte[] bytecode, int maxStackSize, bool waitFor)
            : base(name, superProc, waitFor, argumentNames, argumentTypes)
        {
            Bytecode = bytecode;
            _maxStackSize = maxStackSize;
        }

        public override DMProcState CreateState(DreamThread thread, DreamObject src, DreamObject usr, DreamProcArguments arguments)
        {
            return new DMProcState(this, thread, _maxStackSize, src, usr, arguments);
        }
    }

    class DMProcState : ProcState
    {
        delegate ProcStatus? OpcodeHandler(DMProcState state);

        // TODO: These pools are not returned to if the proc runtimes
        private static ArrayPool<DreamValue> _dreamValuePool = ArrayPool<DreamValue>.Shared;
        private static ArrayPool<DreamValue> _stackPool = ArrayPool<DreamValue>.Shared;

        #region Opcode Handlers
        //In the same order as the DreamProcOpcode enum
        private static readonly OpcodeHandler[] _opcodeHandlers = {
            null, //0x0
            DMOpcodeHandlers.BitShiftLeft,
            DMOpcodeHandlers.GetIdentifier,
            DMOpcodeHandlers.PushString,
            DMOpcodeHandlers.FormatString,
            DMOpcodeHandlers.SwitchCaseRange,
            DMOpcodeHandlers.SetLocalVariable,
            DMOpcodeHandlers.PushPath,
            DMOpcodeHandlers.Add,
            DMOpcodeHandlers.Assign,
            DMOpcodeHandlers.Call,
            DMOpcodeHandlers.Dereference,
            DMOpcodeHandlers.JumpIfFalse,
            DMOpcodeHandlers.JumpIfTrue,
            DMOpcodeHandlers.Jump,
            DMOpcodeHandlers.CompareEquals,
            DMOpcodeHandlers.Return,
            DMOpcodeHandlers.PushNull,
            DMOpcodeHandlers.Subtract,
            DMOpcodeHandlers.CompareLessThan,
            DMOpcodeHandlers.CompareGreaterThan,
            DMOpcodeHandlers.BooleanAnd,
            DMOpcodeHandlers.BooleanNot,
            DMOpcodeHandlers.PushSuperProc,
            DMOpcodeHandlers.Negate,
            DMOpcodeHandlers.Modulus,
            DMOpcodeHandlers.Append,
            DMOpcodeHandlers.CreateRangeEnumerator,
            DMOpcodeHandlers.PushUsr,
            DMOpcodeHandlers.CompareLessThanOrEqual,
            DMOpcodeHandlers.IndexList,
            DMOpcodeHandlers.Remove,
            DMOpcodeHandlers.DeleteObject,
            DMOpcodeHandlers.PushResource,
            DMOpcodeHandlers.CreateList,
            DMOpcodeHandlers.CallStatement,
            DMOpcodeHandlers.BitAnd,
            DMOpcodeHandlers.CompareNotEquals,
            DMOpcodeHandlers.ListAppend,
            DMOpcodeHandlers.Divide,
            DMOpcodeHandlers.Multiply,
            DMOpcodeHandlers.PushSelf,
            DMOpcodeHandlers.BitXor,
            DMOpcodeHandlers.BitOr,
            DMOpcodeHandlers.BitNot,
            DMOpcodeHandlers.Combine,
            DMOpcodeHandlers.CreateObject,
            DMOpcodeHandlers.BooleanOr,
            DMOpcodeHandlers.PushArgumentList,
            DMOpcodeHandlers.CompareGreaterThanOrEqual,
            DMOpcodeHandlers.SwitchCase,
            DMOpcodeHandlers.Mask,
            DMOpcodeHandlers.ListAppendAssociated,
            DMOpcodeHandlers.Error,
            DMOpcodeHandlers.IsInList,
            DMOpcodeHandlers.PushArguments,
            DMOpcodeHandlers.PushFloat,
            DMOpcodeHandlers.PushSrc,
            DMOpcodeHandlers.CreateListEnumerator,
            DMOpcodeHandlers.Enumerate,
            DMOpcodeHandlers.DestroyEnumerator,
            DMOpcodeHandlers.Browse,
            DMOpcodeHandlers.BrowseResource,
            DMOpcodeHandlers.OutputControl,
            DMOpcodeHandlers.BitShiftRight,
            DMOpcodeHandlers.PushLocalVariable,
            DMOpcodeHandlers.Power,
            DMOpcodeHandlers.DereferenceProc,
            DMOpcodeHandlers.GetProc,
            DMOpcodeHandlers.Prompt,
            DMOpcodeHandlers.PushProcArguments,
            DMOpcodeHandlers.Initial,
            DMOpcodeHandlers.CallSelf,
            DMOpcodeHandlers.IsType,
            DMOpcodeHandlers.LocateCoord,
            DMOpcodeHandlers.Locate,
            DMOpcodeHandlers.IsNull,
            DMOpcodeHandlers.Spawn,
            DMOpcodeHandlers.DereferenceConditional,
            DMOpcodeHandlers.DereferenceProcConditional,
            DMOpcodeHandlers.JumpIfNullIdentifier,
            DMOpcodeHandlers.Pop,
            DMOpcodeHandlers.PushCopy,
            DMOpcodeHandlers.IsSaved,
            DMOpcodeHandlers.PickUnweighted,
            DMOpcodeHandlers.PickWeighted,
            DMOpcodeHandlers.Increment,
            DMOpcodeHandlers.Decrement,
            DMOpcodeHandlers.CompareEquivalent,
            DMOpcodeHandlers.CompareNotEquivalent,
            DMOpcodeHandlers.Throw,
            DMOpcodeHandlers.IsInRange,
            DMOpcodeHandlers.GetGlobal,
            DMOpcodeHandlers.IndexListConditional,
            DMOpcodeHandlers.PushType
        };
        #endregion

        public IDreamManager DreamManager = IoCManager.Resolve<IDreamManager>();
        public DreamObject Instance;
        public readonly DreamObject Usr;
        public readonly DreamProcArguments Arguments;
        public readonly DreamValue[] LocalVariables;
        public readonly Stack<IEnumerator<DreamValue>> EnumeratorStack = new();

        private int _pc = 0;

        private DMProc _proc;
        public override DreamProc Proc => _proc;

        public DMProcState(DMProc proc, DreamThread thread, int maxStackSize, DreamObject instance, DreamObject usr, DreamProcArguments arguments)
            : base(thread)
        {
            _proc = proc;
            _stack = _stackPool.Rent(maxStackSize);
            Instance = instance;
            Usr = usr;
            Arguments = arguments;
            LocalVariables = _dreamValuePool.Rent(256);

            // args -> locals
            for (int i = 0; i < proc.ArgumentNames.Count; i++) {
                string argumentName = proc.ArgumentNames[i];

                if (Arguments.NamedArguments.TryGetValue(argumentName, out DreamValue argumentValue)) {
                    LocalVariables[i] = argumentValue;
                } else if (i < Arguments.OrderedArguments.Count) {
                    LocalVariables[i] = Arguments.OrderedArguments[i];
                } else {
                    LocalVariables[i] = DreamValue.Null;
                }
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
            Arguments = other.Arguments;
            _pc = other._pc;

            _stack = _stackPool.Rent(other._stack.Length);
            Array.Copy(other._stack, _stack, _stack.Length);

            LocalVariables = _dreamValuePool.Rent(256);
            Array.Copy(other.LocalVariables, LocalVariables, 256);
        }

        protected override ProcStatus InternalResume()
        {
            while (_pc < _proc.Bytecode.Length) {
                int opcode = _proc.Bytecode[_pc++];

                var status = _opcodeHandlers[opcode].Invoke(this);

                if (status != null) {
                    if (status == ProcStatus.Returned) {
                        // TODO: This should be automatic (dispose pattern?)
                        _dreamValuePool.Return(LocalVariables, true);
                        _stackPool.Return(_stack);
                    }

                    return status.Value;
                }
            }

            // TODO: This should be automatic (dispose pattern?)
            _dreamValuePool.Return(LocalVariables, true);
            _stackPool.Return(_stack);
            return ProcStatus.Returned;
        }

        public override void ReturnedInto(DreamValue value)
        {
            Push(value);
        }

        public override void AppendStackFrame(StringBuilder builder)
        {
            builder.Append($"{Proc.Name}(...)");
        }

        public void Jump(int position) {
            _pc = position;
        }

        public void SetReturn(DreamValue value) {
            Result = value;
        }

        public void Call(DreamProc proc, DreamObject src, DreamProcArguments arguments) {
            var state = proc.CreateState(Thread, src, Usr, arguments);
            Thread.PushProcState(state);
        }

        public DreamThread Spawn() {
            var thread = new DreamThread();

            var state = new DMProcState(this, thread);
            thread.PushProcState(state);

            return thread;
        }

        #region Stack
        private DreamValue[] _stack;
        private int _stackIndex = 0;

        public void Push(DreamValue value) {
            _stack[_stackIndex++] = value;
        }

        public void Push(IDreamProcIdentifier value) {
            _stack[_stackIndex++] = new DreamValue(value);
        }

        public void Push(DreamProcArguments value) {
            _stack[_stackIndex++] = new DreamValue(value);
        }

        public void PushCopy() {
            _stack[_stackIndex] = Peek();
            _stackIndex++;
        }

        public DreamValue Pop() {
            return _stack[--_stackIndex];
        }

        public DreamValue Peek() {
            return _stack[_stackIndex - 1];
        }

        public IDreamProcIdentifier PeekIdentifier() {
            return (IDreamProcIdentifier)(Peek().Value);
        }

        public IDreamProcIdentifier PopIdentifier() {
            return (IDreamProcIdentifier)(Pop().Value);
        }

        //renamed from PopDreamValue to PopUnboxedValue to signify that Pop() gives you the raw DreamValue that may hold a reference, PopUnboxedValue will return a "formatted" version of it.
        public DreamValue PopUnboxedValue() {
            DreamValue value = Pop();
            if(value.Type == DreamValue.DreamValueType.Reference){
                if(value.Value is IDreamProcIdentifier){
                    return ((IDreamProcIdentifier)value.Value).GetValue();
                }
                throw new Exception("Last object on stack was not a dream value or identifier");
            }

            return value;
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
        #endregion
    }
}
