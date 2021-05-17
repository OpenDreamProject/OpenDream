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

    delegate DreamValue NativeFunc(DreamObject a, DreamObject b, DreamProcArguments c);

    // TODO: Interface with separate imp for DMProc and NativeProc?
    class Proc {
        public Proc SuperProc;
        public readonly string Name;
        public readonly List<string> ArgumentNames = new();
        public readonly List<DMValueType> ArgumentTypes = new();
        public readonly byte[] Bytecode;
        public readonly NativeFunc Native;

        public Proc(NativeFunc native) {
            Name = "<native proc>";
            Native = native;
        }

        public Proc(string name, byte[] bytecode) {
            Name = name;
            Bytecode = bytecode;
        }

        public Proc(string name, byte[] bytecode, List<string> argumentNames, List<DMValueType> argumentTypes)
            : this(name, bytecode)
        {
            ArgumentNames = argumentNames;
            ArgumentTypes = argumentTypes;
        }

        // Wrapper that matches legacy call style
        // TODO: Remove
        public DreamValue Run(DreamObject instance, DreamProcArguments arguments, DreamObject usr = null) {
            var context = new ExecutionContext();
            
            if (Bytecode != null) {
                context.PushNewProcState(this, instance, usr, arguments);
            }

            var value = context.Resume();
            return value ?? DreamValue.Null;
        }

        // TODO: Remove. Should be part of an interface or something
        public ProcState NewProcState(ExecutionContext context, DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            if (Bytecode != null) {
                return new DMProcState(this, context, instance, usr, arguments);
            }

            if (Native != null) {
                return new NativeProcState(this, context, instance, usr, arguments);
            }

            throw new InvalidOperationException();
        }
    }

    abstract class ProcState {
        public Proc Proc { get; }
        public ExecutionContext Context { get; }
        public DreamValue Result { set; get; } = DreamValue.Null;
        
        public ProcState(Proc proc, ExecutionContext context) {
            Proc = proc;
            Context = context;
        }

        public abstract string Name { get; }
        public abstract ProcStatus Resume();
        public abstract void ReturnedInto(DreamValue value);
    }

    class NativeProcState : ProcState
    {
        public override string Name => "<native proc>";

        public DreamObject Instance;
        public DreamObject Usr;
        public DreamProcArguments Arguments;
        

        public NativeProcState(Proc proc, ExecutionContext context, DreamObject instance, DreamObject usr, DreamProcArguments arguments)
            : base(proc, context)
        {
            Instance = instance;
            Usr = usr;
            Arguments = arguments;
        }

        public override ProcStatus Resume()
        {
            Result = new DreamValue("Hello from native");
            return ProcStatus.Returned;
        }

        public override void ReturnedInto(DreamValue value) {
            
        }
    }

    class ExecutionContext {
        private ProcState _current; 
        private Stack<ProcState> _stack = new();

        public DreamValue? Resume() {
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
                        return null;

                    // Our top-most proc just called a function
                    // This means _current has changed!
                    case ProcStatus.Called:
                        // Nothing to do. The loop will call into _current.Resume for us.
                        break;
                }
            }

            return null;
        }

        public void PushNativeProcState(NativeProcState state) {
            if (_current != null) {
                _stack.Push(_current);
            }
            _current = state;
        }

        public void PushNewProcState(Proc proc, DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            if (_current != null) {
                _stack.Push(_current);
            }
            _current = proc.NewProcState(this, instance, usr, arguments);
        }

        public void PushCopiedDMProcState(DMProcState state) {
            if (_current != null) {
                _stack.Push(_current);
            }
            _current = new DMProcState(state, this);
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

        public override string Name => Proc.Name;

        public readonly DreamObject Instance;
        public readonly DreamObject Usr;
        public readonly DreamProcArguments Arguments;
        public readonly DreamValue[] LocalVariables;
        public readonly Stack<IEnumerator<DreamValue>> EnumeratorStack = new();

        private int _pc = 0;

        public DMProcState(Proc proc, ExecutionContext context, DreamObject instance, DreamObject usr, DreamProcArguments arguments)
            : base(proc, context)
        {
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
            : base(other.Proc, context)
        {
            if (other.EnumeratorStack.Count > 0) {
                throw new NotImplementedException();
            }

            Instance = other.Instance;
            Usr = other.Usr;
            Arguments = other.Arguments;
            _pc = 0;
            _stack = new Stack<object>(other._stack);

            LocalVariables = _dreamValuePool.Rent(256);
            Array.Copy(other.LocalVariables, LocalVariables, 256);
        }

        public override ProcStatus Resume()
        {
            while (_pc < Proc.Bytecode.Length) {
                int opcode = Proc.Bytecode[_pc++];

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

        public void Call(Proc proc, DreamObject instance, DreamProcArguments arguments) {
            Context.PushNewProcState(proc, instance, Usr, arguments);
        }

        public ExecutionContext Spawn() {
            var context = new ExecutionContext();
            context.PushCopiedDMProcState(this);
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
            return Proc.Bytecode[_pc++];
        }

        public int ReadInt() {
            int value = BitConverter.ToInt32(Proc.Bytecode, _pc);
            _pc += 4;

            return value;
        }

        public float ReadFloat() {
            float value = BitConverter.ToSingle(Proc.Bytecode, _pc);
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