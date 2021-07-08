using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.MetaObjects;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace OpenDreamRuntime.Procs {
    static class DMOpcodeHandlers {
        #region Values
        public static ProcStatus? Assign(DMProcState state) {
            DreamValue value = state.PopDreamValue();
            IDreamProcIdentifier identifier = state.PeekIdentifier();

            identifier.Assign(value);
            return null;
        }

        public static ProcStatus? CreateList(DMProcState state) {
            var list = state.Runtime.ObjectTree.CreateObject(DreamPath.List);

            var initProcState = list.InitProc(state.Thread, state.Usr, new DreamProcArguments(null));
            state.Thread.PushProcState(initProcState);
            return ProcStatus.Called;
        }

        public static ProcStatus? CreateListEnumerator(DMProcState state) {
            DreamObject listObject = state.PopDreamValue().GetValueAsDreamObject();
            DreamList list = listObject as DreamList;

            if (list == null) {
                if (listObject == null) {
                    list = null;
                } else if (listObject.IsSubtypeOf(DreamPath.Atom) || listObject.IsSubtypeOf(DreamPath.World)) {
                    list = listObject.GetVariable("contents").GetValueAsDreamList();
                } else {
                    throw new Exception("Object " + listObject + " is not a " + DreamPath.List + ", " + DreamPath.Atom + " or " + DreamPath.World);
                }
            }

            //Enumerate a copy of the list
            List<DreamValue> values = new();
            if (list != null) {
                values.Capacity = list.GetLength();

                foreach (DreamValue value in list.GetValues()) {
                    values.Add(value);
                }
            }

            state.EnumeratorStack.Push(values.GetEnumerator());
            return null;
        }

        public static ProcStatus? CreateRangeEnumerator(DMProcState state) {
            float step = state.PopDreamValue().GetValueAsFloat();
            float rangeEnd = state.PopDreamValue().GetValueAsFloat();
            float rangeStart = state.PopDreamValue().GetValueAsFloat();

            state.EnumeratorStack.Push(new DreamProcRangeEnumerator(rangeStart, rangeEnd, step));
            return null;
        }

        public static ProcStatus? CreateObject(DMProcState state) {
            DreamProcArguments arguments = state.PopArguments();
            DreamPath objectPath = state.PopDreamValue().GetValueAsPath();

            DreamObject newObject = state.Runtime.ObjectTree.CreateObject(objectPath);
            state.Thread.PushProcState(newObject.InitProc(state.Thread, state.Usr, arguments));
            return ProcStatus.Called;
        }

        public static ProcStatus? Dereference(DMProcState state) {
            DreamObject dreamObject = state.PopDreamValue().GetValueAsDreamObject();
            string identifierName = state.ReadString();

            if (dreamObject == null) throw new Exception("Cannot dereference '" + identifierName + "' on a null object");
            state.Push(new DreamProcIdentifierVariable(dreamObject, identifierName));
            return null;
        }

        public static ProcStatus? DereferenceConditional(DMProcState state) {
            DreamValue operand = state.PopDreamValue();
            string identifierName = state.ReadString();

            if (operand == DreamValue.Null) {
                state.Push(new DreamProcIdentifierNull());
                return null;
            }

            DreamObject dreamObject = operand.GetValueAsDreamObject();
            state.Push(new DreamProcIdentifierVariable(dreamObject, identifierName));
            return null;
        }

        public static ProcStatus? DereferenceProc(DMProcState state) {
            DreamObject dreamObject = state.PopDreamValue().GetValueAsDreamObject();
            string identifierName = state.ReadString();

            if (dreamObject == null) throw new Exception("Cannot dereference '" + identifierName + "' on a null object");

            if (dreamObject.TryGetProc(identifierName, out DreamProc proc)) {
                state.Push(new DreamProcIdentifierProc(proc, dreamObject));
            } else {
                throw new Exception("Proc '" + identifierName + "' doesn't exist");
            }
            return null;
        }

        public static ProcStatus? DereferenceProcConditional(DMProcState state) {
            DreamValue operand = state.PopDreamValue();
            string identifierName = state.ReadString();

            if (operand == DreamValue.Null) {
                state.Push(new DreamProcIdentifierNull());
                return null;
            }

            DreamObject dreamObject = operand.GetValueAsDreamObject();

            if (dreamObject.TryGetProc(identifierName, out DreamProc proc)) {
                state.Push(new DreamProcIdentifierProc(proc, dreamObject));
            } else {
                throw new Exception("Proc '" + identifierName + "' doesn't exist");
            }
            return null;
        }

        public static ProcStatus? DestroyEnumerator(DMProcState state) {
            state.EnumeratorStack.Pop();
            return null;
        }

        public static ProcStatus? Enumerate(DMProcState state) {
            int outputVarId = state.ReadByte();
            IEnumerator<DreamValue> enumerator = state.EnumeratorStack.Peek();
            bool successfulEnumeration = enumerator.MoveNext();

            state.Push(new DreamValue(successfulEnumeration ? 1 : 0));
            if (successfulEnumeration) {
                state.LocalVariables[outputVarId] = enumerator.Current;
            }
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
                            DreamValue value = state.PopDreamValue();

                            formattedString.Append(value.Stringify());
                            break;
                        }
                        case StringFormatTypes.Ref: {
                            DreamObject refObject = state.PopDreamValue().GetValueAsDreamObject();

                            formattedString.Append(refObject.CreateReferenceID());
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

        public static ProcStatus? GetIdentifier(DMProcState state) {
            string identifierName = state.ReadString();

            if (identifierName == "args") {
                DreamList argsList = state.Arguments.CreateDreamList(state.Runtime);

                argsList.ValueAssigned += (DreamList argsList, DreamValue key, DreamValue value) => {
                    switch (key.Type) {
                        case DreamValue.DreamValueType.String: {
                            string argumentName = key.GetValueAsString();

                            state.Arguments.NamedArguments[argumentName] = value;
                            state.LocalVariables[state.Proc.ArgumentNames.IndexOf(argumentName)] = value;
                            break;
                        }
                        case DreamValue.DreamValueType.Float: {
                            int argumentIndex = key.GetValueAsInteger() - 1;

                            state.Arguments.OrderedArguments[argumentIndex] = value;
                            state.LocalVariables[argumentIndex] = value;
                            break;
                        }
                        default:
                            throw new Exception("Invalid key used on an args list");
                    }
                };

                state.Push(new DreamValue(argsList));
            } else {
                state.Push(new DreamProcIdentifierVariable(state.Instance, identifierName));
            }
            return null;
        }

        public static ProcStatus? PushLocalVariable(DMProcState state) {
            int localVariableId = state.ReadByte();

            state.Push(new DreamProcIdentifierLocalVariable(state.LocalVariables, localVariableId));
            return null;
        }

        public static ProcStatus? GetProc(DMProcState state) {
            string identifierName = state.ReadString();

            if (state.Instance.TryGetProc(identifierName, out DreamProc proc)) {
                state.Push(new DreamProcIdentifierProc(proc, state.Instance));
            } else {
                throw new Exception("Proc '" + identifierName + "' doesn't exist");
            }
            return null;
        }

        public static ProcStatus? IndexList(DMProcState state) {
            DreamValue index = state.PopDreamValue();
            DreamValue indexing = state.PopDreamValue();

            if (indexing.TryGetValueAsDreamObject(out DreamObject dreamObject)) {
                state.Push(new DreamProcIdentifierIndex(dreamObject, index));
            } else if (indexing.TryGetValueAsString(out string text)) {
                char c = text[index.GetValueAsInteger() - 1];

                state.Push(new DreamValue(Convert.ToString(c)));
            } else {
                throw new Exception("Cannot index " + indexing);
            }
            
            return null;
        }

        public static ProcStatus? Initial(DMProcState state) {
            DreamValue owner = state.PopDreamValue();
            string property = state.ReadString();

            DreamObjectDefinition objectDefinition;
            if (owner.TryGetValueAsDreamObject(out DreamObject dreamObject)) {
                objectDefinition = dreamObject.ObjectDefinition;
            } else if (owner.TryGetValueAsPath(out DreamPath path)) {
                objectDefinition = state.Runtime.ObjectTree.GetObjectDefinitionFromPath(path);
            } else {
                throw new Exception("Invalid owner for initial() call " + owner);
            }

            state.Push(objectDefinition.Variables[property]);
            return null;
        }

        public static ProcStatus? IsNull(DMProcState state) {
            DreamValue value = state.PopDreamValue();

            state.Push(new DreamValue((value == DreamValue.Null) ? 1 : 0));
            return null;
        }

        public static ProcStatus? IsInList(DMProcState state) {
            DreamValue listValue = state.PopDreamValue();
            DreamValue value = state.PopDreamValue();

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
            DreamValue value = state.PopDreamValue();
            DreamList list = state.PopDreamValue().GetValueAsDreamList();

            list.AddValue(value);
            state.Push(new DreamValue(list));
            return null;
        }

        public static ProcStatus? ListAppendAssociated(DMProcState state) {
            DreamValue index = state.PopDreamValue();
            DreamValue value = state.PopDreamValue();
            DreamList list = state.PopDreamValue().GetValueAsDreamList();

            list.SetValue(index, value);
            state.Push(new DreamValue(list));
            return null;
        }

        public static ProcStatus? Pop(DMProcState state) {
            state.Pop();
            return null;
        }

        public static ProcStatus? PushCopy(DMProcState state) {
            state.PushCopy();
            return null;
        }

        public static ProcStatus? PushArgumentList(DMProcState state) {
            DreamProcArguments arguments = new DreamProcArguments(new List<DreamValue>(), new Dictionary<string, DreamValue>());
            DreamValue argListValue = state.PopDreamValue();

            if (argListValue.Value != null) {
                DreamList argList = argListValue.GetValueAsDreamList();
                List<DreamValue> argListValues = argList.GetValues();
                Dictionary<DreamValue, DreamValue> argListNamedValues = argList.GetAssociativeValues();

                foreach (DreamValue value in argListValues) {
                    if (!argListNamedValues.ContainsKey(value)) {
                        arguments.OrderedArguments.Add(value);
                    }
                }

                foreach (KeyValuePair<DreamValue, DreamValue> namedValue in argListNamedValues) {
                    string name = namedValue.Key.Value as string;

                    if (name != null) {
                        arguments.NamedArguments.Add(name, namedValue.Value);
                    } else {
                        throw new Exception("List contains a non-string key, and cannot be used as an arglist");
                    }
                }
            }

            state.Push(arguments);
            return null;
        }

        public static ProcStatus? PushArguments(DMProcState state) {
            DreamProcArguments arguments = new DreamProcArguments(new List<DreamValue>(), new Dictionary<string, DreamValue>());
            int argumentCount = state.ReadInt();
            DreamValue[] argumentValues = new DreamValue[argumentCount];

            for (int i = argumentCount - 1; i >= 0; i--) {
                argumentValues[i] = state.PopDreamValue();
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

        public static ProcStatus? PushProcArguments(DMProcState state) {
            state.Push(state.Arguments);
            return null;
        }

        public static ProcStatus? PushResource(DMProcState state) {
            string resourcePath = state.ReadString();

            state.Push(new DreamValue(state.Runtime.ResourceManager.LoadResource(resourcePath)));
            return null;
        }

        public static ProcStatus? PushSelf(DMProcState state) {
            state.Push(new DreamProcIdentifierSelfProc(state));
            return null;
        }

        public static ProcStatus? PushSrc(DMProcState state) {
            state.Push(new DreamValue(state.Instance));
            return null;
        }

        public static ProcStatus? PushUsr(DMProcState state) {
            state.Push(new DreamValue(state.Usr));
            return null;
        }

        public static ProcStatus? PushString(DMProcState state) {
            state.Push(new DreamValue(state.ReadString()));
            return null;
        }

        public static ProcStatus? PushSuperProc(DMProcState state) {
            state.Push(new DreamProcIdentifierProc(state.Proc.SuperProc, state.Instance));
            return null;
        }

        public static ProcStatus? SetLocalVariable(DMProcState state) {
            int variableId = state.ReadByte();
            DreamValue value = state.PopDreamValue();

            state.LocalVariables[variableId] = value;
            return null;
        }
        #endregion Values

        #region Math
        public static ProcStatus? Add(DMProcState state) {
            DreamValue second = state.PopDreamValue();
            DreamValue first = state.PopDreamValue();
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
            DreamValue second = state.PopDreamValue();
            IDreamProcIdentifier identifier = state.PeekIdentifier();
            DreamValue first = identifier.GetValue();

            if (first.Type == DreamValue.DreamValueType.DreamObject) {
                if (first.Value != null) {
                    IDreamMetaObject metaObject = first.GetValueAsDreamObject().ObjectDefinition.MetaObject;

                    if (metaObject != null) {
                        state.Pop();
                        state.Push(metaObject.OperatorAppend(first, second));
                    } else {
                        throw new Exception("Invalid append operation on " + first + " and " + second);
                    }
                } else {
                    identifier.Assign(second);
                }
            } else if (second.Value != null) {
                switch (first.Type) {
                    case DreamValue.DreamValueType.Float when second.Type == DreamValue.DreamValueType.Float:
                        identifier.Assign(new DreamValue(first.GetValueAsFloat() + second.GetValueAsFloat()));
                        break;
                    case DreamValue.DreamValueType.String when second.Type == DreamValue.DreamValueType.String:
                        identifier.Assign(new DreamValue(first.GetValueAsString() + second.GetValueAsString()));
                        break;
                    default:
                        throw new Exception("Invalid append operation on " + first + " and " + second);
                }
            }

            return null;
        }

        public static ProcStatus? BitAnd(DMProcState state) {
            DreamValue second = state.PopDreamValue();
            DreamValue first = state.PopDreamValue();

            if (first.TryGetValueAsDreamList(out DreamList list)) {
                DreamList newList = DreamList.Create(state.Runtime);

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

        public static ProcStatus? BitNot(DMProcState state) {
            int value = state.PopDreamValue().GetValueAsInteger();

            state.Push(new DreamValue((~value) & 0xFFFFFF));
            return null;
        }

        public static ProcStatus? BitOr(DMProcState state) {                        // x | y
            DreamValue second = state.PopDreamValue();
            DreamValue first = state.PopDreamValue();

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
            DreamValue second = state.PopDreamValue();
            object first = state.Pop();
            IDreamProcIdentifier firstIdentifier = first as IDreamProcIdentifier;

            //Savefiles get special treatment
            //"savefile["entry"] << ..." is the same as "savefile["entry"] = ..."
            if (first is DreamProcIdentifierIndex index && index.Object.IsSubtypeOf(DreamPath.Savefile)) {
                index.Assign(second);

                return null;
            }

            DreamValue firstValue = firstIdentifier?.GetValue() ?? (DreamValue)first;

            switch (firstValue.Type) {
                case DreamValue.DreamValueType.DreamObject: { //Output operation
                    if (firstValue == DreamValue.Null) {
                        state.Push(new DreamValue(0));
                    } else {
                        IDreamMetaObject metaObject = firstValue.GetValueAsDreamObject().ObjectDefinition.MetaObject;

                        state.Push(metaObject?.OperatorOutput(firstValue, second) ?? DreamValue.Null);
                    }
                    
                    break;
                }
                case DreamValue.DreamValueType.DreamResource:
                    firstValue.GetValueAsDreamResource().Output(second);

                    state.Push(DreamValue.Null);
                    break;
                case DreamValue.DreamValueType.Float when second.Type == DreamValue.DreamValueType.Float:
                    state.Push(new DreamValue(firstValue.GetValueAsInteger() << second.GetValueAsInteger()));
                    break;
                default:
                    throw new Exception("Invalid bit shift left operation on " + firstValue + " and " + second);
            }

            return null;
        }

        public static ProcStatus? BitShiftRight(DMProcState state) {
            object second = state.Pop();
            IDreamProcIdentifier secondIdentifier = second as IDreamProcIdentifier;

            //Savefiles get special treatment
            //"savefile["entry"] >> ..." is the same as "... = savefile["entry"]"
            if (state.Peek() is DreamProcIdentifierIndex index && index.Object.IsSubtypeOf(DreamPath.Savefile)) {
                state.Pop();

                secondIdentifier.Assign(index.GetValue());
                return null;
            }

            DreamValue first = state.PopDreamValue();
            DreamValue secondValue = secondIdentifier?.GetValue() ?? (DreamValue)second;

            if (first == DreamValue.Null) {
                state.Push(new DreamValue(0));
            } else if (first.Type == DreamValue.DreamValueType.Float && secondValue.Type == DreamValue.DreamValueType.Float) {
                state.Push(new DreamValue(first.GetValueAsInteger() >> secondValue.GetValueAsInteger()));
            } else {
                throw new Exception("Invalid bit shift right operation on " + first + " and " + secondValue);
            }

            return null;
        }

        public static ProcStatus? BitXor(DMProcState state) {
            DreamValue second = state.PopDreamValue();
            DreamValue first = state.PopDreamValue();

            if (first.TryGetValueAsDreamList(out DreamList list)) {
                DreamList newList = DreamList.Create(state.Runtime);
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

                state.Push(new DreamValue(newList));
            } else {
                state.Push(new DreamValue(first.GetValueAsInteger() ^ second.GetValueAsInteger()));
            }

            return null;
        }

        public static ProcStatus? BooleanAnd(DMProcState state) {
            DreamValue a = state.PopDreamValue();
            int jumpPosition = state.ReadInt();

            if (!a.IsTruthy()) {
                state.Push(a);
                state.Jump(jumpPosition);
            }

            return null;
        }

        public static ProcStatus? BooleanNot(DMProcState state) {
            DreamValue value = state.PopDreamValue();

            state.Push(new DreamValue(value.IsTruthy() ? 0 : 1));
            return null;
        }

        public static ProcStatus? BooleanOr(DMProcState state) {
            DreamValue a = state.PopDreamValue();
            int jumpPosition = state.ReadInt();

            if (a.IsTruthy()) {
                state.Push(a);
                state.Jump(jumpPosition);
            }
            return null;
        }

        public static ProcStatus? Combine(DMProcState state) {
            DreamValue second = state.PopDreamValue();
            IDreamProcIdentifier identifier = state.PeekIdentifier();
            DreamValue first = identifier.GetValue();

            if (first.Type == DreamValue.DreamValueType.DreamObject) {
                if (first.Value != null) {
                    IDreamMetaObject metaObject = first.GetValueAsDreamObject().ObjectDefinition.MetaObject;

                    if (metaObject != null) {
                        state.Pop();
                        state.Push(metaObject.OperatorCombine(first, second));
                    } else {
                        throw new Exception("Invalid combine operation on " + first + " and " + second);
                    }
                } else {
                    identifier.Assign(second);
                }
            } else if (second.Value != null) {
                if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Float) {
                    identifier.Assign(new DreamValue(first.GetValueAsInteger() | second.GetValueAsInteger()));
                } else if (first.Value == null) {
                    identifier.Assign(second);
                } else {
                    throw new Exception("Invalid combine operation on " + first + " and " + second);
                }
            }

            return null;
        }

        public static ProcStatus? Divide(DMProcState state) {
            DreamValue second = state.PopDreamValue();
            DreamValue first = state.PopDreamValue();

            if (first.Value == null) {
                state.Push(new DreamValue(0));
            } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Float) {
                state.Push(new DreamValue(first.GetValueAsFloat() / second.GetValueAsFloat()));
            } else {
                throw new Exception("Invalid divide operation on " + first + " and " + second);
            }

            return null;
        }

        public static ProcStatus? Mask(DMProcState state) {
            DreamValue second = state.PopDreamValue();
            IDreamProcIdentifier identifier = state.PeekIdentifier();
            DreamValue first = identifier.GetValue();

            switch (first.Type) {
                case DreamValue.DreamValueType.DreamObject when first.Value != null: {
                    IDreamMetaObject metaObject = first.GetValueAsDreamObject().ObjectDefinition.MetaObject;

                    if (metaObject != null) {
                        state.Pop();
                        state.Push(metaObject.OperatorMask(first, second));
                    } else {
                        throw new Exception("Invalid mask operation on " + first + " and " + second);
                    }
                    break;
                }
                case DreamValue.DreamValueType.DreamObject:
                    identifier.Assign(new DreamValue(0));
                    break;
                case DreamValue.DreamValueType.Float when second.Type == DreamValue.DreamValueType.Float:
                    identifier.Assign(new DreamValue(first.GetValueAsInteger() & second.GetValueAsInteger()));
                    break;
                default:
                    throw new Exception("Invalid mask operation on " + first + " and " + second);
            }

            return null;
        }

        public static ProcStatus? Modulus(DMProcState state) {
            DreamValue second = state.PopDreamValue();
            DreamValue first = state.PopDreamValue();

            if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Float) {
                state.Push(new DreamValue(first.GetValueAsInteger() % second.GetValueAsInteger()));
            } else {
                throw new Exception("Invalid modulus operation on " + first + " and " + second);
            }

            return null;
        }

        public static ProcStatus? Multiply(DMProcState state) {
            DreamValue second = state.PopDreamValue();
            DreamValue first = state.PopDreamValue();

            if (first.Value == null || second.Value == null) {
                state.Push(new DreamValue(0));
            } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Float) {
                state.Push(new DreamValue(first.GetValueAsFloat() * second.GetValueAsFloat()));
            } else {
                throw new Exception("Invalid multiply operation on " + first + " and " + second);
            }

            return null;
        }

        public static ProcStatus? Negate(DMProcState state) {
            DreamValue value = state.PopDreamValue();

            switch (value.Type) {
                case DreamValue.DreamValueType.Float: state.Push(new DreamValue(-value.GetValueAsFloat())); break;
                default: throw new Exception("Invalid negate operation on " + value);
            }

            return null;
        }

        public static ProcStatus? Power(DMProcState state) {
            DreamValue second = state.PopDreamValue();
            DreamValue first = state.PopDreamValue();

            if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Float) {
                state.Push(new DreamValue((float)Math.Pow(first.GetValueAsFloat(), second.GetValueAsFloat())));
            } else {
                throw new Exception("Invalid power operation on " + first + " and " + second);
            }

            return null;
        }

        public static ProcStatus? Remove(DMProcState state) {
            DreamValue second = state.PopDreamValue();
            IDreamProcIdentifier identifier = state.PeekIdentifier();
            DreamValue first = identifier.GetValue();

            switch (first.Type) {
                case DreamValue.DreamValueType.DreamObject when first.Value != null: {
                    IDreamMetaObject metaObject = first.GetValueAsDreamObject().ObjectDefinition.MetaObject;

                    if (metaObject != null) {
                        state.Pop();
                        state.Push(metaObject.OperatorRemove(first, second));
                    } else {
                        throw new Exception("Invalid remove operation on " + first + " and " + second);
                    }
                    break;
                }
                case DreamValue.DreamValueType.DreamObject when second.Type == DreamValue.DreamValueType.Float:
                    identifier.Assign(new DreamValue(-second.GetValueAsFloat()));
                    break;
                case DreamValue.DreamValueType.DreamObject:
                    throw new Exception("Invalid remove operation on " + first + " and " + second);
                case DreamValue.DreamValueType.Float when second.Type == DreamValue.DreamValueType.Float:
                    identifier.Assign(new DreamValue(first.GetValueAsFloat() - second.GetValueAsFloat()));
                    break;
                default:
                    throw new Exception("Invalid remove operation on " + first + " and " + second);
            }

            return null;
        }

        public static ProcStatus? Subtract(DMProcState state) {
            DreamValue second = state.PopDreamValue();
            DreamValue first = state.PopDreamValue();
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
            DreamValue second = state.PopDreamValue();
            DreamValue first = state.PopDreamValue();

            state.Push(new DreamValue(IsEqual(first, second) ? 1 : 0));
            return null;
        }

        public static ProcStatus? CompareGreaterThan(DMProcState state) {
            DreamValue second = state.PopDreamValue();
            DreamValue first = state.PopDreamValue();

            state.Push(new DreamValue(IsGreaterThan(first, second) ? 1 : 0));
            return null;
        }

        public static ProcStatus? CompareGreaterThanOrEqual(DMProcState state) {
            DreamValue second = state.PopDreamValue();
            DreamValue first = state.PopDreamValue();
            DreamValue result;

            if (first.TryGetValueAsInteger(out int firstInt) && firstInt == 0 && second == DreamValue.Null) result = new DreamValue(1);
            else if (first == DreamValue.Null && second.TryGetValueAsInteger(out int secondInt) && secondInt == 0) result = new DreamValue(1);
            else result = new DreamValue((IsEqual(first, second) || IsGreaterThan(first, second)) ? 1 : 0);

            state.Push(result);
            return null;
        }

        public static ProcStatus? CompareLessThan(DMProcState state) {
            DreamValue second = state.PopDreamValue();
            DreamValue first = state.PopDreamValue();

            state.Push(new DreamValue(IsLessThan(first, second) ? 1 : 0));
            return null;
        }

        public static ProcStatus? CompareLessThanOrEqual(DMProcState state) {
            DreamValue second = state.PopDreamValue();
            DreamValue first = state.PopDreamValue();
            DreamValue result;

            if (first.TryGetValueAsInteger(out int firstInt) && firstInt == 0 && second == DreamValue.Null) result = new DreamValue(1);
            else if (first == DreamValue.Null && second.TryGetValueAsInteger(out int secondInt) && secondInt == 0) result = new DreamValue(1);
            else result = new DreamValue((IsEqual(first, second) || IsLessThan(first, second)) ? 1 : 0);

            state.Push(result);
            return null;
        }

        public static ProcStatus? CompareNotEquals(DMProcState state) {
            DreamValue second = state.PopDreamValue();
            DreamValue first = state.PopDreamValue();

            state.Push(new DreamValue(IsEqual(first, second) ? 0 : 1));
            return null;
        }

        public static ProcStatus? IsType(DMProcState state) {
            DreamValue typeValue = state.PopDreamValue();
            DreamValue value = state.PopDreamValue();
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
            DreamProcArguments arguments = state.PopArguments();
            DreamProcIdentifierProc procIdentifier = (DreamProcIdentifierProc)state.PopIdentifier();
            state.Call(procIdentifier.Proc, procIdentifier.Instance, arguments);
            return ProcStatus.Called;
        }

        public static ProcStatus? CallSelf(DMProcState state) {
            DreamProcArguments arguments = state.PopArguments();

            state.Call(state.Proc, state.Instance, arguments);
            return ProcStatus.Called;
        }

        public static ProcStatus? CallStatement(DMProcState state) {
            DreamProcArguments arguments = state.PopArguments();
            DreamValue source = state.PopDreamValue();

            switch (source.Type) {
                case DreamValue.DreamValueType.DreamObject: {
                    DreamObject dreamObject = source.GetValueAsDreamObject();
                    DreamValue procId = state.PopDreamValue();
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
                                string procName = procPath.LastElement;

                                proc = dreamObject.GetProc(procName);
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
                    if (fullProcPath.Elements.Length != 2) throw new Exception("Invalid call() proc \"" + fullProcPath + "\"");
                    string procName = fullProcPath.LastElement;
                    DreamProc proc = state.Instance.GetProc(procName);

                    state.Call(proc, state.Instance, arguments);
                    return ProcStatus.Called;
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
            DreamValue value = state.PopDreamValue();

            if (!value.IsTruthy()) {
                state.Jump(position);
            }

            return null;
        }

        public static ProcStatus? JumpIfTrue(DMProcState state) {
            int position = state.ReadInt();
            DreamValue value = state.PopDreamValue();

            if (value.IsTruthy()) {
                state.Jump(position);
            }

            return null;
        }

        public static ProcStatus? JumpIfNullIdentifier(DMProcState state) {
            int position = state.ReadInt();

            var proc = state.PeekIdentifier();
            if (proc is DreamProcIdentifierNull) {
                state.Jump(position);
            }

            return null;
        }

        public static ProcStatus? Return(DMProcState state) {
            state.SetReturn(state.PopDreamValue());
            return ProcStatus.Returned;
        }

        public static ProcStatus? SwitchCase(DMProcState state) {
            int casePosition = state.ReadInt();
            DreamValue testValue = state.PopDreamValue();
            DreamValue value = state.PopDreamValue();

            if (IsEqual(value, testValue)) {
                state.Jump(casePosition);
            } else {
                state.Push(value);
            }

            return null;
        }

        public static ProcStatus? SwitchCaseRange(DMProcState state) {
            int casePosition = state.ReadInt();
            DreamValue rangeUpper = state.PopDreamValue();
            DreamValue rangeLower = state.PopDreamValue();
            DreamValue value = state.PopDreamValue();

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
        // TODO: If delay negative, do a switcharoo
        public static ProcStatus? Spawn(DMProcState state) {
            int jumpTo = state.ReadInt();
            float delay = state.PopDreamValue().GetValueAsFloat();
            int delayMilliseconds = (int)(delay * 100);

            // TODO: It'd be nicer if we could use something such as DreamThread.Spawn here
            // and have state.Spawn return a ProcState instead
            DreamThread newContext = state.Spawn();
            state.Runtime.TaskFactory.StartNew(async () => {
                await Task.Delay(delayMilliseconds);
                newContext.Resume();
            });

            state.Jump(jumpTo);
            return null;
        }
        #endregion Flow

        #region Others
        public static ProcStatus? Browse(DMProcState state) {
            string options = state.PopDreamValue().GetValueAsString();
            DreamValue body = state.PopDreamValue();
            DreamObject receiver = state.PopDreamValue().GetValueAsDreamObject();

            DreamObject client;
            if (receiver.IsSubtypeOf(DreamPath.Mob)) {
                client = receiver.GetVariable("client").GetValueAsDreamObject();
            } else if (receiver.IsSubtypeOf(DreamPath.Client)) {
                client = receiver;
            } else {
                throw new Exception("Invalid browse() recipient");
            }

            if (client != null) {
                DreamConnection connection = state.Runtime.Server.GetConnectionFromClient(client);

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
            DreamValue filename = state.PopDreamValue();
            DreamResource file = state.PopDreamValue().GetValueAsDreamResource();
            DreamObject receiver = state.PopDreamValue().GetValueAsDreamObject();

            DreamObject client;
            if (receiver.IsSubtypeOf(DreamPath.Mob)) {
                client = receiver.GetVariable("client").GetValueAsDreamObject();
            } else if (receiver.IsSubtypeOf(DreamPath.Client)) {
                client = receiver;
            } else {
                throw new Exception("Invalid browse_rsc() recipient");
            }

            if (client != null) {
                DreamConnection connection = state.Runtime.Server.GetConnectionFromClient(client);

                connection.BrowseResource(file, (filename.Value != null) ? filename.GetValueAsString() : Path.GetFileName(file.ResourcePath));
            }

            return null;
        }

        public static ProcStatus? DeleteObject(DMProcState state) {
            DreamObject dreamObject = state.PopDreamValue().GetValueAsDreamObject();

            if (dreamObject != null) {
                dreamObject.Delete();
            }

            return null;
        }

        public static ProcStatus? OutputControl(DMProcState state) {
            string control = state.PopDreamValue().GetValueAsString();
            DreamValue message = state.PopDreamValue();
            DreamObject receiver = state.PopDreamValue().GetValueAsDreamObject();

            DreamObject client;
            if (receiver.IsSubtypeOf(DreamPath.Mob)) {
                client = receiver.GetVariable("client").GetValueAsDreamObject();
            } else if (receiver.IsSubtypeOf(DreamPath.Client)) {
                client = receiver;
            } else {
                throw new Exception("Invalid output() recipient");
            }

            if (client != null) {
                DreamConnection connection = state.Runtime.Server.GetConnectionFromClient(client);

                if (message.Type != DreamValue.DreamValueType.String && message.Value != null) throw new Exception("Invalid output() message " + message);
                connection.OutputControl((string)message.Value, control);
            }

            return null;
        }

        public static ProcStatus? Prompt(DMProcState state) {
            DMValueType types = (DMValueType)state.ReadInt();
            DreamObject recipientMob;
            DreamValue title, message, defaultValue;

            DreamValue firstArg = state.PopDreamValue();
            if (firstArg.TryGetValueAsDreamObjectOfType(DreamPath.Mob, out recipientMob)) {
                message = state.PopDreamValue();
                title = state.PopDreamValue();
                defaultValue = state.PopDreamValue();
            } else {
                recipientMob = state.Usr;
                message = firstArg;
                title = state.PopDreamValue();
                defaultValue = state.PopDreamValue();
                state.PopDreamValue(); //Fourth argument, should be null
            }

            DreamObject clientObject;
            if (recipientMob != null && recipientMob.GetVariable("client").TryGetValueAsDreamObjectOfType(DreamPath.Client, out clientObject)) {
                DreamConnection connection = state.Runtime.Server.GetConnectionFromClient(clientObject);
                Task<DreamValue> promptTask = connection.Prompt(types, title.Stringify(), message.Stringify(), defaultValue.Stringify());

                // Could use a better solution. Either no anonymous async native proc at all, or just a better way to call them.
                var waiter = AsyncNativeProc.CreateAnonymousState(state.Thread, async (state) => await promptTask);
                state.Thread.PushProcState(waiter);
                return ProcStatus.Called;
            }

            return null;
        }

        public static ProcStatus? LocateCoord(DMProcState state) {
            int z = state.PopDreamValue().GetValueAsInteger();
            int y = state.PopDreamValue().GetValueAsInteger();
            int x = state.PopDreamValue().GetValueAsInteger();

            state.Push(new DreamValue(state.Runtime.Map.GetTurfAt(x, y, z)));
            return null;
        }

        public static ProcStatus? Locate(DMProcState state) {
            DreamObject container = state.PopDreamValue().GetValueAsDreamObject();
            DreamValue value = state.PopDreamValue();

            DreamList containerList;
            if (container != null && container.IsSubtypeOf(DreamPath.Atom)) {
                containerList = container.GetVariable("contents").GetValueAsDreamList();
            } else {
                containerList = container as DreamList;
            }

            if (value.TryGetValueAsString(out string refString)) {
                int refID = int.Parse(refString);

                state.Push(new DreamValue(DreamObject.GetFromReferenceID(state.Runtime, refID)));
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

        public static ProcStatus? IsSaved(DMProcState state) {
            DreamValue owner = state.PopDreamValue();
            string property = state.ReadString();

            DreamObjectDefinition objectDefinition;
            if (owner.TryGetValueAsDreamObject(out DreamObject dreamObject)) {
                objectDefinition = dreamObject.ObjectDefinition;
            } else if (owner.TryGetValueAsPath(out DreamPath path)) {
                objectDefinition = state.Runtime.ObjectTree.GetObjectDefinitionFromPath(path);
            } else {
                throw new Exception("Invalid owner for issaved() call " + owner);
            }

            //TODO: Add support for var/const/ and var/tmp/ once those are properly in
            if (objectDefinition.HasGlobalVariable(property))
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
        #endregion Helpers
    }
}
