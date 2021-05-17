using System;
using System.Buffers;
using System.Collections.Generic;
using OpenDreamServer.Dream.Objects;
using OpenDreamShared.Dream.Procs;

namespace OpenDreamServer.Dream.Procs {
    enum ProcStatus {
        Returned,
        Deferred,
        Called,
    }

    delegate DreamValue NativeProcHandler(DreamObject src, DreamObject usr, DreamProcArguments arguments);

    abstract class DreamProc {
        public string Name { get; }

        // This is currently publically settable because the loading code doesn't know what our super is until after we are instantiated
        public DreamProc SuperProc { set; get; }

        public List<String> ArgumentNames { get; }
        public List<DMValueType> ArgumentTypes { get; }

        protected DreamProc(string name, DreamProc superProc, List<String> argumentNames, List<DMValueType> argumentTypes) {
            Name = name;
            SuperProc = superProc;
            ArgumentNames = argumentNames ?? new();
            ArgumentTypes = argumentTypes ?? new();
        }

        public abstract ProcState CreateState(ExecutionContext context, DreamObject src, DreamObject usr, DreamProcArguments arguments);

        // Execute this proc. This will behave as if the proc has `set waitfor = 0`
        public DreamValue Run(DreamObject src, DreamProcArguments arguments, DreamObject usr = null) {
            var context = new ExecutionContext();
            var state = CreateState(context, src, usr, arguments);
            context.PushProcState(state);
            return context.Resume();
        }
    }

    class NativeProc : DreamProc {
        public NativeProcHandler Func { get; }

        public NativeProc(string name, DreamProc superProc, List<String> argumentNames, List<DMValueType> argumentTypes, NativeProcHandler func)
            : base(name, superProc, argumentNames, argumentTypes)
        {
            Func = func;
        }

        public override TrivialNativeProcState CreateState(ExecutionContext context, DreamObject src, DreamObject usr, DreamProcArguments arguments)
        {
            return new TrivialNativeProcState(this, context, src, usr, arguments);
        }
    }

    class DMProc : DreamProc {
        public byte[] Bytecode { get; }

        public DMProc(string name, DreamProc superProc, List<String> argumentNames, List<DMValueType> argumentTypes, byte[] bytecode)
            : base(name, superProc, argumentNames, argumentTypes)
        {
            Bytecode = bytecode;
        }

        public override DMProcState CreateState(ExecutionContext context, DreamObject src, DreamObject usr, DreamProcArguments arguments)
        {
            return new DMProcState(this, context, src, usr, arguments);
        }
    }

    abstract class ProcState {
        public ExecutionContext Context { get; }
        public DreamValue Result { set; get; } = DreamValue.Null;
        
        public ProcState(ExecutionContext context) {
            Context = context;
        }

        public abstract DreamProc Proc { get; }
        public abstract ProcStatus Resume();

        // Most implementations won't require this, so give it a default
        public virtual void ReturnedInto(DreamValue value) {}
    }

    // Used for synchronous procs that won't need to persist any state between executions (as they only execute once.)
    class TrivialNativeProcState : ProcState
    {
        public DreamObject Src;
        public DreamObject Usr;
        public DreamProcArguments Arguments;
        
        private NativeProc _proc;
        public override DreamProc Proc => _proc;

        public TrivialNativeProcState(NativeProc proc, ExecutionContext context, DreamObject src, DreamObject usr, DreamProcArguments arguments)
            : base(context)
        {
            _proc = proc;
            Src = src;
            Usr = usr;
            Arguments = arguments;
        }

        public override ProcStatus Resume()
        {
            Result = _proc.Func.Invoke(Src, Usr, Arguments);
            return ProcStatus.Returned;
        }

        public override void ReturnedInto(DreamValue value) {
            
        }
    }

    class ExecutionContext {
        private ProcState _current; 
        private Stack<ProcState> _stack = new();

        public DreamValue Resume() {
            if (!Program.IsMainThread) {
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
            if (_current != null) {
                _stack.Push(_current);
            }
            _current = state;
        }
    }

