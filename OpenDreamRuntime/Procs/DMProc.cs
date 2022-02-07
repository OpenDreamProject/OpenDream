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
            DMOpcodeHandlers.PushType,
            DMOpcodeHandlers.PushString,
            DMOpcodeHandlers.FormatString,
            DMOpcodeHandlers.SwitchCaseRange,
            DMOpcodeHandlers.PushReferenceValue,
            DMOpcodeHandlers.PushPath,
            DMOpcodeHandlers.Add,
            DMOpcodeHandlers.Assign,
            DMOpcodeHandlers.Call,
            DMOpcodeHandlers.MultiplyReference,
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
            DMOpcodeHandlers.DivideReference,
            DMOpcodeHandlers.Negate,
            DMOpcodeHandlers.Modulus,
            DMOpcodeHandlers.Append,
            DMOpcodeHandlers.CreateRangeEnumerator,
            null, //0x1C
            DMOpcodeHandlers.CompareLessThanOrEqual,
            null, //0x1E
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
            DMOpcodeHandlers.BitXorReference,
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
            DMOpcodeHandlers.ModulusReference,
            DMOpcodeHandlers.CreateListEnumerator,
            DMOpcodeHandlers.Enumerate,
            DMOpcodeHandlers.DestroyEnumerator,
            DMOpcodeHandlers.Browse,
            DMOpcodeHandlers.BrowseResource,
            DMOpcodeHandlers.OutputControl,
            DMOpcodeHandlers.BitShiftRight,
            null, //0x41
            DMOpcodeHandlers.Power,
            null, //0x43
            null, //0x44
            DMOpcodeHandlers.Prompt,
            DMOpcodeHandlers.PushProcArguments,
            DMOpcodeHandlers.Initial,
            null, //0x48
            DMOpcodeHandlers.IsType,
            DMOpcodeHandlers.LocateCoord,
            DMOpcodeHandlers.Locate,
            DMOpcodeHandlers.IsNull,
            DMOpcodeHandlers.Spawn,
            null, //0x4E
            null, //0x4F
            DMOpcodeHandlers.JumpIfNullDereference,
            DMOpcodeHandlers.Pop,
            null, //0x52
            DMOpcodeHandlers.IsSaved,
            DMOpcodeHandlers.PickUnweighted,
            DMOpcodeHandlers.PickWeighted,
            DMOpcodeHandlers.Increment,
            DMOpcodeHandlers.Decrement,
            DMOpcodeHandlers.CompareEquivalent,
            DMOpcodeHandlers.CompareNotEquivalent,
            DMOpcodeHandlers.Throw,
            DMOpcodeHandlers.IsInRange
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

        public void Push(DreamProcArguments value) {
            _stack[_stackIndex++] = new DreamValue(value);
        }

        public DreamValue Pop() {
            return _stack[--_stackIndex];
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
            DMReference.Type refType = (DMReference.Type)ReadByte();

            switch (refType) {
                case DMReference.Type.Local: return DMReference.CreateLocal(ReadByte());
                case DMReference.Type.Global: return DMReference.CreateGlobal(ReadInt());
                case DMReference.Type.Field: return DMReference.CreateField(ReadString());
                case DMReference.Type.SrcField: return DMReference.CreateSrcField(ReadString());
                case DMReference.Type.Proc: return DMReference.CreateProc(ReadString());
                case DMReference.Type.GlobalProc: return DMReference.CreateGlobalProc(ReadString());
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

        public void AssignReference(DMReference reference, DreamValue value) {
            switch (reference.RefType) {
                case DMReference.Type.Self: Result = value; break;
                case DMReference.Type.Local: LocalVariables[reference.LocalId] = value; break;
                case DMReference.Type.SrcField: Instance.SetVariable(reference.FieldName, value); break;
                case DMReference.Type.Global: DreamManager.Globals[reference.GlobalId] = value; break;
                case DMReference.Type.Field: {
                    DreamValue owner = Pop();
                    if (!owner.TryGetValueAsDreamObject(out var ownerObj) || ownerObj == null)
                        throw new Exception($"Cannot assign field \"{reference.FieldName}\" on {owner}");

                    ownerObj.SetVariable(reference.FieldName, value);
                    break;
                }
                case DMReference.Type.ListIndex: {
                    DreamValue index = Pop();
                    DreamValue list = Pop();
                    if (!list.TryGetValueAsDreamList(out var listObj))
                        throw new Exception($"Cannot assign to index {index} of {list}");

                    listObj.SetValue(index, value);
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
                case DMReference.Type.Global: return DreamManager.Globals[reference.GlobalId];
                case DMReference.Type.Local: return LocalVariables[reference.LocalId];
                case DMReference.Type.Args: {
                    DreamList argsList = Arguments.CreateDreamList();

                    argsList.ValueAssigned += (DreamList argsList, DreamValue key, DreamValue value) => {
                        switch (key.Type) {
                            case DreamValue.DreamValueType.String: {
                                string argumentName = key.GetValueAsString();

                                Arguments.NamedArguments[argumentName] = value;
                                LocalVariables[Proc.ArgumentNames.IndexOf(argumentName)] = value;
                                break;
                            }
                            case DreamValue.DreamValueType.Float: {
                                int argumentIndex = key.GetValueAsInteger() - 1;

                                Arguments.OrderedArguments[argumentIndex] = value;
                                LocalVariables[argumentIndex] = value;
                                break;
                            }
                            default:
                                throw new Exception("Invalid key used on an args list");
                        }
                    };

                    return new(argsList);
                }
                case DMReference.Type.Field: {
                    DreamValue owner = peek ? Peek() : Pop();
                    if (!owner.TryGetValueAsDreamObject(out var ownerObj) || ownerObj == null)
                        throw new Exception($"Cannot get field \"{reference.FieldName}\" from {owner}");
                    if (!ownerObj.TryGetVariable(reference.FieldName, out var fieldValue))
                        throw new Exception($"Type {ownerObj.ObjectDefinition.Type} has no field called \"{reference.FieldName}\"");

                    return fieldValue;
                }
                case DMReference.Type.SrcField: {
                    if (!Instance.TryGetVariable(reference.FieldName, out var fieldValue))
                        throw new Exception($"Type {Instance.ObjectDefinition.Type} has no field called \"{reference.FieldName}\"");

                    return fieldValue;
                }
                case DMReference.Type.ListIndex: {
                    DreamValue index = peek ? _stack[_stackIndex - 1] : Pop();
                    DreamValue list = peek ? _stack[_stackIndex - 2] : Pop();
                    if (!list.TryGetValueAsDreamList(out var listObj))
                        throw new Exception($"Cannot get index {index} of {list}");

                    return listObj.GetValue(index);
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
    }
}
