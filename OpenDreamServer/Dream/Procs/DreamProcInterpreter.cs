using OpenDreamServer.Dream.Objects;
using System;
using System.Buffers;
using System.Collections.Generic;

namespace OpenDreamServer.Dream.Procs {
    delegate void InterpreterOpcode(DreamProcInterpreter interpreter);

    class DreamProcInterpreter {
        private static ArrayPool<DreamValue> _dreamValuePool = ArrayPool<DreamValue>.Shared;
        
        //In the same order as the DreamProcOpcode enum
        private static InterpreterOpcode[] _opcodeHandlers = new InterpreterOpcode[] {
            null, //0x0
            DreamProcInterpreterOpcodes.BitShiftLeft,
            DreamProcInterpreterOpcodes.GetIdentifier,
            DreamProcInterpreterOpcodes.PushString,
            DreamProcInterpreterOpcodes.FormatString,
            DreamProcInterpreterOpcodes.PushInt,
            DreamProcInterpreterOpcodes.SetLocalVariable,
            DreamProcInterpreterOpcodes.PushPath,
            DreamProcInterpreterOpcodes.Add,
            DreamProcInterpreterOpcodes.Assign,
            DreamProcInterpreterOpcodes.Call,
            DreamProcInterpreterOpcodes.Dereference,
            DreamProcInterpreterOpcodes.JumpIfFalse,
            DreamProcInterpreterOpcodes.JumpIfTrue,
            DreamProcInterpreterOpcodes.Jump,
            DreamProcInterpreterOpcodes.CompareEquals,
            DreamProcInterpreterOpcodes.Return,
            DreamProcInterpreterOpcodes.PushNull,
            DreamProcInterpreterOpcodes.Subtract,
            DreamProcInterpreterOpcodes.CompareLessThan,
            DreamProcInterpreterOpcodes.CompareGreaterThan,
            DreamProcInterpreterOpcodes.BooleanAnd,
            DreamProcInterpreterOpcodes.BooleanNot,
            DreamProcInterpreterOpcodes.PushSuperProc,
            DreamProcInterpreterOpcodes.Negate,
            DreamProcInterpreterOpcodes.Modulus,
            DreamProcInterpreterOpcodes.Append,
            DreamProcInterpreterOpcodes.CreateRangeEnumerator,
            DreamProcInterpreterOpcodes.PushUsr,
            DreamProcInterpreterOpcodes.CompareLessThanOrEqual,
            DreamProcInterpreterOpcodes.IndexList,
            DreamProcInterpreterOpcodes.Remove,
            DreamProcInterpreterOpcodes.DeleteObject,
            DreamProcInterpreterOpcodes.PushResource,
            DreamProcInterpreterOpcodes.CreateList,
            DreamProcInterpreterOpcodes.CallStatement,
            DreamProcInterpreterOpcodes.BitAnd,
            DreamProcInterpreterOpcodes.CompareNotEquals,
            DreamProcInterpreterOpcodes.ListAppend,
            DreamProcInterpreterOpcodes.Divide,
            DreamProcInterpreterOpcodes.Multiply,
            DreamProcInterpreterOpcodes.PushSelf,
            DreamProcInterpreterOpcodes.BitXor,
            DreamProcInterpreterOpcodes.BitOr,
            DreamProcInterpreterOpcodes.BitNot,
            DreamProcInterpreterOpcodes.Combine,
            DreamProcInterpreterOpcodes.CreateObject,
            DreamProcInterpreterOpcodes.BooleanOr,
            DreamProcInterpreterOpcodes.PushArgumentList,
            DreamProcInterpreterOpcodes.CompareGreaterThanOrEqual,
            DreamProcInterpreterOpcodes.SwitchCase,
            DreamProcInterpreterOpcodes.Mask,
            DreamProcInterpreterOpcodes.ListAppendAssociated,
            DreamProcInterpreterOpcodes.Error,
            DreamProcInterpreterOpcodes.IsInList,
            DreamProcInterpreterOpcodes.PushArguments,
            DreamProcInterpreterOpcodes.PushFloat,
            DreamProcInterpreterOpcodes.PushSrc,
            DreamProcInterpreterOpcodes.CreateListEnumerator,
            DreamProcInterpreterOpcodes.Enumerate,
            DreamProcInterpreterOpcodes.DestroyEnumerator,
            DreamProcInterpreterOpcodes.Browse,
            DreamProcInterpreterOpcodes.BrowseResource,
            DreamProcInterpreterOpcodes.OutputControl,
            DreamProcInterpreterOpcodes.BitShiftRight,
            DreamProcInterpreterOpcodes.PushLocalVariable,
            DreamProcInterpreterOpcodes.Power,
            DreamProcInterpreterOpcodes.DereferenceProc,
            DreamProcInterpreterOpcodes.GetProc,
            DreamProcInterpreterOpcodes.Prompt,
            DreamProcInterpreterOpcodes.PushProcArguments,
            DreamProcInterpreterOpcodes.Initial,
            DreamProcInterpreterOpcodes.CallSelf,
            DreamProcInterpreterOpcodes.IsType,
            DreamProcInterpreterOpcodes.LocateCoord,
            DreamProcInterpreterOpcodes.Locate,
            DreamProcInterpreterOpcodes.IsNull,
            DreamProcInterpreterOpcodes.Spawn
        };