    class DMProcState : ProcState
    {
        delegate ProcStatus? OpcodeHandler(DMProcState state);

        private static ArrayPool<DreamValue> _dreamValuePool = ArrayPool<DreamValue>.Shared;

        //In the same order as the DreamProcOpcode enum
        private static OpcodeHandler[] _opcodeHandlers = new OpcodeHandler[] {
            null, //0x0
            DMOpcodeHandlers.BitShiftLeft,
            DMOpcodeHandlers.GetIdentifier,
            DMOpcodeHandlers.PushString,
            DMOpcodeHandlers.FormatString,
            DMOpcodeHandlers.PushInt,
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
            DMOpcodeHandlers.Spawn
        };

        public readonly DreamObject Instance;
        public readonly DreamObject Usr;
        public readonly DreamProcArguments Arguments;
        public readonly DreamValue[] LocalVariables;
        public readonly Stack<IEnumerator<DreamValue>> EnumeratorStack = new();

        private int _pc = 0;

        private DMProc _proc;
        public override DreamProc Proc => _proc;

        public DMProcState(DMProc proc, ExecutionContext context, DreamObject instance, DreamObject usr, DreamProcArguments arguments)
            : base(context)
        {
            _proc = proc;
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

        public DMProcState(DMProcState other, ExecutionContext context)
            : base(context)
        {
            if (other.EnumeratorStack.Count > 0) {
                throw new NotImplementedException();
            }

            _proc = other._proc;
            Instance = other.Instance;
            Usr = other.Usr;
            Arguments = other.Arguments;
            _pc = other._pc;
            _stack = new Stack<object>(other._stack);

            LocalVariables = _dreamValuePool.Rent(256);
            Array.Copy(other.LocalVariables, LocalVariables, 256);
        }

        public override ProcStatus Resume()
        {
            while (_pc < _proc.Bytecode.Length) {
                int opcode = _proc.Bytecode[_pc++];

                var status = _opcodeHandlers[opcode].Invoke(this);

                if (status != null) {
                    if (status == ProcStatus.Returned) {
                        _dreamValuePool.Return(LocalVariables, true); // TODO: This should be automatic (dispose pattern?)
                    }
                    
                    return status.Value;
                }
            }

            _dreamValuePool.Return(LocalVariables, true); // TODO: This should be automatic (dispose pattern?)
            return ProcStatus.Returned;
        }

        public override void ReturnedInto(DreamValue value) {
            Push(value);
        }

        public void Jump(int position) {
            _pc = position;
        }

        public void SetReturn(DreamValue value) {
            Result = value;
        }

        public void Call(DreamProc proc, DreamObject src, DreamProcArguments arguments) {
            var state = proc.CreateState(Context, src, Usr, arguments);
            Context.PushProcState(state);
        }

        public ExecutionContext Spawn() {
            var context = new ExecutionContext();

            var state = new DMProcState(this, context);
            context.PushProcState(state);

            return context;
        }

        #region Stack
        private Stack<object> _stack = new();

        public void Push(DreamValue value) {
            _stack.Push(value);
        }

        public void Push(IDreamProcIdentifier value) {
            _stack.Push(value);
        }

        public void Push(DreamProcArguments value) {
            _stack.Push(value);
        }

        public IDreamProcIdentifier PopIdentifier() {
            return (IDreamProcIdentifier)_stack.Pop();
        }

        public DreamValue PopDreamValue() {
            object value = _stack.Pop();

            if (value is IDreamProcIdentifier identifier) {
                return identifier.GetValue();
            } else if (value is DreamValue dreamValue) {
                return dreamValue;
            } else {
                throw new Exception("Last object on stack was not a dream value or identifier");
            }
        }

        public DreamProcArguments PopArguments() {
            return (DreamProcArguments)_stack.Pop();
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

            return Program.CompiledJson.Strings[stringID];
        }
        #endregion
    }

}