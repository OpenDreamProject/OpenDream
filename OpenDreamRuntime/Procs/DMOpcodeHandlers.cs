using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.MetaObjects;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;

namespace OpenDreamRuntime.Procs {
    static class DMOpcodeHandlers {
        #region Values
        public static ProcStatus? PushReferenceValue(DMProcState state) {
            DMReference reference = state.ReadReference();

            state.Push(state.GetReferenceValue(reference));
            return null;
        }

        public static ProcStatus? Assign(DMProcState state) {
            DMReference reference = state.ReadReference();
            DreamValue value = state.Pop();

            state.AssignReference(reference, value);
            state.Push(value);
            return null;
        }

        public static ProcStatus? CreateList(DMProcState state) {
            var list = DreamList.Create();

            state.Push(new DreamValue(list));
            return null;
        }

        public static ProcStatus? CreateListEnumerator(DMProcState state)
        {
            var popped = state.Pop();

            DreamList? list = null;
            if (popped.TryGetValueAsDreamObject(out var listObject))
                list = listObject as DreamList;

            if (list == null) {
                if (listObject == null) {
                    list = null;
                } else if (listObject.IsSubtypeOf(DreamPath.Atom) || listObject.IsSubtypeOf(DreamPath.World))
                {
                    list = listObject.GetVariable("contents").GetValueAsDreamList();
                } else {
                    throw new Exception("Object " + listObject + " is not a " + DreamPath.List + ", " + DreamPath.Atom + " or " + DreamPath.World);
                }
            }

            if (list == null)
            {
                state.EnumeratorStack.Push(Enumerable.Empty<DreamValue>().GetEnumerator());
            }
            else
            {
                var values = new List<DreamValue>(list.GetValues());
                state.EnumeratorStack.Push(values.GetEnumerator());
            }

            return null;
        }

        public static ProcStatus? CreateTypeEnumerator(DMProcState state)
        {
            if (!state.Pop().TryGetValueAsPath(out var type))
            {
                throw new Exception($"Cannot create a type enumerator for a non-path");
            }

            if (type == DreamPath.Client)
            {
                state.EnumeratorStack.Push(new DreamObjectEnumerator(state.DreamManager.Clients));
                return null;
            }
            if (state.DreamManager.ObjectTree.GetObjectDefinition(type).IsSubtypeOf(DreamPath.Atom))
            {
                state.EnumeratorStack.Push(new DreamValueAsObjectEnumerator(state.DreamManager.WorldContentsList.GetValues(), type));
                return null;
            }
            if (state.DreamManager.ObjectTree.GetObjectDefinition(type).IsSubtypeOf(DreamPath.Datum))
            {
                state.EnumeratorStack.Push(new DreamObjectEnumerator(state.DreamManager.Datums));
                return null;
            }

            throw new Exception($"Type enumeration of {type.ToString()} is not supported");
        }

        public static ProcStatus? CreateRangeEnumerator(DMProcState state) {
            float step = state.Pop().GetValueAsFloat();
            float rangeEnd = state.Pop().GetValueAsFloat();
            float rangeStart = state.Pop().GetValueAsFloat();

            state.EnumeratorStack.Push(new DreamProcRangeEnumerator(rangeStart, rangeEnd, step));
            return null;
        }

        public static ProcStatus? CreateObject(DMProcState state) {
            DreamProcArguments arguments = state.PopArguments();
            var val = state.Pop();
            if (!val.TryGetValueAsPath(out var objectPath))
            {
                if (val.TryGetValueAsString(out var pathString))
                {
                    objectPath = new DreamPath(pathString);
                    if (!state.DreamManager.ObjectTree.HasTreeEntry(objectPath))
                    {
                        throw new Exception($"Cannot create unknown object {val.Value}");
                    }
                }
                else
                {
                    throw new Exception("Attempted to create an object that is neither a path nor a path string");
                }

            }

            DreamObject newObject = state.DreamManager.ObjectTree.CreateObject(objectPath);
            state.Thread.PushProcState(newObject.InitProc(state.Thread, state.Usr, arguments));
            return ProcStatus.Called;
        }

        public static ProcStatus? CreateMultidimensionalList(DMProcState state)
        {
            var count = state.ReadInt();

            List<int> sizes = new List<int>(count);
            for (var i = 0; i < count; i++)
            {
                state.Pop().TryGetValueAsInteger(out var size);
                sizes.Add(size);
            }

            sizes.Reverse();
            var list = DreamList.CreateMultidimensional(sizes);

            state.Push(new DreamValue(list));
            return null;
        }

        public static ProcStatus? DestroyEnumerator(DMProcState state) {
            state.EnumeratorStack.Pop();
            return null;
        }

        public static ProcStatus? Enumerate(DMProcState state) {
            IEnumerator<DreamValue> enumerator = state.EnumeratorStack.Peek();
            DMReference reference = state.ReadReference();
            bool successfulEnumeration = enumerator.MoveNext();

            state.AssignReference(reference, enumerator.Current);
            state.Push(new DreamValue(successfulEnumeration ? 1 : 0));
            return null;
        }

        public static ProcStatus? FormatString(DMProcState state) {
            string unformattedString = state.ReadString();
            StringBuilder formattedString = new StringBuilder();

            for (int i = 0; i < unformattedString.Length; i++) {
                char c = unformattedString[i];

                if (c == (char)0xFF) {
                    c = unformattedString[++i];

                    switch ((StringFormatTypes)c) {
                        case StringFormatTypes.Stringify: {
                            DreamValue value = state.Pop();

                            formattedString.Append(value.Stringify());
                            break;
                        }
                        case StringFormatTypes.Ref: {
                            DreamObject refObject = state.Pop().GetValueAsDreamObject();

                            formattedString.Append(refObject.CreateReferenceID(state.DreamManager));
                            break;
                        }
                        default: throw new Exception("Invalid special character");
                    }
                } else {
                    formattedString.Append(c);
                }
            }

            state.Push(new DreamValue(formattedString.ToString()));
            return null;
        }

        public static ProcStatus? Initial(DMProcState state) {
            DreamValue owner = state.Pop();
            string property = state.ReadString();

            DreamObjectDefinition objectDefinition;
            if (owner.TryGetValueAsDreamObject(out DreamObject dreamObject)) {
                objectDefinition = dreamObject.ObjectDefinition;
            } else if (owner.TryGetValueAsPath(out DreamPath path)) {
                objectDefinition = state.DreamManager.ObjectTree.GetObjectDefinition(path);
            } else {
                throw new Exception("Invalid owner for initial() call " + owner);
            }

            state.Push(objectDefinition.Variables[property]);
            return null;
        }

        public static ProcStatus? IsNull(DMProcState state) {
            DreamValue value = state.Pop();

            state.Push(new DreamValue((value == DreamValue.Null) ? 1 : 0));
            return null;
        }