        public DreamProc_Old SelfProc;
        public DreamObject Instance, Usr;
        public DreamProcArguments Arguments;
        public DreamValue DefaultReturnValue = DreamValue.Null;
        public DreamValue[] LocalVariables;
        public Stack<IEnumerator<DreamValue>> EnumeratorStack = new();

        private byte[] _bytecode;
        private int _pc;
        private Stack<object> _stack = new();

        public DreamProcInterpreter(DreamProc_Old selfProc, DreamObject instance, DreamObject usr, DreamProcArguments arguments, byte[] bytecode) {
            SelfProc = selfProc;
            Instance = instance;
            Usr = usr;
            Arguments = arguments;
            _bytecode = bytecode;
        }

        private DreamProcInterpreter(DreamProcInterpreter copyFrom) {
            SelfProc = copyFrom.SelfProc;
            Instance = copyFrom.Instance;
            Usr = copyFrom.Usr;
            Arguments = copyFrom.Arguments;
            DefaultReturnValue = copyFrom.DefaultReturnValue;
            Arguments = copyFrom.Arguments;
            LocalVariables = _dreamValuePool.Rent(256);
            _bytecode = copyFrom._bytecode;
            _pc = copyFrom._pc;
            _stack = new Stack<object>(copyFrom._stack);

            Array.Copy(copyFrom.LocalVariables, LocalVariables, 256);
        }

        public DreamValue Run() {
            if (LocalVariables == null) {
                LocalVariables = _dreamValuePool.Rent(256);

                for (int i = 0; i < SelfProc.ArgumentNames.Count; i++) {
                    string argumentName = SelfProc.ArgumentNames[i];

                    if (Arguments.NamedArguments.TryGetValue(argumentName, out DreamValue argumentValue)) {
                        LocalVariables[i] = argumentValue;
                    } else if (i < Arguments.OrderedArguments.Count) {
                        LocalVariables[i] = Arguments.OrderedArguments[i];
                    } else {
                        LocalVariables[i] = DreamValue.Null;
                    }
                }
            }

            while (_pc < _bytecode.Length) {
                int opcode = _bytecode[_pc++];

                _opcodeHandlers[opcode].Invoke(this);
            }

            _dreamValuePool.Return(LocalVariables, true);
            return DefaultReturnValue;
        }

        public DreamProcInterpreter Copy() {
            return new DreamProcInterpreter(this);
        }

        public void JumpTo(int position) {
            _pc = position;
        }

        public void End() {
            _pc = _bytecode.Length; //End the proc by moving to the end
        }

        public string ReadString() {
            int stringID = ReadInt();

            return Program.CompiledJson.Strings[stringID];
        }

        public int ReadByte() {
            return _bytecode[_pc++];
        }

        public int ReadInt() {
            int value = BitConverter.ToInt32(_bytecode, _pc);
            _pc += 4;

            return value;
        }

        public float ReadFloat() {
            float value = BitConverter.ToSingle(_bytecode, _pc);
            _pc += 4;

            return value;
        }

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

        public DreamValue RunProc(DreamProc_Old proc, DreamObject instance, DreamProcArguments arguments) {
            return proc.Run(instance, arguments, Usr);
        }
    }
}
