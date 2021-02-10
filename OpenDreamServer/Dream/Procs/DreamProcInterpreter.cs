using OpenDreamServer.Dream.Objects;
using OpenDreamShared.Dream.Procs;
using System;
using System.Collections.Generic;
using System.IO;

namespace OpenDreamServer.Dream.Procs {
    delegate void InterpreterOpcode(DreamProcInterpreter interpreter);

    class DreamProcInterpreter {
        public DreamValue DefaultReturnValue = new DreamValue((DreamObject)null);
        public DreamProcArguments Arguments;
        public DreamProcScope TopScope;
        public DreamProcScope CurrentScope = null;
        public Dictionary<int, DreamValue> LocalVariables = new();
        public Stack<DreamProcListEnumerator> ListEnumeratorStack = new();
        public DreamProc SelfProc;

        private static Dictionary<DreamProcOpcode, InterpreterOpcode> _opcodeHandlers = new() {
            { DreamProcOpcode.Add, DreamProcInterpreterOpcodes.Add },
            { DreamProcOpcode.Append, DreamProcInterpreterOpcodes.Append },
            { DreamProcOpcode.Assign, DreamProcInterpreterOpcodes.Assign },
            { DreamProcOpcode.BitAnd, DreamProcInterpreterOpcodes.BitAnd },
            { DreamProcOpcode.BitNot, DreamProcInterpreterOpcodes.BitNot },
            { DreamProcOpcode.BitOr, DreamProcInterpreterOpcodes.BitOr },
            { DreamProcOpcode.BitShiftLeft, DreamProcInterpreterOpcodes.BitShiftLeft },
            { DreamProcOpcode.BitShiftRight, DreamProcInterpreterOpcodes.BitShiftRight },
            { DreamProcOpcode.BitXor, DreamProcInterpreterOpcodes.BitXor },
            { DreamProcOpcode.BooleanAnd, DreamProcInterpreterOpcodes.BooleanAnd },
            { DreamProcOpcode.BooleanNot, DreamProcInterpreterOpcodes.BooleanNot },
            { DreamProcOpcode.BooleanOr, DreamProcInterpreterOpcodes.BooleanOr },
            { DreamProcOpcode.Browse, DreamProcInterpreterOpcodes.Browse },
            { DreamProcOpcode.BrowseResource, DreamProcInterpreterOpcodes.BrowseResource },
            { DreamProcOpcode.Call, DreamProcInterpreterOpcodes.Call },
            { DreamProcOpcode.CallSelf, DreamProcInterpreterOpcodes.CallSelf },
            { DreamProcOpcode.CallStatement, DreamProcInterpreterOpcodes.CallStatement },
            { DreamProcOpcode.Combine, DreamProcInterpreterOpcodes.Combine },
            { DreamProcOpcode.CompareEquals, DreamProcInterpreterOpcodes.CompareEquals },
            { DreamProcOpcode.CompareGreaterThan, DreamProcInterpreterOpcodes.CompareGreaterThan },
            { DreamProcOpcode.CompareGreaterThanOrEqual, DreamProcInterpreterOpcodes.CompareGreaterThanOrEqual },
            { DreamProcOpcode.CompareLessThan, DreamProcInterpreterOpcodes.CompareLessThan },
            { DreamProcOpcode.CompareLessThanOrEqual, DreamProcInterpreterOpcodes.CompareLessThanOrEqual },
            { DreamProcOpcode.CompareNotEquals, DreamProcInterpreterOpcodes.CompareNotEquals },
            { DreamProcOpcode.CreateList, DreamProcInterpreterOpcodes.CreateList },
            { DreamProcOpcode.CreateListEnumerator, DreamProcInterpreterOpcodes.CreateListEnumerator },
            { DreamProcOpcode.CreateObject, DreamProcInterpreterOpcodes.CreateObject },
            { DreamProcOpcode.CreateScope, DreamProcInterpreterOpcodes.CreateScope },
            { DreamProcOpcode.DeleteObject, DreamProcInterpreterOpcodes.DeleteObject },
            { DreamProcOpcode.Dereference, DreamProcInterpreterOpcodes.Dereference },
            { DreamProcOpcode.DereferenceProc, DreamProcInterpreterOpcodes.DereferenceProc },
            { DreamProcOpcode.DestroyListEnumerator, DreamProcInterpreterOpcodes.DestroyListEnumerator },
            { DreamProcOpcode.DestroyScope, DreamProcInterpreterOpcodes.DestroyScope },
            { DreamProcOpcode.Divide, DreamProcInterpreterOpcodes.Divide },
            { DreamProcOpcode.EnumerateList, DreamProcInterpreterOpcodes.EnumerateList },
            { DreamProcOpcode.Error, DreamProcInterpreterOpcodes.Error },
            { DreamProcOpcode.FormatString, DreamProcInterpreterOpcodes.FormatString },
            { DreamProcOpcode.GetIdentifier, DreamProcInterpreterOpcodes.GetIdentifier },
            { DreamProcOpcode.GetLocalVariable, DreamProcInterpreterOpcodes.GetLocalVariable },
            { DreamProcOpcode.GetProc, DreamProcInterpreterOpcodes.GetProc },
            { DreamProcOpcode.IndexList, DreamProcInterpreterOpcodes.IndexList },
            { DreamProcOpcode.Initial, DreamProcInterpreterOpcodes.Initial },
            { DreamProcOpcode.IsInList, DreamProcInterpreterOpcodes.IsInList },
            { DreamProcOpcode.Jump, DreamProcInterpreterOpcodes.Jump },
            { DreamProcOpcode.JumpIfFalse, DreamProcInterpreterOpcodes.JumpIfFalse },
            { DreamProcOpcode.JumpIfTrue, DreamProcInterpreterOpcodes.JumpIfTrue },
            { DreamProcOpcode.ListAppend, DreamProcInterpreterOpcodes.ListAppend },
            { DreamProcOpcode.ListAppendAssociated, DreamProcInterpreterOpcodes.ListAppendAssociated },
            { DreamProcOpcode.Mask, DreamProcInterpreterOpcodes.Mask },
            { DreamProcOpcode.Modulus, DreamProcInterpreterOpcodes.Modulus },
            { DreamProcOpcode.Multiply, DreamProcInterpreterOpcodes.Multiply },
            { DreamProcOpcode.Negate, DreamProcInterpreterOpcodes.Negate },
            { DreamProcOpcode.OutputControl, DreamProcInterpreterOpcodes.OutputControl },
            { DreamProcOpcode.Power, DreamProcInterpreterOpcodes.Power },
            { DreamProcOpcode.Prompt, DreamProcInterpreterOpcodes.Prompt },
            { DreamProcOpcode.PushArgumentList, DreamProcInterpreterOpcodes.PushArgumentList },
            { DreamProcOpcode.PushArguments, DreamProcInterpreterOpcodes.PushArguments },
            { DreamProcOpcode.PushFloat, DreamProcInterpreterOpcodes.PushFloat },
            { DreamProcOpcode.PushInt, DreamProcInterpreterOpcodes.PushInt },
            { DreamProcOpcode.PushNull, DreamProcInterpreterOpcodes.PushNull },
            { DreamProcOpcode.PushPath, DreamProcInterpreterOpcodes.PushPath },
            { DreamProcOpcode.PushProcArguments, DreamProcInterpreterOpcodes.PushProcArguments },
            { DreamProcOpcode.PushResource, DreamProcInterpreterOpcodes.PushResource },
            { DreamProcOpcode.PushSelf, DreamProcInterpreterOpcodes.PushSelf },
            { DreamProcOpcode.PushSrc, DreamProcInterpreterOpcodes.PushSrc },
            { DreamProcOpcode.PushString, DreamProcInterpreterOpcodes.PushString },
            { DreamProcOpcode.PushSuperProc, DreamProcInterpreterOpcodes.PushSuperProc },
            { DreamProcOpcode.Remove, DreamProcInterpreterOpcodes.Remove },
            { DreamProcOpcode.SetLocalVariable, DreamProcInterpreterOpcodes.SetLocalVariable },
            { DreamProcOpcode.Subtract, DreamProcInterpreterOpcodes.Subtract },
            { DreamProcOpcode.SwitchCase, DreamProcInterpreterOpcodes.SwitchCase }
        };