        public static ProcStatus? IsInList(DMProcState state) {
            DreamValue listValue = state.Pop();
            DreamValue value = state.Pop();

            if (listValue.Value != null) {
                DreamObject listObject = listValue.GetValueAsDreamObject();
                DreamList list = listObject as DreamList;

                if (list == null) {
                    if (listObject.IsSubtypeOf(DreamPath.Atom) || listObject.IsSubtypeOf(DreamPath.World)) {
                        list = listObject.GetVariable("contents").GetValueAsDreamList();
                    } else {
                        throw new Exception("Value " + listValue + " is not a " + DreamPath.List + ", " + DreamPath.Atom + " or " + DreamPath.World);
                    }
                }

                state.Push(new DreamValue(list.ContainsValue(value) ? 1 : 0));
            } else {
                state.Push(new DreamValue(0));
            }

            return null;
        }

        public static ProcStatus? ListAppend(DMProcState state) {
            DreamValue value = state.Pop();
            DreamList list = state.Pop().GetValueAsDreamList();

            list.AddValue(value);
            state.Push(new DreamValue(list));
            return null;
        }

        public static ProcStatus? ListAppendAssociated(DMProcState state) {
            DreamValue index = state.Pop();
            DreamValue value = state.Pop();
            DreamList list = state.Pop().GetValueAsDreamList();

            if (index.TryGetValueAsInteger(out var idx) && idx == list.GetLength() + 1)
            {
                list.Resize(list.GetLength() + 1);
            }

            list.SetValue(index, value);
            state.Push(new DreamValue(list));
            return null;
        }

        public static ProcStatus? Pop(DMProcState state) {
            state.Pop();
            return null;
        }

        public static ProcStatus? PushArgumentList(DMProcState state) {
            DreamProcArguments arguments = new DreamProcArguments(new(), new());
            DreamList argList = state.Pop().GetValueAsDreamList();

            if (argList != null)
            {
                foreach (DreamValue value in argList.GetValues()) {
                    if (argList.ContainsKey(value)) { //Named argument
                        if (value.TryGetValueAsString(out string name)) {
                            arguments.NamedArguments.Add(name, argList.GetValue(value));
                        } else {
                            throw new Exception("List contains a non-string key, and cannot be used as an arglist");
                        }
                    } else { //Ordered argument
                        arguments.OrderedArguments.Add(value);
                    }
                }
            }

            state.Push(arguments);
            return null;
        }

        public static ProcStatus? PushArguments(DMProcState state) {
            int argumentCount = state.ReadInt();
            int namedCount = state.ReadInt();
            int unnamedCount = argumentCount - namedCount;
            DreamProcArguments arguments = new DreamProcArguments(unnamedCount > 0 ? new List<DreamValue>(unnamedCount) : null, namedCount > 0 ? new Dictionary<string, DreamValue>(namedCount) : null);
            DreamValue[]? argumentValues = argumentCount > 0 ? new DreamValue[argumentCount] : null;

            for (int i = argumentCount - 1; i >= 0; i--) {
                argumentValues[i] = state.Pop();
            }

            for (int i = 0; i < argumentCount; i++) {
                DreamProcOpcodeParameterType argumentType = (DreamProcOpcodeParameterType)state.ReadByte();

                switch (argumentType) {
                    case DreamProcOpcodeParameterType.Named: {
                        string argumentName = state.ReadString();

                        arguments.NamedArguments[argumentName] = argumentValues[i];
                        break;
                    }
                    case DreamProcOpcodeParameterType.Unnamed:
                        arguments.OrderedArguments.Add(argumentValues[i]);
                        break;
                    default:
                        throw new Exception("Invalid argument type (" + argumentType + ")");
                }
            }

            state.Push(arguments);
            return null;
        }

        public static ProcStatus? PushFloat(DMProcState state) {
            float value = state.ReadFloat();

            state.Push(new DreamValue(value));
            return null;
        }

        public static ProcStatus? PushNull(DMProcState state) {
            state.Push(DreamValue.Null);
            return null;
        }

        public static ProcStatus? PushPath(DMProcState state) {
            DreamPath path = new DreamPath(state.ReadString());

            state.Push(new DreamValue(path));
            return null;
        }

        public static ProcStatus? PushType(DMProcState state) {
            int typeId = state.ReadInt();
            DreamPath path = state.DreamManager.ObjectTree.Types[typeId].Path;

            state.Push(new DreamValue(path));
            return null;
        }

        public static ProcStatus? PushProcArguments(DMProcState state) {
            List<DreamValue> args = new(state.Arguments.AsSpan(0, state.ArgumentCount).ToArray());

            state.Push(new DreamProcArguments(args));
            return null;
        }

        public static ProcStatus? PushResource(DMProcState state) {
            string resourcePath = state.ReadString();

            state.Push(new DreamValue(IoCManager.Resolve<DreamResourceManager>().LoadResource(resourcePath)));
            return null;
        }

        public static ProcStatus? PushString(DMProcState state) {
            state.Push(new DreamValue(state.ReadString()));
            return null;
        }
        #endregion Values

        #region Math
        public static ProcStatus? Add(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();
            DreamValue? output = null;

            if (second.Value == null) {
                output = first;
            } else if (first.Value == null) {
                output = second;
            } else switch (first.Type) {
                case DreamValue.DreamValueType.Float: {
                    float firstFloat = first.GetValueAsFloat();

                    output = second.Type switch {
                        DreamValue.DreamValueType.Float => new DreamValue(firstFloat + second.GetValueAsFloat()),
                        _ => null
                    };
                    break;
                }
                case DreamValue.DreamValueType.String when second.Type == DreamValue.DreamValueType.String:
                    output = new DreamValue(first.GetValueAsString() + second.GetValueAsString());
                    break;
                case DreamValue.DreamValueType.DreamObject: {
                    IDreamMetaObject metaObject = first.GetValueAsDreamObject().ObjectDefinition.MetaObject;

                    output = metaObject?.OperatorAdd(first, second);
                    break;
                }
            }

            if (output != null) {
                state.Push(output.Value);
            } else {
                throw new Exception("Invalid add operation on " + first + " and " + second);
            }

            return null;
        }

        public static ProcStatus? Append(DMProcState state) {
            DMReference reference = state.ReadReference();
            DreamValue second = state.Pop();
            DreamValue first = state.GetReferenceValue(reference, peek: true);

            DreamValue result;
            if (first.TryGetValueAsDreamObject(out var firstObj)) {
                if (firstObj != null) {
                    IDreamMetaObject metaObject = firstObj.ObjectDefinition.MetaObject;

                    if (metaObject != null) {
                        state.PopReference(reference);
                        state.Push(metaObject.OperatorAppend(first, second));

                        return null;
                    } else {
                        throw new Exception("Invalid append operation on " + first + " and " + second);
                    }
                } else {
                    result = second;
                }
            } else if (second.Value != null) {
                switch (first.Type) {
                    case DreamValue.DreamValueType.Float when second.Type == DreamValue.DreamValueType.Float:
                        result = new DreamValue(first.GetValueAsFloat() + second.GetValueAsFloat());
                        break;
                    case DreamValue.DreamValueType.String when second.Type == DreamValue.DreamValueType.String:
                        result = new DreamValue(first.GetValueAsString() + second.GetValueAsString());
                        break;
                    case DreamValue.DreamValueType.DreamResource when (second.Type == DreamValue.DreamValueType.String && first.TryGetValueAsDreamResource(out var rsc) &&  rsc.ResourcePath.EndsWith("dmi")):
                        // TODO icon += hexcolor is the same as Blend()
                        state.DreamManager.WriteWorldLog("Appending colors to DMIs is not implemented", LogLevel.Warning, "opendream.unimplemented");
                        result = first;
                        break;
                    default:
                        throw new Exception("Invalid append operation on " + first + " and " + second);
                }
            } else {
                result = first;
            }

            state.AssignReference(reference, result);
            state.Push(result);
            return null;
        }

        public static ProcStatus? Increment(DMProcState state) {
            DMReference reference = state.ReadReference();
            DreamValue value = state.GetReferenceValue(reference, peek: true);

            if (value.TryGetValueAsInteger(out int intValue)) {
                state.AssignReference(reference, new(intValue + 1));
            } else {
                //If it's not a number, it turns into 1
                state.AssignReference(reference, new(1));
            }

            state.Push(value);
            return null;
        }

        public static ProcStatus? Decrement(DMProcState state) {
            DMReference reference = state.ReadReference();
            DreamValue value = state.GetReferenceValue(reference, peek: true);

            if (value.TryGetValueAsInteger(out int intValue)) {
                state.AssignReference(reference, new(intValue - 1));
            } else {
                //If it's not a number, it turns into -1
                state.AssignReference(reference, new(-1));
            }

            state.Push(value);
            return null;
        }

        public static ProcStatus? BitAnd(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            if (first.TryGetValueAsDreamList(out DreamList list)) {
                DreamList newList = DreamList.Create();

                if (second.TryGetValueAsDreamList(out DreamList secondList)) {
                    int len = list.GetLength();

                    for (int i = 1; i <= len; i++) {
                        DreamValue value = list.GetValue(new DreamValue(i));

                        if (secondList.ContainsValue(value)) {
                            DreamValue associativeValue = list.GetValue(value);

                            newList.AddValue(value);
                            if (associativeValue.Value != null) newList.SetValue(value, associativeValue);
                        }
                    }
                } else {
                    int len = list.GetLength();

                    for (int i = 1; i <= len; i++) {
                        DreamValue value = list.GetValue(new DreamValue(i));

                        if (value == second) {
                            DreamValue associativeValue = list.GetValue(value);

                            newList.AddValue(value);
                            if (associativeValue.Value != null) newList.SetValue(value, associativeValue);
                        }
                    }
                }

                state.Push(new DreamValue(newList));
            } else if (first.Value != null && second.Value != null) {
                state.Push(new DreamValue(first.GetValueAsInteger() & second.GetValueAsInteger()));
            } else {
                state.Push(new DreamValue(0));
            }

            return null;
        }

        public static ProcStatus? BitNot(DMProcState state)
        {
            var input = state.Pop();
            if (input.TryGetValueAsInteger(out var value))
            {
                state.Push(new DreamValue((~value) & 0xFFFFFF));
            }
            else
            {
                if (input.TryGetValueAsDreamObjectOfType(DreamPath.Matrix, out _)) // TODO ~ on /matrix
                {
                    throw new NotImplementedException("/matrix does not support the '~' operator yet");
                }
                state.Push(new DreamValue(16777215)); // 2^24 - 1
            }

            return null;
        }

        public static ProcStatus? BitOr(DMProcState state) {                        // x | y
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            if (first.Type == DreamValue.DreamValueType.DreamObject) {              // Object | y
                if (first.Value != null) {
                    IDreamMetaObject metaObject = first.GetValueAsDreamObject().ObjectDefinition.MetaObject;

                    if (metaObject != null) {
                        state.Push(metaObject.OperatorOr(first, second));
                    } else {
                        throw new Exception("Invalid or operation on " + first + " and " + second);
                    }
                } else {
                    state.Push(DreamValue.Null);
                }
            } else if (second.Value != null) {                                      // Non-Object | y
                switch (first.Type) {
                    case DreamValue.DreamValueType.Float when second.Type == DreamValue.DreamValueType.Float:
                        state.Push(new DreamValue(first.GetValueAsInteger() | second.GetValueAsInteger()));
                        break;
                    default:
                        throw new Exception("Invalid or operation on " + first + " and " + second);
                }
            }
            return null;
        }

        public static ProcStatus? BitShiftLeft(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            //TODO: Savefiles get special treatment
            //"savefile["entry"] << ..." is the same as "savefile["entry"] = ..."

            switch (first.Type) {
                case DreamValue.DreamValueType.DreamObject: { //Output operation
                    if (first == DreamValue.Null) {
                        state.Push(new DreamValue(0));
                    } else {
                        IDreamMetaObject metaObject = first.GetValueAsDreamObject().ObjectDefinition.MetaObject;

                        state.Push(metaObject?.OperatorOutput(first, second) ?? DreamValue.Null);
                    }

                    break;
                }
                case DreamValue.DreamValueType.DreamResource:
                    first.GetValueAsDreamResource().Output(second);

                    state.Push(DreamValue.Null);
                    break;
                case DreamValue.DreamValueType.Float when second.Type == DreamValue.DreamValueType.Float:
                    state.Push(new DreamValue(first.GetValueAsInteger() << second.GetValueAsInteger()));
                    break;
                default:
                    throw new Exception("Invalid bit shift left operation on " + first + " and " + second);
            }

            return null;
        }

        public static ProcStatus? BitShiftRight(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            //TODO: Savefiles get special treatment
            //"savefile["entry"] >> ..." is the same as "... = savefile["entry"]"

            if (first == DreamValue.Null) {
                state.Push(new DreamValue(0));
            } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Float) {
                state.Push(new DreamValue(first.GetValueAsInteger() >> second.GetValueAsInteger()));
            } else {
                throw new Exception("Invalid bit shift right operation on " + first + " and " + second);
            }

            return null;
        }

        public static ProcStatus? BitXor(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            state.Push(BitXorValues(first, second));
            return null;
        }

        public static ProcStatus? BitXorReference(DMProcState state) {
            DreamValue second = state.Pop();
            DMReference reference = state.ReadReference();
            DreamValue first = state.GetReferenceValue(reference, peek: true);
            DreamValue result = BitXorValues(first, second);

            state.AssignReference(reference, result);
            state.Push(result);
            return null;
        }

        public static ProcStatus? BooleanAnd(DMProcState state) {
            DreamValue a = state.Pop();
            int jumpPosition = state.ReadInt();

            if (!a.IsTruthy()) {
                state.Push(a);
                state.Jump(jumpPosition);
            }

            return null;
        }

        public static ProcStatus? BooleanNot(DMProcState state) {
            DreamValue value = state.Pop();

            state.Push(new DreamValue(value.IsTruthy() ? 0 : 1));
            return null;
        }

        public static ProcStatus? BooleanOr(DMProcState state) {
            DreamValue a = state.Pop();
            int jumpPosition = state.ReadInt();

            if (a.IsTruthy()) {
                state.Push(a);
                state.Jump(jumpPosition);
            }
            return null;
        }