        private MemoryStream _bytecodeStream;
        private BinaryReader _binaryReader;
        private Stack<object> _stack = new();
        private Stack<DreamProcScope> _scopeStack = new();

        public DreamProcInterpreter(DreamProc selfProc, byte[] bytecode) {
            _bytecodeStream = new MemoryStream(bytecode);
            _binaryReader = new BinaryReader(_bytecodeStream);
            SelfProc = selfProc;
        }

        public DreamValue Run(DreamProcScope scope, DreamProcArguments arguments) {
            Arguments = arguments;
            TopScope = scope;

            PushScope(scope);
            while (_bytecodeStream.Position < _bytecodeStream.Length) {
                DreamProcOpcode opcode = (DreamProcOpcode)_bytecodeStream.ReadByte();

                if (opcode == DreamProcOpcode.Return) {
                    DefaultReturnValue = PopDreamValue();

                    break;
                } else {
                    InterpreterOpcode opcodeHandler = _opcodeHandlers[opcode];

                    opcodeHandler.Invoke(this);
                }
            }

            return DefaultReturnValue;
        }

        public void SeekTo(int position) {
            _bytecodeStream.Seek(position, SeekOrigin.Begin);
        }

        public string ReadString() {
            int stringID = ReadInt();

            return Program.CompiledJson.Strings[stringID];
        }

        public int ReadByte() {
            return _bytecodeStream.ReadByte();
        }

        public int ReadInt() {
            return _binaryReader.ReadInt32();
        }

        public float ReadFloat() {
            return _binaryReader.ReadSingle();
        }

        public void PushScope(DreamProcScope scope) {
            _scopeStack.Push(scope);
            CurrentScope = scope;
        }

        public void PopScope() {
            _scopeStack.Pop();
            CurrentScope = _scopeStack.Peek();
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

        public DreamValue RunProc(DreamProc proc, DreamObject instance, DreamProcArguments arguments) {
            return proc.Run(instance, arguments, CurrentScope.Usr);
        }
    }
}