        public static ProcStatus? Combine(DMProcState state) {
            DMReference reference = state.ReadReference();
            DreamValue second = state.Pop();
            DreamValue first = state.GetReferenceValue(reference, peek: true);

            DreamValue result;
            if (first.TryGetValueAsDreamObject(out var firstObj)) {
                if (firstObj != null) {
                    IDreamMetaObject metaObject = firstObj.ObjectDefinition.MetaObject;

                    if (metaObject != null) {
                        state.PopReference(reference);
                        state.Push(metaObject.OperatorCombine(first, second));

                        return null;
                    } else {
                        throw new Exception("Invalid combine operation on " + first + " and " + second);
                    }
                } else {
                    result = second;
                }
            } else if (second.Value != null) {
                if (first.TryGetValueAsInteger(out var firstInt) && second.TryGetValueAsInteger(out var secondInt)) {
                    result = new DreamValue(firstInt | secondInt);
                } else if (first.Value == null) {
                    result = second;
                } else {
                    throw new Exception("Invalid combine operation on " + first + " and " + second);
                }
            } else {
                throw new Exception("Invalid combine operation on " + first + " and " + second);
            }


            state.AssignReference(reference, result);
            state.Push(result);
            return null;
        }

        public static ProcStatus? Divide(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            state.Push(DivideValues(first, second));
            return null;
        }

        public static ProcStatus? DivideReference(DMProcState state) {
            DMReference reference = state.ReadReference();
            DreamValue second = state.Pop();
            DreamValue first = state.GetReferenceValue(reference, peek: true);
            DreamValue result = DivideValues(first, second);

            state.AssignReference(reference, result);
            state.Push(result);
            return null;
        }

        public static ProcStatus? Mask(DMProcState state) {
            DMReference reference = state.ReadReference();
            DreamValue second = state.Pop();
            DreamValue first = state.GetReferenceValue(reference, peek: true);

            DreamValue result;
            switch (first.Type) {
                case DreamValue.DreamValueType.DreamObject when first.Value != null: {
                    IDreamMetaObject metaObject = first.GetValueAsDreamObject().ObjectDefinition.MetaObject;

                    if (metaObject != null) {
                        state.PopReference(reference);
                        state.Push(metaObject.OperatorMask(first, second));

                        return null;
                    } else {
                        throw new Exception("Invalid mask operation on " + first + " and " + second);
                    }
                }
                case DreamValue.DreamValueType.DreamObject:
                    result = new DreamValue(0);
                    break;
                case DreamValue.DreamValueType.Float when second.Type == DreamValue.DreamValueType.Float:
                    result = new DreamValue(first.GetValueAsInteger() & second.GetValueAsInteger());
                    break;
                default:
                    throw new Exception("Invalid mask operation on " + first + " and " + second);
            }

            state.AssignReference(reference, result);
            state.Push(result);
            return null;
        }

        public static ProcStatus? Modulus(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Float) {
                state.Push(new DreamValue(first.GetValueAsInteger() % second.GetValueAsInteger()));
            } else {
                throw new Exception("Invalid modulus operation on " + first + " and " + second);
            }

            return null;
        }

        public static ProcStatus? ModulusReference(DMProcState state) {
            DreamValue second = state.Pop();
            DMReference reference = state.ReadReference();
            DreamValue first = state.GetReferenceValue(reference, peek: true);
            DreamValue result = ModulusValues(first, second);

            state.AssignReference(reference, result);
            state.Push(result);
            return null;
        }

        public static ProcStatus? Multiply(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            state.Push(MultiplyValues(first, second));
            return null;
        }

        public static ProcStatus? MultiplyReference(DMProcState state) {
            DMReference reference = state.ReadReference();
            DreamValue second = state.Pop();
            DreamValue first = state.GetReferenceValue(reference, peek: true);
            DreamValue result = MultiplyValues(first, second);

            state.AssignReference(reference, result);
            state.Push(result);
            return null;
        }

        public static ProcStatus? Negate(DMProcState state) {
            DreamValue value = state.Pop();

            switch (value.Type) {
                case DreamValue.DreamValueType.Float: state.Push(new DreamValue(-value.GetValueAsFloat())); break;
                default: throw new Exception("Invalid negate operation on " + value);
            }

            return null;
        }

        public static ProcStatus? Power(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Float) {
                state.Push(new DreamValue((float)Math.Pow(first.GetValueAsFloat(), second.GetValueAsFloat())));
            } else {
                throw new Exception("Invalid power operation on " + first + " and " + second);
            }

            return null;
        }

        public static ProcStatus? Remove(DMProcState state) {
            DMReference reference = state.ReadReference();
            DreamValue second = state.Pop();
            DreamValue first = state.GetReferenceValue(reference, peek: true);

            DreamValue result;
            switch (first.Type) {
                case DreamValue.DreamValueType.DreamObject when first.Value != null: {
                    IDreamMetaObject metaObject = first.GetValueAsDreamObject().ObjectDefinition.MetaObject;

                    if (metaObject != null) {
                        state.PopReference(reference);
                        state.Push(metaObject.OperatorRemove(first, second));

                        return null;
                    } else {
                        throw new Exception("Invalid remove operation on " + first + " and " + second);
                    }
                }
                case DreamValue.DreamValueType.DreamObject when second.Type == DreamValue.DreamValueType.Float:
                    result = new DreamValue(-second.GetValueAsFloat());
                    break;
                case DreamValue.DreamValueType.Float when second.Type == DreamValue.DreamValueType.Float:
                    result = new DreamValue(first.GetValueAsFloat() - second.GetValueAsFloat());
                    break;
                default:
                    throw new Exception("Invalid remove operation on " + first + " and " + second);
            }

            state.AssignReference(reference, result);
            state.Push(result);
            return null;
        }

        public static ProcStatus? Subtract(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();
            DreamValue? output = null;

            if (second.Value == null) {
                output = first;
            } else if (first.Value == null && second.Type == DreamValue.DreamValueType.Float) {
                output = new DreamValue(-second.GetValueAsFloat());
            } else switch (first.Type) {
                case DreamValue.DreamValueType.Float: {
                    float firstFloat = first.GetValueAsFloat();

                    output = second.Type switch {
                        DreamValue.DreamValueType.Float => new DreamValue(firstFloat - second.GetValueAsFloat()),
                        _ => null
                    };
                    break;
                }
                case DreamValue.DreamValueType.DreamObject: {
                    IDreamMetaObject metaObject = first.GetValueAsDreamObject().ObjectDefinition.MetaObject;

                    if (metaObject != null) {
                        output = metaObject.OperatorSubtract(first, second);
                    }
                    break;
                }
            }

            if (output != null) {
                state.Push(output.Value);
            } else {
                throw new Exception("Invalid subtract operation on " + first + " and " + second);
            }

            return null;
        }
        #endregion Math

        #region Comparisons
        public static ProcStatus? CompareEquals(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            state.Push(new DreamValue(IsEqual(first, second) ? 1 : 0));
            return null;
        }

        public static ProcStatus? CompareEquivalent(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            state.Push(new DreamValue(IsEquivalent(first, second) ? 1 : 0));
            return null;
        }

        public static ProcStatus? CompareGreaterThan(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            state.Push(new DreamValue(IsGreaterThan(first, second) ? 1 : 0));
            return null;
        }

        public static ProcStatus? CompareGreaterThanOrEqual(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();
            DreamValue result;

            if (first.TryGetValueAsInteger(out int firstInt) && firstInt == 0 && second == DreamValue.Null) result = new DreamValue(1);
            else if (first == DreamValue.Null && second.TryGetValueAsInteger(out int secondInt) && secondInt == 0) result = new DreamValue(1);
            else result = new DreamValue((IsEqual(first, second) || IsGreaterThan(first, second)) ? 1 : 0);

            state.Push(result);
            return null;
        }

        public static ProcStatus? CompareLessThan(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            state.Push(new DreamValue(IsLessThan(first, second) ? 1 : 0));
            return null;
        }

        public static ProcStatus? CompareLessThanOrEqual(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();
            DreamValue result;

            if (first.TryGetValueAsInteger(out int firstInt) && firstInt == 0 && second == DreamValue.Null) result = new DreamValue(1);
            else if (first == DreamValue.Null && second.TryGetValueAsInteger(out int secondInt) && secondInt == 0) result = new DreamValue(1);
            else result = new DreamValue((IsEqual(first, second) || IsLessThan(first, second)) ? 1 : 0);

            state.Push(result);
            return null;
        }

        public static ProcStatus? CompareNotEquals(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            state.Push(new DreamValue(IsEqual(first, second) ? 0 : 1));
            return null;
        }

        public static ProcStatus? CompareNotEquivalent(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            state.Push(new DreamValue(IsEquivalent(first, second) ? 0 : 1));
            return null;
        }

        public static ProcStatus? IsInRange(DMProcState state)
        {
            DreamValue end = state.Pop();
            DreamValue start = state.Pop();
            DreamValue var = state.Pop();
            if (var.Type != DreamValue.DreamValueType.Float) var = new DreamValue(0f);
            if (start.Type != DreamValue.DreamValueType.Float) start = new DreamValue(0f);
            if (end.Type != DreamValue.DreamValueType.Float) end = new DreamValue(0f);
            bool inRange = (IsEqual(start, var) || IsLessThan(start, var)) && (IsEqual(var, end) || IsLessThan(var, end));
            state.Push(new DreamValue(inRange ? 1 : 0));
            return null;
        }

        public static ProcStatus? IsType(DMProcState state) {
            DreamValue typeValue = state.Pop();
            DreamValue value = state.Pop();
            DreamPath type;

            if (typeValue.TryGetValueAsDreamObject(out DreamObject typeObject)) {
                if (typeObject == null) {
                    state.Push(new DreamValue(0));
                    return null;
                }

                type = typeObject.ObjectDefinition.Type;
            } else {
                type = typeValue.GetValueAsPath();
            }

            if (value.TryGetValueAsDreamObject(out DreamObject dreamObject) && dreamObject != null) {
                state.Push(new DreamValue(dreamObject.IsSubtypeOf(type) ? 1 : 0));
            } else {
                state.Push(new DreamValue(0));
            }

            return null;
        }
        #endregion Comparisons

        #region Flow
        public static ProcStatus? Call(DMProcState state) {
            DMReference procRef = state.ReadReference();
            DreamProcArguments arguments = state.PopArguments();

            DreamObject instance;
            DreamProc proc;
            switch (procRef.RefType) {
                case DMReference.Type.Self: {
                    instance = state.Instance;
                    proc = state.Proc;
                    break;
                }
                case DMReference.Type.SuperProc: {
                    instance = state.Instance;
                    proc = state.Proc.SuperProc;

                    if (proc == null) {
                        //Attempting to call a super proc where there is none will just return null
                        state.Push(DreamValue.Null);
                        return null;
                    }

                    break;
                }
                case DMReference.Type.Proc: {
                    DreamValue owner = state.Pop();
                    if (!owner.TryGetValueAsDreamObject(out instance) || instance == null)
                        throw new Exception($"Cannot dereference proc \"{procRef.Name}\" from {owner}");
                    if (!instance.TryGetProc(procRef.Name, out proc))
                        throw new Exception($"Type {instance.ObjectDefinition.Type} has no proc called \"{procRef.Name}\"");

                    break;
                }
                case DMReference.Type.GlobalProc: {
                    instance = null;
                    proc = state.DreamManager.GlobalProcs[procRef.Name];

                    break;
                }
                case DMReference.Type.SrcProc: {
                    instance = state.Instance;
                    if (!instance.TryGetProc(procRef.Name, out proc))
                        throw new Exception($"Type {instance.ObjectDefinition.Type} has no proc called \"{procRef.Name}\"");

                    break;
                }
                default: throw new Exception($"Invalid proc reference type {procRef.RefType}");
            }

            state.Call(proc, instance, arguments);
            return ProcStatus.Called;
        }

        public static ProcStatus? CallStatement(DMProcState state) {
            DreamProcArguments arguments = state.PopArguments();
            DreamValue source = state.Pop();

            switch (source.Type) {
                case DreamValue.DreamValueType.DreamObject: {
                    DreamObject dreamObject = source.GetValueAsDreamObject();
                    DreamValue procId = state.Pop();
                    DreamProc proc = null;

                    switch (procId.Type) {
                        case DreamValue.DreamValueType.String:
                            proc = dreamObject.GetProc(procId.GetValueAsString());
                            break;
                        case DreamValue.DreamValueType.DreamPath: {
                            DreamPath fullProcPath = procId.GetValueAsPath();
                            int procElementIndex = fullProcPath.FindElement("proc");

                            if (procElementIndex != -1) {
                                DreamPath procPath = fullProcPath.FromElements(procElementIndex + 1);
                                string? procName = procPath.LastElement;

                                if(procName != null) proc = dreamObject.GetProc(procName);
                            }
                            break;
                        }
                    }

                    if (proc != null) {
                        state.Call(proc, dreamObject, arguments);
                        return ProcStatus.Called;
                    }
                    throw new Exception("Invalid proc (" + procId + ")");
                }
                case DreamValue.DreamValueType.DreamPath: {
                    DreamPath fullProcPath = source.GetValueAsPath();
                    if (fullProcPath.Elements.Length != 2 || fullProcPath.LastElement is null) //Only global procs are supported here currently
                        throw new Exception($"Invalid call() proc \"{fullProcPath}\"");
                    string procName = fullProcPath.LastElement;
                    DreamProc proc = state.DreamManager.GlobalProcs[procName];

                    state.Call(proc, state.Instance, arguments);
                    return ProcStatus.Called;
                }
                case DreamValue.DreamValueType.String:
                    unsafe
                    {
                        var dllName = source.GetValueAsString();
                        var procName = state.Pop().GetValueAsString();
                        // DLL Invoke
                        var entryPoint = DllHelper.ResolveDllTarget(IoCManager.Resolve<DreamResourceManager>(), dllName, procName);

                        Span<nint> argV = stackalloc nint[arguments.ArgumentCount];
                        argV.Fill(0);
                        try
                        {
                            for (var i = 0; i < argV.Length; i++)
                            {
                                var arg = arguments.OrderedArguments[i].Stringify();
                                argV[i] = Marshal.StringToCoTaskMemUTF8(arg);
                            }

                            byte* ret;
                            if (arguments.ArgumentCount > 0) {
                                fixed (nint* ptr = &argV[0]) {
                                    ret = entryPoint(arguments.ArgumentCount, (byte**)ptr);
                                }
                            } else {
                                ret = entryPoint(0, (byte**)0);
                            }

                            if (ret == null) {
                                state.Push(DreamValue.Null);
                                return null;
                            }

                            var retString = Marshal.PtrToStringUTF8((nint)ret);
                            state.Push(new DreamValue(retString));
                            return null;
                        }
                        finally
                        {
                            foreach (var arg in argV)
                            {
                                if (arg != 0)
                                    Marshal.ZeroFreeCoTaskMemUTF8(arg);
                            }
                        }
                    }
                default:
                    throw new Exception("Call statement has an invalid source (" + source + ")");
            }
        }

        public static ProcStatus? Error(DMProcState state) {
            throw new Exception("Reached an error opcode");
        }

        public static ProcStatus? Jump(DMProcState state) {
            int position = state.ReadInt();

            state.Jump(position);
            return null;
        }

        public static ProcStatus? JumpIfFalse(DMProcState state) {
            int position = state.ReadInt();
            DreamValue value = state.Pop();

            if (!value.IsTruthy()) {
                state.Jump(position);
            }

            return null;
        }

        public static ProcStatus? JumpIfTrue(DMProcState state) {
            int position = state.ReadInt();
            DreamValue value = state.Pop();

            if (value.IsTruthy()) {
                state.Jump(position);
            }

            return null;
        }

        public static ProcStatus? JumpIfNullDereference(DMProcState state) {
            DMReference reference = state.ReadReference();
            int position = state.ReadInt();

            if (state.IsNullDereference(reference)) {
                state.Push(DreamValue.Null);
                state.Jump(position);
            }

            return null;
        }

        public static ProcStatus? Return(DMProcState state) {
            state.SetReturn(state.Pop());
            return ProcStatus.Returned;
        }

        public static ProcStatus? Throw(DMProcState state) {
            DreamValue value = state.Pop();

            if (value.TryGetValueAsDreamObjectOfType(DreamPath.Exception, out DreamObject exception)) {
                throw new CancellingRuntime($"'throw' thrown ({exception.GetVariable("name").GetValueAsString()})");
            }

            throw new CancellingRuntime($"'throw' thrown ({value})");
        }

        public static ProcStatus? SwitchCase(DMProcState state) {
            int casePosition = state.ReadInt();
            DreamValue testValue = state.Pop();
            DreamValue value = state.Pop();

            if (IsEqual(value, testValue)) {
                state.Jump(casePosition);
            } else {
                state.Push(value);
            }

            return null;
        }

        public static ProcStatus? SwitchCaseRange(DMProcState state) {
            int casePosition = state.ReadInt();
            DreamValue rangeUpper = state.Pop();
            DreamValue rangeLower = state.Pop();
            DreamValue value = state.Pop();

            bool matchesLower = IsGreaterThan(value, rangeLower) || IsEqual(value, rangeLower);
            bool matchesUpper = IsLessThan(value, rangeUpper) || IsEqual(value, rangeUpper);
            if (matchesLower && matchesUpper) {
                state.Jump(casePosition);
            } else {
                state.Push(value);
            }

            return null;
        }

        //Copy & run the interpreter in a new thread
        //Jump the current thread to after the spawn's code
        public static ProcStatus? Spawn(DMProcState state) {
            int jumpTo = state.ReadInt();
            float delay = state.Pop().GetValueAsFloat();
            int delayMilliseconds = (int)(delay * 100);

            // TODO: It'd be nicer if we could use something such as DreamThread.Spawn here
            // and have state.Spawn return a ProcState instead
            DreamThread newContext = state.Spawn();

            //Negative delays mean the spawned code runs immediately
            if (delayMilliseconds < 0) {
                newContext.Resume();
                // TODO: Does the rest of the proc get scheduled?
                // Does the value of the delay mean anything?
            } else {
                new Task(async () => {
                    if (delayMilliseconds != 0) {
                        await Task.Delay(delayMilliseconds);
                    } else {
                        await Task.Yield();
                    }
                    newContext.Resume();
                }).Start(TaskScheduler.FromCurrentSynchronizationContext());
            }

            state.Jump(jumpTo);
            return null;
        }
        #endregion Flow

        #region Others
        public static ProcStatus? Browse(DMProcState state) {
            string options = state.Pop().GetValueAsString();
            DreamValue body = state.Pop();
            DreamObject receiver = state.Pop().GetValueAsDreamObject();

            DreamObject client;
            if (receiver.IsSubtypeOf(DreamPath.Mob)) {
                client = receiver.GetVariable("client").GetValueAsDreamObject();
            } else if (receiver.IsSubtypeOf(DreamPath.Client)) {
                client = receiver;
            } else {
                throw new Exception("Invalid browse() recipient");
            }

            if (client != null) {
                DreamConnection connection = state.DreamManager.GetConnectionFromClient(client);

                string browseValue;
                if (body.Type == DreamValue.DreamValueType.DreamResource) {
                    browseValue = body.GetValueAsDreamResource().ReadAsString();
                } else {
                    browseValue = (string)body.Value;
                }

                connection.Browse(browseValue, options);
            }

            return null;
        }

        public static ProcStatus? BrowseResource(DMProcState state) {
            DreamValue filename = state.Pop();
            var value = state.Pop();
            DreamResource file;
            if (!value.TryGetValueAsDreamResource(out file))
            {
                if (value.TryGetValueAsDreamObjectOfType(DreamPath.Icon, out var icon))
                {
                    // TODO Only load the correct state/dir
                    file = IoCManager.Resolve<DreamResourceManager>()
                        .LoadResource(DreamMetaObjectIcon.ObjectToDreamIcon[icon].Icon);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            DreamObject receiver = state.Pop().GetValueAsDreamObject();

            DreamObject client;
            if (receiver.IsSubtypeOf(DreamPath.Mob)) {
                client = receiver.GetVariable("client").GetValueAsDreamObject();
            } else if (receiver.IsSubtypeOf(DreamPath.Client)) {
                client = receiver;
            } else {
                throw new Exception("Invalid browse_rsc() recipient");
            }

            if (client != null) {
                DreamConnection connection = state.DreamManager.GetConnectionFromClient(client);

                connection.BrowseResource(file, (filename.Value != null) ? filename.GetValueAsString() : Path.GetFileName(file.ResourcePath));
            }

            return null;
        }

        public static ProcStatus? DeleteObject(DMProcState state) {
            DreamObject dreamObject = state.Pop().GetValueAsDreamObject();

            dreamObject?.Delete(state.DreamManager);
            return null;
        }

        public static ProcStatus? OutputControl(DMProcState state) {
            string control = state.Pop().GetValueAsString();
            DreamValue message = state.Pop();
            DreamObject receiver = state.Pop().GetValueAsDreamObject();

            if (receiver == state.DreamManager.WorldInstance) {
                //Same as "world << ..."
                receiver.ObjectDefinition.MetaObject.OperatorOutput(new(receiver), message);
                return null;
            }

            DreamObject client;
            if (receiver.IsSubtypeOf(DreamPath.Mob)) {
                client = receiver.GetVariable("client").GetValueAsDreamObject();
            } else if (receiver.IsSubtypeOf(DreamPath.Client)) {
                client = receiver;
            } else {
                throw new Exception("Invalid output() recipient");
            }

            if (client != null) {
                DreamConnection connection = state.DreamManager.GetConnectionFromClient(client);

                if (message.Type != DreamValue.DreamValueType.String && message.Value != null) throw new Exception("Invalid output() message " + message);
                connection.OutputControl((string)message.Value, control);
            }

            return null;
        }

        public static ProcStatus? Prompt(DMProcState state) {
            DMValueType types = (DMValueType)state.ReadInt();
            DreamObject recipientMob;
            DreamValue title, message, defaultValue;

            DreamValue firstArg = state.Pop();
            if (firstArg.TryGetValueAsDreamObjectOfType(DreamPath.Mob, out recipientMob)) {
                message = state.Pop();
                title = state.Pop();
                defaultValue = state.Pop();
            } else {
                recipientMob = state.Usr;
                message = firstArg;
                title = state.Pop();
                defaultValue = state.Pop();
                state.Pop(); //Fourth argument, should be null
            }

            DreamObject clientObject;
            if (recipientMob != null && recipientMob.GetVariable("client").TryGetValueAsDreamObjectOfType(DreamPath.Client, out clientObject)) {
                DreamConnection connection = state.DreamManager.GetConnectionFromClient(clientObject);
                Task<DreamValue> promptTask = connection.Prompt(types, title.Stringify(), message.Stringify(), defaultValue.Stringify());

                // Could use a better solution. Either no anonymous async native proc at all, or just a better way to call them.
                var waiter = AsyncNativeProc.CreateAnonymousState(state.Thread, async (state) => await promptTask);
                state.Thread.PushProcState(waiter);
                return ProcStatus.Called;
            }

            return null;
        }

        public static ProcStatus? LocateCoord(DMProcState state)
        {
            var z = state.Pop();
            var y = state.Pop();
            var x = state.Pop();
            if (x.TryGetValueAsInteger(out var xInt) && y.TryGetValueAsInteger(out var yInt) &&
                z.TryGetValueAsInteger(out var zInt))
            {
                state.Push(new DreamValue(IoCManager.Resolve<IDreamMapManager>().GetTurf(xInt, yInt, zInt)));
            }
            else
            {
                state.Push(DreamValue.Null);
            }

            return null;
        }

        public static ProcStatus? Locate(DMProcState state) {
            if (!state.Pop().TryGetValueAsDreamObject(out var container))
            {
                state.Push(DreamValue.Null);
                return null;
            }

            DreamValue value = state.Pop();

            DreamList containerList;
            if (container != null && container.IsSubtypeOf(DreamPath.Atom)) {
                container.GetVariable("contents").TryGetValueAsDreamList(out containerList);
            } else {
                containerList = container as DreamList;
            }

            if (value.TryGetValueAsString(out string refString)) {
                if(int.TryParse(refString, out var refID))
                {
                    state.Push(new DreamValue(DreamObject.GetFromReferenceID(state.DreamManager, refID)));
                }
                else if (state.DreamManager.Tags.ContainsKey(refString))
                {
                    state.Push(new DreamValue(state.DreamManager.Tags[refString].First()));
                }
                else
                {
                    state.Push(DreamValue.Null);
                }

            } else if (value.TryGetValueAsPath(out DreamPath type)) {
                if (containerList == null) {
                    state.Push(DreamValue.Null);

                    return null;
                }

                foreach (DreamValue containerItem in containerList.GetValues()) {
                    if (!containerItem.TryGetValueAsDreamObject(out DreamObject dmObject)) continue;

                    if (dmObject.IsSubtypeOf(type)) {
                        state.Push(containerItem);

                        return null;
                    }
                }

                state.Push(DreamValue.Null);
            } else {
                if (containerList == null) {
                    state.Push(DreamValue.Null);

                    return null;
                }

                foreach (DreamValue containerItem in containerList.GetValues()) {
                    if (IsEqual(containerItem, value)) {
                        state.Push(containerItem);

                        return null;
                    }
                }

                state.Push(DreamValue.Null);
            }

            return null;
        }

        public static ProcStatus? PickWeighted(DMProcState state) {
            int count = state.ReadInt();

            (DreamValue Value, float CumulativeWeight)[] values = new (DreamValue, float)[count];
            float totalWeight = 0;
            for (int i = 0; i < count; i++) {
                DreamValue value = state.Pop();
                if (!state.Pop().TryGetValueAsFloat(out var weight))
                {
                    // Breaking change, no clue what weight BYOND is giving to non-nums
                    throw new Exception($"pick() weight '{weight}' is not a number");
                }

                totalWeight += weight;
                values[i] = (value, totalWeight);
            }

            double pick = state.DreamManager.Random.NextDouble() * totalWeight;
            for (int i = 0; i < values.Length; i++) {
                if (pick < values[i].CumulativeWeight) {
                    state.Push(values[i].Value);
                    break;
                }
            }

            return null;
        }

public static ProcStatus? PickUnweighted(DMProcState state) {
    int count = state.ReadInt();

    DreamValue picked = DreamValue.Null;
    if (count == 1) {
        DreamValue value = state.Pop();

        List<DreamValue> values;
        if (value.TryGetValueAsDreamList(out DreamList list)) {
            values = list.GetValues();
        } else if (value.Value is DreamProcArguments args) {
            values = args.GetAllArguments();
        } else {
            state.Push(value);
            return null;
        }

        picked = values[state.DreamManager.Random.Next(0, values.Count)];
    } else {
        int pickedIndex = state.DreamManager.Random.Next(0, count);

        for (int i = 0; i < count; i++) {
            DreamValue value = state.Pop();

            if (i == pickedIndex)
                picked = value;
        }
    }

    state.Push(picked);
    return null;
}

        ///<summary>Right now this is used exclusively by addtext() calls, to concatenate its arguments together,
        ///but later it might make sense to have this be a simplification path for detected repetitive additions of strings,
        ///so as to slightly reduce the amount of re-allocation taking place.
        ///</summary>.
        public static ProcStatus? MassConcatenation(DMProcState state)
        {
            int count = state.ReadInt();
            if (count < 2) // One or zero arguments -- shouldn't really ever happen. addtext() compiletimes with <2 args and stringification should probably be a different opcode
            {
                Logger.Warning("addtext() called with " + count.ToString() + " arguments at runtime."); // TODO: tweak this warning if this ever gets used for other sorts of string concat
                state.Push(DreamValue.Null);
                return null;
            }
            int estimated_string_size = count * 10; // FIXME: We can do better with string size prediction here.
            StringBuilder builder = new StringBuilder(estimated_string_size); // An approximate guess at how big this string is going to be.
            for(int i = 0; i < count; ++i)
            {
                if (state.Pop().TryGetValueAsString(out var addStr))
                {
                    builder.Append(addStr);
                }
            }
            state.Push(new DreamValue(builder.ToString()));
            return null;
        }

        public static ProcStatus? IsSaved(DMProcState state) {
            DreamValue owner = state.Pop();
            string property = state.ReadString();

            DreamObjectDefinition objectDefinition;
            if (owner.TryGetValueAsDreamObject(out DreamObject dreamObject)) {
                objectDefinition = dreamObject.ObjectDefinition;
            } else if (owner.TryGetValueAsPath(out DreamPath path)) {
                objectDefinition = state.DreamManager.ObjectTree.GetObjectDefinition(path);
            } else {
                throw new Exception("Invalid owner for issaved() call " + owner);
            }

            //TODO: Add support for var/const/ and var/tmp/ once those are properly in
            if (objectDefinition.GlobalVariables.ContainsKey(property))
            {
                state.Push(new DreamValue(0));
            }
            else
            {
                state.Push(new DreamValue(1));
            }
            return null;
        }
        #endregion Others

        #region Helpers
        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        private static bool IsEqual(DreamValue first, DreamValue second) {
            switch (first.Type) {
                case DreamValue.DreamValueType.DreamObject: {
                    DreamObject firstValue = first.GetValueAsDreamObject();

                    switch (second.Type) {
                        case DreamValue.DreamValueType.DreamObject: return firstValue == second.GetValueAsDreamObject();
                        case DreamValue.DreamValueType.DreamPath:
                        case DreamValue.DreamValueType.String:
                        case DreamValue.DreamValueType.Float: return false;
                    }

                    break;
                }
                case DreamValue.DreamValueType.Float: {
                    float firstValue = first.GetValueAsFloat();

                    switch (second.Type) {
                        case DreamValue.DreamValueType.Float: return firstValue == second.GetValueAsFloat();
                        case DreamValue.DreamValueType.DreamPath:
                        case DreamValue.DreamValueType.DreamObject:
                        case DreamValue.DreamValueType.String: return false;
                    }

                    break;
                }
                case DreamValue.DreamValueType.String: {
                    string firstValue = first.GetValueAsString();

                    switch (second.Type) {
                        case DreamValue.DreamValueType.String: return firstValue == second.GetValueAsString();
                        case DreamValue.DreamValueType.DreamObject:
                        case DreamValue.DreamValueType.Float: return false;
                    }

                    break;
                }
                case DreamValue.DreamValueType.DreamPath: {
                    DreamPath firstValue = first.GetValueAsPath();

                    switch (second.Type) {
                        case DreamValue.DreamValueType.DreamPath: return firstValue.Equals(second.GetValueAsPath());
                        case DreamValue.DreamValueType.Float:
                        case DreamValue.DreamValueType.DreamObject:
                        case DreamValue.DreamValueType.String: return false;
                    }

                    break;
                }
                case DreamValue.DreamValueType.DreamResource: {
                    DreamResource firstValue = first.GetValueAsDreamResource();

                    switch (second.Type) {
                        case DreamValue.DreamValueType.DreamResource: return firstValue.ResourcePath == second.GetValueAsDreamResource().ResourcePath;
                        default: return false;
                    }
                }
            }

            throw new NotImplementedException("Equal comparison for " + first + " and " + second + " is not implemented");
        }

        private static bool IsEquivalent(DreamValue first, DreamValue second) {
            if (first.TryGetValueAsDreamList(out var firstList) && second.TryGetValueAsDreamList(out var secondList))
            {
                if (firstList.GetLength() != secondList.GetLength()) return false;
                var firstValues = firstList.GetValues();
                var secondValues = secondList.GetValues();
                for(var i = 0; i < firstValues.Count; i++)
                {
                    if (!firstValues[i].Equals(secondValues[i])) return false;
                }

                return true;
            }


            throw new NotImplementedException("Equivalence comparison for " + first + " and " + second + " is not implemented");
        }

        private static bool IsGreaterThan(DreamValue first, DreamValue second) {
            switch (first.Type) {
                case DreamValue.DreamValueType.Float when second.Type == DreamValue.DreamValueType.Float:
                    return first.GetValueAsFloat() > second.GetValueAsFloat();
                case DreamValue.DreamValueType.Float when second.Value == null:
                    return first.GetValueAsFloat() > 0;
                default: {
                    if (first.Value == null && second.Type == DreamValue.DreamValueType.Float) {
                        return 0 > second.GetValueAsFloat();
                    }
                    throw new Exception("Invalid greater than comparison on " + first + " and " + second);
                }
            }
        }

        private static bool IsLessThan(DreamValue first, DreamValue second) {
            switch (first.Type) {
                case DreamValue.DreamValueType.Float when second.Type == DreamValue.DreamValueType.Float:
                    return first.GetValueAsFloat() < second.GetValueAsFloat();
                case DreamValue.DreamValueType.Float when second.Value == null:
                    return first.GetValueAsFloat() < 0;
                default: {
                    if (first.Value == null && second.Type == DreamValue.DreamValueType.Float) {
                        return 0 < second.GetValueAsFloat();
                    }
                    throw new Exception("Invalid less than comparison between " + first + " and " + second);
                }
            }
        }

        private static DreamValue MultiplyValues(DreamValue first, DreamValue second) {
            if (first == DreamValue.Null || second == DreamValue.Null) {
                return new(0);
            } else if (first.TryGetValueAsDreamObject(out var firstObject)) {
                if (firstObject.ObjectDefinition.MetaObject == null)
                    throw new Exception("Invalid multiply operation on " + first + " and " + second);

                return firstObject.ObjectDefinition.MetaObject.OperatorMultiply(first, second);
            } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Float) {
                return new(first.GetValueAsFloat() * second.GetValueAsFloat());
            } else {
                throw new Exception("Invalid multiply operation on " + first + " and " + second);
            }
        }

        private static DreamValue DivideValues(DreamValue first, DreamValue second) {
            if (first.Value == null) {
                return new(0);
            } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Float) {
                return new(first.GetValueAsFloat() / second.GetValueAsFloat());
            } else {
                throw new Exception("Invalid divide operation on " + first + " and " + second);
            }
        }

        private static DreamValue BitXorValues(DreamValue first, DreamValue second) {
            if (first.TryGetValueAsDreamList(out DreamList list)) {
                DreamList newList = DreamList.Create();
                List<DreamValue> values;

                if (second.TryGetValueAsDreamList(out DreamList secondList)) {
                    values = secondList.GetValues();
                } else {
                    values = new List<DreamValue>() { second };
                }

                foreach (DreamValue value in values) {
                    bool inFirstList = list.ContainsValue(value);
                    bool inSecondList = secondList.ContainsValue(value);

                    if (inFirstList ^ inSecondList) {
                        newList.AddValue(value);

                        DreamValue associatedValue = inFirstList ? list.GetValue(value) : secondList.GetValue(value);
                        if (associatedValue.Value != null) newList.SetValue(value, associatedValue);
                    }
                }

                return new DreamValue(newList);
            } else {
                return new DreamValue(first.GetValueAsInteger() ^ second.GetValueAsInteger());
            }
        }

        private static DreamValue ModulusValues(DreamValue first, DreamValue second) {
            if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Float) {
                return new DreamValue(first.GetValueAsInteger() % second.GetValueAsInteger());
            } else {
                throw new Exception("Invalid modulus operation on " + first + " and " + second);
            }
        }
        #endregion Helpers
    }
}
