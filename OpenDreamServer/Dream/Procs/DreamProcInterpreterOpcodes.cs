using OpenDreamServer.Dream.Objects;
using OpenDreamServer.Dream.Objects.MetaObjects;
using OpenDreamServer.Net;
using OpenDreamServer.Resources;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace OpenDreamServer.Dream.Procs {
    static class DreamProcInterpreterOpcodes {
        #region Values
        public static void Assign(DreamProcInterpreter interpreter) {
            DreamValue value = interpreter.PopDreamValue();
            IDreamProcIdentifier identifier = interpreter.PopIdentifier();

            identifier.Assign(value);
            interpreter.Push(value);
        }

        public static void CreateList(DreamProcInterpreter interpreter) {
            interpreter.Push(new DreamValue(Program.DreamObjectTree.CreateObject(DreamPath.List)));
        }

        public static void CreateListEnumerator(DreamProcInterpreter interpreter) {
            DreamObject listObject = interpreter.PopDreamValue().GetValueAsDreamObject();
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

            interpreter.EnumeratorStack.Push(values.GetEnumerator());
        }

        public static void CreateRangeEnumerator(DreamProcInterpreter interpreter) {
            float step = interpreter.PopDreamValue().GetValueAsNumber();
            float rangeEnd = interpreter.PopDreamValue().GetValueAsNumber();
            float rangeStart = interpreter.PopDreamValue().GetValueAsNumber();

            interpreter.EnumeratorStack.Push(new DreamProcRangeEnumerator(rangeStart, rangeEnd, step));
        }

        public static void CreateObject(DreamProcInterpreter interpreter) {
            DreamProcArguments arguments = interpreter.PopArguments();
            DreamPath objectPath = interpreter.PopDreamValue().GetValueAsPath();

            DreamObject newObject = Program.DreamObjectTree.CreateObject(objectPath, arguments);
            interpreter.Push(new DreamValue(newObject));
        }

        public static void Dereference(DreamProcInterpreter interpreter) {
            DreamObject dreamObject = interpreter.PopDreamValue().GetValueAsDreamObject();
            string identifierName = interpreter.ReadString();

            if (dreamObject == null) throw new Exception("Cannot dereference '" + identifierName + "' on a null object");
            interpreter.Push(new DreamProcIdentifierVariable(dreamObject, identifierName));
        }

        public static void DereferenceProc(DreamProcInterpreter interpreter) {
            DreamObject dreamObject = interpreter.PopDreamValue().GetValueAsDreamObject();
            string identifierName = interpreter.ReadString();

            if (dreamObject == null) throw new Exception("Cannot dereference '" + identifierName + "' on a null object");

            if (dreamObject.TryGetProc(identifierName, out DreamProc proc)) {
                interpreter.Push(new DreamProcIdentifierProc(proc, dreamObject, identifierName));
            } else {
                throw new Exception("Proc '" + identifierName + "' doesn't exist");
            }
        }

        public static void DestroyEnumerator(DreamProcInterpreter interpreter) {
            interpreter.EnumeratorStack.Pop();
        }

        public static void Enumerate(DreamProcInterpreter interpreter) {
            int outputVarId = interpreter.ReadByte();
            IEnumerator<DreamValue> enumerator = interpreter.EnumeratorStack.Peek();
            bool successfulEnumeration = enumerator.MoveNext();

            interpreter.Push(new DreamValue(successfulEnumeration ? 1 : 0));
            if (successfulEnumeration) {
                interpreter.LocalVariables[outputVarId] = enumerator.Current;
            }
        }

        public static void FormatString(DreamProcInterpreter interpreter) {
            string unformattedString = interpreter.ReadString();
            StringBuilder formattedString = new StringBuilder();

            for (int i = 0; i < unformattedString.Length; i++) {
                char c = unformattedString[i];

                if (c == (char)0xFF) {
                    c = unformattedString[++i];

                    switch ((StringFormatTypes)c) {
                        case StringFormatTypes.Stringify: {
                            DreamValue value = interpreter.PopDreamValue();

                            formattedString.Append(value.Stringify());
                            break;
                        }
                        case StringFormatTypes.Ref: {
                            DreamObject refObject = interpreter.PopDreamValue().GetValueAsDreamObject();

                            formattedString.Append(refObject.CreateReferenceID());
                            break;
                        }
                        default: throw new Exception("Invalid special character");
                    }
                } else {
                    formattedString.Append(c);
                }
            }

            interpreter.Push(new DreamValue(formattedString.ToString()));
        }

        public static void GetIdentifier(DreamProcInterpreter interpreter) {
            string identifierName = interpreter.ReadString();

            if (identifierName == "args") {
                DreamList argsList = interpreter.Arguments.CreateDreamList();

                argsList.ValueAssigned += (DreamList argsList, DreamValue key, DreamValue value) => {
                    if (key.Type == DreamValue.DreamValueType.String) {
                        string argumentName = key.GetValueAsString();

                        interpreter.Arguments.NamedArguments[argumentName] = value;
                        interpreter.LocalVariables[interpreter.ArgumentNames.IndexOf(argumentName)] = value;
                    } else if (key.Type == DreamValue.DreamValueType.Integer) {
                        int argumentIndex = key.GetValueAsInteger() - 1;

                        interpreter.Arguments.OrderedArguments[argumentIndex] = value;
                        interpreter.LocalVariables[argumentIndex] = value;
                    } else {
                        throw new Exception("Invalid key used on an args list");
                    }
                };

                interpreter.Push(new DreamValue(argsList));
            } else if (identifierName == "usr") {
                interpreter.Push(new DreamValue(interpreter.Usr));
            } else {
                interpreter.Push(new DreamProcIdentifierVariable(interpreter.Instance, identifierName));
            }
        }

        public static void PushLocalVariable(DreamProcInterpreter interpreter) {
            int localVariableId = interpreter.ReadByte();

            interpreter.Push(new DreamProcIdentifierLocalVariable(interpreter.LocalVariables, localVariableId));
        }

        public static void GetProc(DreamProcInterpreter interpreter) {
            string identifierName = interpreter.ReadString();

            if (interpreter.Instance.TryGetProc(identifierName, out DreamProc proc)) {
                interpreter.Push(new DreamProcIdentifierProc(proc, interpreter.Instance, identifierName));
            } else {
                throw new Exception("Proc '" + identifierName + "' doesn't exist");
            }
        }

        public static void IndexList(DreamProcInterpreter interpreter) {
            DreamValue index = interpreter.PopDreamValue();
            DreamList list = interpreter.PopDreamValue().GetValueAsDreamList();

            interpreter.Push(new DreamProcIdentifierListIndex(list, index));
        }

        public static void Initial(DreamProcInterpreter interpreter) {
            DreamValue owner = interpreter.PopDreamValue();
            string property = interpreter.ReadString();

            DreamObjectDefinition objectDefinition;
            if (owner.TryGetValueAsDreamObject(out DreamObject dreamObject)) {
                objectDefinition = dreamObject.ObjectDefinition;
            } else if (owner.TryGetValueAsPath(out DreamPath path)) {
                objectDefinition = Program.DreamObjectTree.GetObjectDefinitionFromPath(path);
            } else {
                throw new Exception("Invalid owner for initial() call " + owner);
            }

            interpreter.Push(objectDefinition.Variables[property]);
        }

        public static void IsNull(DreamProcInterpreter interpreter) {
            DreamValue value = interpreter.PopDreamValue();

            interpreter.Push(new DreamValue((value == DreamValue.Null) ? 1 : 0));
        }

        public static void IsInList(DreamProcInterpreter interpreter) {
            DreamValue listValue = interpreter.PopDreamValue();
            DreamValue value = interpreter.PopDreamValue();

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

                interpreter.Push(new DreamValue(list.ContainsValue(value) ? 1 : 0));
            } else {
                interpreter.Push(new DreamValue(0));
            }
        }

        public static void ListAppend(DreamProcInterpreter interpreter) {
            DreamValue value = interpreter.PopDreamValue();
            DreamList list = interpreter.PopDreamValue().GetValueAsDreamList();

            list.AddValue(value);
            interpreter.Push(new DreamValue(list));
        }

        public static void ListAppendAssociated(DreamProcInterpreter interpreter) {
            DreamValue index = interpreter.PopDreamValue();
            DreamValue value = interpreter.PopDreamValue();
            DreamList list = interpreter.PopDreamValue().GetValueAsDreamList();

            list.SetValue(index, value);
            interpreter.Push(new DreamValue(list));
        }

        public static void PushArgumentList(DreamProcInterpreter interpreter) {
            DreamProcArguments arguments = new DreamProcArguments(new List<DreamValue>(), new Dictionary<string, DreamValue>());
            DreamValue argListValue = interpreter.PopDreamValue();

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

            interpreter.Push(arguments);
        }

        public static void PushArguments(DreamProcInterpreter interpreter) {
            DreamProcArguments arguments = new DreamProcArguments(new List<DreamValue>(), new Dictionary<string, DreamValue>());
            int argumentCount = interpreter.ReadInt();
            DreamValue[] argumentValues = new DreamValue[argumentCount];

            for (int i = argumentCount - 1; i >= 0; i--) {
                argumentValues[i] = interpreter.PopDreamValue();
            }

            for (int i = 0; i < argumentCount; i++) {
                DreamProcOpcodeParameterType argumentType = (DreamProcOpcodeParameterType)interpreter.ReadByte();

                if (argumentType == DreamProcOpcodeParameterType.Named) {
                    string argumentName = interpreter.ReadString();

                    arguments.NamedArguments[argumentName] = argumentValues[i];
                } else if (argumentType == DreamProcOpcodeParameterType.Unnamed) {
                    arguments.OrderedArguments.Add(argumentValues[i]);
                } else {
                    throw new Exception("Invalid argument type (" + argumentType + ")");
                }
            }

            interpreter.Push(arguments);
        }

        public static void PushFloat(DreamProcInterpreter interpreter) {
            float value = interpreter.ReadFloat();

            interpreter.Push(new DreamValue(value));
        }

        public static void PushInt(DreamProcInterpreter interpreter) {
            int value = interpreter.ReadInt();

            interpreter.Push(new DreamValue(value));
        }

        public static void PushNull(DreamProcInterpreter interpreter) {
            interpreter.Push(DreamValue.Null);
        }

        public static void PushPath(DreamProcInterpreter interpreter) {
            DreamPath path = new DreamPath(interpreter.ReadString());

            interpreter.Push(new DreamValue(path));
        }

        public static void PushProcArguments(DreamProcInterpreter interpreter) {
            interpreter.Push(interpreter.Arguments);
        }

        public static void PushResource(DreamProcInterpreter interpreter) {
            string resourcePath = interpreter.ReadString();

            interpreter.Push(new DreamValue(Program.DreamResourceManager.LoadResource(resourcePath)));
        }

        public static void PushSelf(DreamProcInterpreter interpreter) {
            interpreter.Push(new DreamProcIdentifierSelfProc(interpreter.SelfProc, interpreter));
        }

        public static void PushSrc(DreamProcInterpreter interpreter) {
            interpreter.Push(new DreamValue(interpreter.Instance));
        }
        
        public static void PushUsr(DreamProcInterpreter interpreter) {
            interpreter.Push(new DreamValue(interpreter.Usr));
        }

        public static void PushString(DreamProcInterpreter interpreter) {
            interpreter.Push(new DreamValue(interpreter.ReadString()));
        }

        public static void PushSuperProc(DreamProcInterpreter interpreter) {
            interpreter.Push(new DreamProcIdentifierProc(interpreter.SuperProc, interpreter.Instance, ".."));
        }

        public static void SetLocalVariable(DreamProcInterpreter interpreter) {
            int variableId = interpreter.ReadByte();
            DreamValue value = interpreter.PopDreamValue();

            interpreter.LocalVariables[variableId] = value;
        }
        #endregion Values

        #region Math
        public static void Add(DreamProcInterpreter interpreter) {
            DreamValue second = interpreter.PopDreamValue();
            DreamValue first = interpreter.PopDreamValue();
            DreamValue? output = null;

            if (second.Value == null) {
                output = first;
            } else if (first.Value == null) {
                output = second;
            } else if (first.Type == DreamValue.DreamValueType.Integer) {
                int firstInt = first.GetValueAsInteger();

                switch (second.Type) {
                    case DreamValue.DreamValueType.Integer: output = new DreamValue(firstInt + second.GetValueAsInteger()); break;
                    case DreamValue.DreamValueType.Float: output = new DreamValue(firstInt + second.GetValueAsFloat()); break;
                }
            } else if (first.Type == DreamValue.DreamValueType.Float) {
                float firstFloat = first.GetValueAsFloat();

                switch (second.Type) {
                    case DreamValue.DreamValueType.Integer: output = new DreamValue(firstFloat + second.GetValueAsInteger()); break;
                    case DreamValue.DreamValueType.Float: output = new DreamValue(firstFloat + second.GetValueAsFloat()); break;
                }
            } else if (first.Type == DreamValue.DreamValueType.String && second.Type == DreamValue.DreamValueType.String) {
                output = new DreamValue(first.GetValueAsString() + second.GetValueAsString());
            } else if (first.Type == DreamValue.DreamValueType.DreamObject) {
                IDreamMetaObject metaObject = first.GetValueAsDreamObject().ObjectDefinition.MetaObject;

                if (metaObject != null) {
                    output = metaObject.OperatorAdd(first, second);
                }
            }
            
            if (output != null) {
                interpreter.Push(output.Value);
            } else {
                throw new Exception("Invalid add operation on " + first + " and " + second);
            }
        }

        public static void Append(DreamProcInterpreter interpreter) {
            DreamValue second = interpreter.PopDreamValue();
            IDreamProcIdentifier identifier = interpreter.PopIdentifier();
            DreamValue first = identifier.GetValue();

            if (first.Type == DreamValue.DreamValueType.DreamObject) {
                if (first.Value != null) {
                    IDreamMetaObject metaObject = first.GetValueAsDreamObject().ObjectDefinition.MetaObject;

                    if (metaObject != null) {
                        interpreter.Push(metaObject.OperatorAppend(first, second));
                    } else {
                        throw new Exception("Invalid append operation on " + first + " and " + second);
                    }
                } else {
                    identifier.Assign(second);
                }
            } else if (second.Value != null) {
                if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Integer) {
                    identifier.Assign(new DreamValue(first.GetValueAsInteger() + second.GetValueAsInteger()));
                } else if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Float) {
                    identifier.Assign(new DreamValue(first.GetValueAsInteger() + second.GetValueAsFloat()));
                } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Float) {
                    identifier.Assign(new DreamValue(first.GetValueAsFloat() + second.GetValueAsFloat()));
                } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Integer) {
                    identifier.Assign(new DreamValue(first.GetValueAsFloat() + second.GetValueAsInteger()));
                } else if (first.Type == DreamValue.DreamValueType.String && second.Type == DreamValue.DreamValueType.String) {
                    identifier.Assign(new DreamValue(first.GetValueAsString() + second.GetValueAsString()));
                } else {
                    throw new Exception("Invalid append operation on " + first + " and " + second);
                }
            }
        }

        public static void BitAnd(DreamProcInterpreter interpreter) {
            DreamValue second = interpreter.PopDreamValue();
            DreamValue first = interpreter.PopDreamValue();

            if (first.TryGetValueAsDreamList(out DreamList list)) {
                DreamList newList = new DreamList();

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

                interpreter.Push(new DreamValue(newList));
            } else if (first.Value != null && second.Value != null) {
                interpreter.Push(new DreamValue((int)first.GetValueAsNumber() & (int)second.GetValueAsNumber()));
            } else {
                interpreter.Push(new DreamValue(0));
            }
        }

        public static void BitNot(DreamProcInterpreter interpreter) {
            int value = interpreter.PopDreamValue().GetValueAsInteger();

            interpreter.Push(new DreamValue((~value) & 0xFFFFFF));
        }

        public static void BitOr(DreamProcInterpreter interpreter) {
            int second = interpreter.PopDreamValue().GetValueAsInteger();
            int first = interpreter.PopDreamValue().GetValueAsInteger();

            interpreter.Push(new DreamValue(first | second));
        }

        public static void BitShiftLeft(DreamProcInterpreter interpreter) {
            DreamValue second = interpreter.PopDreamValue();
            DreamValue first = interpreter.PopDreamValue();

            if (first.Type == DreamValue.DreamValueType.DreamObject && first.Value != null) { //Output operation
                IDreamMetaObject metaObject = first.GetValueAsDreamObject().ObjectDefinition.MetaObject;

                if (metaObject != null) {
                    interpreter.Push(metaObject.OperatorOutput(first, second));
                } else {
                    throw new Exception("Invalid output operation on " + first + " and " + second);
                }
            } else if (first.Type == DreamValue.DreamValueType.DreamResource) {
                first.GetValueAsDreamResource().Output(second);

                interpreter.Push(DreamValue.Null);
            } else if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Integer) {
                interpreter.Push(new DreamValue(first.GetValueAsInteger() << second.GetValueAsInteger()));
            } else {
                throw new Exception("Invalid bit shift left operation on " + first + " and " + second);
            }
        }

        public static void BitShiftRight(DreamProcInterpreter interpreter) {
            DreamValue second = interpreter.PopDreamValue();
            DreamValue first = interpreter.PopDreamValue();

            if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Integer) {
                interpreter.Push(new DreamValue(first.GetValueAsInteger() >> second.GetValueAsInteger()));
            } else {
                throw new Exception("Invalid bit shift right operation on " + first + " and " + second);
            }
        }

        public static void BitXor(DreamProcInterpreter interpreter) {
            DreamValue second = interpreter.PopDreamValue();
            DreamValue first = interpreter.PopDreamValue();

            if (first.TryGetValueAsDreamList(out DreamList list)) {
                DreamList newList = new DreamList();
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

                interpreter.Push(new DreamValue(newList));
            } else {
                interpreter.Push(new DreamValue(first.GetValueAsInteger() ^ second.GetValueAsInteger()));
            }
        }

        public static void BooleanAnd(DreamProcInterpreter interpreter) {
            DreamValue a = interpreter.PopDreamValue();
            int jumpPosition = interpreter.ReadInt();

            if (!IsTruthy(a)) {
                interpreter.Push(a);
                interpreter.JumpTo(jumpPosition);
            }
        }

        public static void BooleanNot(DreamProcInterpreter interpreter) {
            DreamValue value = interpreter.PopDreamValue();

            interpreter.Push(new DreamValue(IsTruthy(value) ? 0 : 1));
        }

        public static void BooleanOr(DreamProcInterpreter interpreter) {
            DreamValue a = interpreter.PopDreamValue();
            int jumpPosition = interpreter.ReadInt();

            if (IsTruthy(a)) {
                interpreter.Push(a);
                interpreter.JumpTo(jumpPosition);
            }
        }

        public static void Combine(DreamProcInterpreter interpreter) {
            DreamValue second = interpreter.PopDreamValue();
            IDreamProcIdentifier identifier = interpreter.PopIdentifier();
            DreamValue first = identifier.GetValue();

            if (first.Type == DreamValue.DreamValueType.DreamObject) {
                if (first.Value != null) {
                    IDreamMetaObject metaObject = first.GetValueAsDreamObject().ObjectDefinition.MetaObject;

                    if (metaObject != null) {
                        interpreter.Push(metaObject.OperatorCombine(first, second));
                    } else {
                        throw new Exception("Invalid combine operation on " + first + " and " + second);
                    }
                } else {
                    identifier.Assign(second);
                }
            } else if (second.Value != null) {
                if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Integer) {
                    identifier.Assign(new DreamValue(first.GetValueAsInteger() | second.GetValueAsInteger()));
                } else if (first.Value == null) {
                    identifier.Assign(second);
                } else {
                    throw new Exception("Invalid combine operation on " + first + " and " + second);
                }
            }
        }

        public static void Divide(DreamProcInterpreter interpreter) {
            DreamValue second = interpreter.PopDreamValue();
            DreamValue first = interpreter.PopDreamValue();

            if (first.Value == null) {
                interpreter.Push(new DreamValue(0));
            } else if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Integer) {
                interpreter.Push(new DreamValue(first.GetValueAsNumber() / second.GetValueAsNumber()));
            } else if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Float) {
                interpreter.Push(new DreamValue(first.GetValueAsNumber() / second.GetValueAsNumber()));
            } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Integer) {
                interpreter.Push(new DreamValue(first.GetValueAsNumber() / second.GetValueAsNumber()));
            } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Float) {
                interpreter.Push(new DreamValue(first.GetValueAsNumber() / second.GetValueAsNumber()));
            } else {
                throw new Exception("Invalid divide operation on " + first + " and " + second);
            }
        }

        public static void Mask(DreamProcInterpreter interpreter) {
            DreamValue second = interpreter.PopDreamValue();
            IDreamProcIdentifier identifier = interpreter.PopIdentifier();
            DreamValue first = identifier.GetValue();

            if (first.Type == DreamValue.DreamValueType.DreamObject) {
                if (first.Value != null) {
                    IDreamMetaObject metaObject = first.GetValueAsDreamObject().ObjectDefinition.MetaObject;

                    if (metaObject != null) {
                        interpreter.Push(metaObject.OperatorMask(first, second));
                    } else {
                        throw new Exception("Invalid mask operation on " + first + " and " + second);
                    }
                } else {
                    identifier.Assign(new DreamValue(0));
                }
            } else if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Integer) {
                identifier.Assign(new DreamValue(first.GetValueAsInteger() & second.GetValueAsInteger()));
            } else {
                throw new Exception("Invalid mask operation on " + first + " and " + second);
            }
        }

        public static void Modulus(DreamProcInterpreter interpreter) {
            DreamValue second = interpreter.PopDreamValue();
            DreamValue first = interpreter.PopDreamValue();

            if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Integer) {
                interpreter.Push(new DreamValue(first.GetValueAsInteger() % second.GetValueAsInteger()));
            } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Integer) {
                interpreter.Push(new DreamValue((int)(first.GetValueAsFloat() % second.GetValueAsInteger())));
            } else {
                throw new Exception("Invalid modulus operation on " + first + " and " + second);
            }
        }

        public static void Multiply(DreamProcInterpreter interpreter) {
            DreamValue second = interpreter.PopDreamValue();
            DreamValue first = interpreter.PopDreamValue();

            if (first.Value == null || second.Value == null) {
                interpreter.Push(new DreamValue(0));
            } else if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Integer) {
                interpreter.Push(new DreamValue(first.GetValueAsInteger() * second.GetValueAsInteger()));
            } else if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Float) {
                interpreter.Push(new DreamValue(first.GetValueAsInteger() * second.GetValueAsFloat()));
            } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Integer) {
                interpreter.Push(new DreamValue(first.GetValueAsFloat() * second.GetValueAsInteger()));
            } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Float) {
                interpreter.Push(new DreamValue(first.GetValueAsFloat() * second.GetValueAsFloat()));
            } else {
                throw new Exception("Invalid multiply operation on " + first + " and " + second);
            }
        }

        public static void Negate(DreamProcInterpreter interpreter) {
            DreamValue value = interpreter.PopDreamValue();

            switch (value.Type) {
                case DreamValue.DreamValueType.Integer: interpreter.Push(new DreamValue(-value.GetValueAsInteger())); break;
                case DreamValue.DreamValueType.Float: interpreter.Push(new DreamValue(-value.GetValueAsFloat())); break;
            }
        }

        public static void Power(DreamProcInterpreter interpreter) {
            DreamValue second = interpreter.PopDreamValue();
            DreamValue first = interpreter.PopDreamValue();

            if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Integer) {
                interpreter.Push(new DreamValue((float)Math.Pow(first.GetValueAsInteger(), second.GetValueAsInteger())));
            } else if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Float) {
                interpreter.Push(new DreamValue((float)Math.Pow(first.GetValueAsInteger(), second.GetValueAsFloat())));
            } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Float) {
                interpreter.Push(new DreamValue((float)Math.Pow(first.GetValueAsFloat(), second.GetValueAsFloat())));
            } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Integer) {
                interpreter.Push(new DreamValue((float)Math.Pow(first.GetValueAsFloat(), second.GetValueAsInteger())));
            } else {
                throw new Exception("Invalid power operation on " + first + " and " + second);
            }
        }

        public static void Remove(DreamProcInterpreter interpreter) {
            DreamValue second = interpreter.PopDreamValue();
            IDreamProcIdentifier identifier = interpreter.PopIdentifier();
            DreamValue first = identifier.GetValue();

            if (first.Type == DreamValue.DreamValueType.DreamObject) {
                if (first.Value != null) {
                    IDreamMetaObject metaObject = first.GetValueAsDreamObject().ObjectDefinition.MetaObject;

                    if (metaObject != null) {
                        interpreter.Push(metaObject.OperatorRemove(first, second));
                    } else {
                        throw new Exception("Invalid remove operation on " + first + " and " + second);
                    }
                } else if (second.Type == DreamValue.DreamValueType.Integer) {
                    identifier.Assign(new DreamValue(-(second.GetValueAsInteger())));
                } else {
                    throw new Exception("Invalid remove operation on " + first + " and " + second);
                }
            } else if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Integer) {
                identifier.Assign(new DreamValue(first.GetValueAsInteger() - second.GetValueAsInteger()));
            } else if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Float) {
                identifier.Assign(new DreamValue(first.GetValueAsInteger() - second.GetValueAsFloat()));
            } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Float) {
                identifier.Assign(new DreamValue(first.GetValueAsFloat() - second.GetValueAsFloat()));
            } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Integer) {
                identifier.Assign(new DreamValue(first.GetValueAsFloat() - second.GetValueAsInteger()));
            } else {
                throw new Exception("Invalid remove operation on " + first + " and " + second);
            }
        }

        public static void Subtract(DreamProcInterpreter interpreter) {
            DreamValue second = interpreter.PopDreamValue();
            DreamValue first = interpreter.PopDreamValue();
            DreamValue? output = null;

            if (second.Value == null) {
                output = first;
            } else if (first.Value == null) {
                if (second.Type == DreamValue.DreamValueType.Integer) {
                    output = new DreamValue(-second.GetValueAsInteger());
                } else if (second.Type == DreamValue.DreamValueType.Float) {
                    output = new DreamValue(-second.GetValueAsFloat());
                }
            } else if (first.Type == DreamValue.DreamValueType.Integer) {
                int firstInt = first.GetValueAsInteger();

                switch (second.Type) {
                    case DreamValue.DreamValueType.Integer: output = new DreamValue(firstInt - second.GetValueAsInteger()); break;
                    case DreamValue.DreamValueType.Float: output = new DreamValue(firstInt - second.GetValueAsFloat()); break;
                }
            } else if (first.Type == DreamValue.DreamValueType.Float) {
                float firstFloat = first.GetValueAsFloat();

                switch (second.Type) {
                    case DreamValue.DreamValueType.Integer: output = new DreamValue(firstFloat - second.GetValueAsInteger()); break;
                    case DreamValue.DreamValueType.Float: output = new DreamValue(firstFloat - second.GetValueAsFloat()); break;
                }
            } else if (first.Type == DreamValue.DreamValueType.DreamObject) {
                IDreamMetaObject metaObject = first.GetValueAsDreamObject().ObjectDefinition.MetaObject;

                if (metaObject != null) {
                    output = metaObject.OperatorSubtract(first, second);
                }
            }

            if (output != null) {
                interpreter.Push(output.Value);
            } else {
                throw new Exception("Invalid subtract operation on " + first + " and " + second);
            }
        }
        #endregion Math

        #region Comparisons
        public static void CompareEquals(DreamProcInterpreter interpreter) {
            DreamValue second = interpreter.PopDreamValue();
            DreamValue first = interpreter.PopDreamValue();

            interpreter.Push(new DreamValue(IsEqual(first, second) ? 1 : 0));
        }

        public static void CompareGreaterThan(DreamProcInterpreter interpreter) {
            DreamValue second = interpreter.PopDreamValue();
            DreamValue first = interpreter.PopDreamValue();

            interpreter.Push(new DreamValue(IsGreaterThan(first, second) ? 1 : 0));
        }

        public static void CompareGreaterThanOrEqual(DreamProcInterpreter interpreter) {
            DreamValue second = interpreter.PopDreamValue();
            DreamValue first = interpreter.PopDreamValue();
            DreamValue result;

            if (first.TryGetValueAsInteger(out int firstInt) && firstInt == 0 && second == DreamValue.Null) result = new DreamValue(1);
            else if (first == DreamValue.Null && second.TryGetValueAsInteger(out int secondInt) && secondInt == 0) result = new DreamValue(1);
            else result = new DreamValue((IsEqual(first, second) || IsGreaterThan(first, second)) ? 1 : 0);

            interpreter.Push(result);
        }

        public static void CompareLessThan(DreamProcInterpreter interpreter) {
            DreamValue second = interpreter.PopDreamValue();
            DreamValue first = interpreter.PopDreamValue();

            interpreter.Push(new DreamValue(IsLessThan(first, second) ? 1 : 0));
        }

        public static void CompareLessThanOrEqual(DreamProcInterpreter interpreter) {
            DreamValue second = interpreter.PopDreamValue();
            DreamValue first = interpreter.PopDreamValue();
            DreamValue result;

            if (first.TryGetValueAsInteger(out int firstInt) && firstInt == 0 && second == DreamValue.Null) result = new DreamValue(1);
            else if (first == DreamValue.Null && second.TryGetValueAsInteger(out int secondInt) && secondInt == 0) result = new DreamValue(1);
            else result = new DreamValue((IsEqual(first, second) || IsLessThan(first, second)) ? 1 : 0);

            interpreter.Push(result);
        }

        public static void CompareNotEquals(DreamProcInterpreter interpreter) {
            DreamValue second = interpreter.PopDreamValue();
            DreamValue first = interpreter.PopDreamValue();

            interpreter.Push(new DreamValue(IsEqual(first, second) ? 0 : 1));
        }

        public static void IsType(DreamProcInterpreter interpreter) {
            DreamValue typeValue = interpreter.PopDreamValue();
            DreamValue value = interpreter.PopDreamValue();
            DreamPath type;

            if (typeValue.TryGetValueAsDreamObject(out DreamObject typeObject)) {
                if (typeObject == null) {
                    interpreter.Push(new DreamValue(0));

                    return;
                }

                type = typeObject.ObjectDefinition.Type;
            } else {
                type = typeValue.GetValueAsPath();
            }

            if (value.TryGetValueAsDreamObject(out DreamObject dreamObject) && dreamObject != null) {
                interpreter.Push(new DreamValue(dreamObject.IsSubtypeOf(type) ? 1 : 0));
            } else {
                interpreter.Push(new DreamValue(0));
            }
        }
        #endregion Comparisons

        #region Flow
        public static void Call(DreamProcInterpreter interpreter) {
            DreamProcArguments arguments = interpreter.PopArguments();
            DreamProcIdentifierProc procIdentifier = (DreamProcIdentifierProc)interpreter.PopIdentifier();
            DreamProc proc = procIdentifier.GetValue().GetValueAsProc();

            try {
                interpreter.Push(interpreter.RunProc(proc, procIdentifier.Instance, arguments));
            } catch (Exception e) {
                throw new Exception("Exception while running proc '" + procIdentifier.ProcName + "' on object of type '" + procIdentifier.Instance.ObjectDefinition.Type + "': " + e.Message, e);
            }
        }
        
        public static void CallSelf(DreamProcInterpreter interpreter) {
            DreamProcArguments arguments = interpreter.PopArguments();

            try {
                interpreter.Push(interpreter.RunProc(interpreter.SelfProc, interpreter.Instance, arguments));
            } catch (Exception e) {
                throw new Exception("Exception while running proc '.' on object of type '" + interpreter.Instance.ObjectDefinition.Type + "': " + e.Message, e);
            }
        }

        public static void CallStatement(DreamProcInterpreter interpreter) {
            DreamProcArguments arguments = interpreter.PopArguments();
            DreamValue source = interpreter.PopDreamValue();

            if (source.Type == DreamValue.DreamValueType.DreamObject) {
                DreamObject dreamObject = source.GetValueAsDreamObject();
                DreamValue procId = interpreter.PopDreamValue();
                DreamProc proc = null;

                if (procId.Type == DreamValue.DreamValueType.String) {
                    proc = dreamObject.GetProc(procId.GetValueAsString());
                } else if (procId.Type == DreamValue.DreamValueType.DreamPath) {
                    DreamPath fullProcPath = procId.GetValueAsPath();
                    int procElementIndex = fullProcPath.FindElement("proc");

                    if (procElementIndex != -1) {
                        DreamPath procPath = fullProcPath.FromElements(procElementIndex + 1);
                        string procName = procPath.LastElement;

                        proc = dreamObject.GetProc(procName);
                    }
                }

                if (proc != null) {
                    try {
                        interpreter.Push(interpreter.RunProc(proc, dreamObject, arguments));
                    } catch (Exception e) {
                        throw new Exception("Exception while running proc " + procId + " on object of type '" + dreamObject.ObjectDefinition.Type + "': " + e.Message, e);
                    }
                } else {

                    throw new Exception("Invalid proc (" + procId + ")");
                }
            } else if (source.Type == DreamValue.DreamValueType.DreamPath) {
                DreamPath fullProcPath = source.GetValueAsPath();
                if (fullProcPath.Elements.Length != 2) throw new Exception("Invalid call() proc \"" + fullProcPath + "\"");
                string procName = fullProcPath.LastElement;
                DreamProc proc = interpreter.Instance.GetProc(procName);

                try {
                    interpreter.Push(interpreter.RunProc(proc, interpreter.Instance, arguments));
                } catch (Exception e) {
                    throw new Exception("Exception while running proc " + fullProcPath + " on object of type '" + interpreter.Instance.ObjectDefinition.Type + "': " + e.Message, e);
                }
            } else {
                throw new Exception("Call statement has an invalid source (" + source + ")");
            }
        }

        public static void Error(DreamProcInterpreter interpreter) {
            throw new Exception("Reached an error opcode");
        }

        public static void Jump(DreamProcInterpreter interpreter) {
            int position = interpreter.ReadInt();

            interpreter.JumpTo(position);
        }

        public static void JumpIfFalse(DreamProcInterpreter interpreter) {
            int position = interpreter.ReadInt();
            DreamValue value = interpreter.PopDreamValue();

            if (!IsTruthy(value)) {
                interpreter.JumpTo(position);
            }
        }

        public static void JumpIfTrue(DreamProcInterpreter interpreter) {
            int position = interpreter.ReadInt();
            DreamValue value = interpreter.PopDreamValue();

            if (IsTruthy(value)) {
                interpreter.JumpTo(position);
            }
        }

        public static void Return(DreamProcInterpreter interpreter) {
            interpreter.DefaultReturnValue = interpreter.PopDreamValue();

            interpreter.End();
        }

        public static void SwitchCase(DreamProcInterpreter interpreter) {
            int casePosition = interpreter.ReadInt();
            DreamValue testValue = interpreter.PopDreamValue();
            DreamValue value = interpreter.PopDreamValue();

            if (IsEqual(value, testValue)) {
                interpreter.JumpTo(casePosition);
            } else {
                interpreter.Push(value);
            }
        }
        #endregion Flow

        #region Others
        public static void Browse(DreamProcInterpreter interpreter) {
            string options = interpreter.PopDreamValue().GetValueAsString();
            DreamValue body = interpreter.PopDreamValue();
            DreamObject receiver = interpreter.PopDreamValue().GetValueAsDreamObject();

            DreamObject client;
            if (receiver.IsSubtypeOf(DreamPath.Mob)) {
                client = receiver.GetVariable("client").GetValueAsDreamObject();
            } else if (receiver.IsSubtypeOf(DreamPath.Client)) {
                client = receiver;
            } else {
                throw new Exception("Invalid browse() recipient");
            }

            if (client != null) {
                DreamConnection connection = Program.ClientToConnection[client];

                string browseValue;
                if (body.Type == DreamValue.DreamValueType.DreamResource) {
                    browseValue = body.GetValueAsDreamResource().ReadAsString();
                } else {
                    browseValue = (string)body.Value;
                }

                connection.Browse(browseValue, options);
            }
        }

        public static void BrowseResource(DreamProcInterpreter interpreter) {
            DreamValue filename = interpreter.PopDreamValue();
            DreamResource file = interpreter.PopDreamValue().GetValueAsDreamResource();
            DreamObject receiver = interpreter.PopDreamValue().GetValueAsDreamObject();

            DreamObject client;
            if (receiver.IsSubtypeOf(DreamPath.Mob)) {
                client = receiver.GetVariable("client").GetValueAsDreamObject();
            } else if (receiver.IsSubtypeOf(DreamPath.Client)) {
                client = receiver;
            } else {
                throw new Exception("Invalid browse_rsc() recipient");
            }

            if (client != null) {
                DreamConnection connection = Program.ClientToConnection[client];

                connection.BrowseResource(file, (filename.Value != null) ? filename.GetValueAsString() : Path.GetFileName(file.ResourcePath));
            }
        }

        public static void DeleteObject(DreamProcInterpreter interpreter) {
            DreamObject dreamObject = interpreter.PopDreamValue().GetValueAsDreamObject();

            if (dreamObject != null) {
                dreamObject.Delete();
            }
        }

        public static void OutputControl(DreamProcInterpreter interpreter) {
            string control = interpreter.PopDreamValue().GetValueAsString();
            DreamValue message = interpreter.PopDreamValue();
            DreamObject receiver = interpreter.PopDreamValue().GetValueAsDreamObject();

            DreamObject client;
            if (receiver.IsSubtypeOf(DreamPath.Mob)) {
                client = receiver.GetVariable("client").GetValueAsDreamObject();
            } else if (receiver.IsSubtypeOf(DreamPath.Client)) {
                client = receiver;
            } else {
                throw new Exception("Invalid output() recipient");
            }

            if (client != null) {
                DreamConnection connection = Program.ClientToConnection[client];

                if (message.Type != DreamValue.DreamValueType.String && message.Value != null) throw new Exception("Invalid output() message " + message);
                connection.OutputControl((string)message.Value, control);
            }
        }

        public static void Prompt(DreamProcInterpreter interpreter) {
            DMValueType types = (DMValueType)interpreter.ReadInt();
            DreamProcArguments arguments = interpreter.PopArguments();
            DreamValue firstArg = arguments.OrderedArguments[0];
            DreamObject recipientMob;
            string message;

            if (firstArg.TryGetValueAsDreamObjectOfType(DreamPath.Mob, out recipientMob)) {
                message = arguments.OrderedArguments[1].GetValueAsString();
            } else {
                recipientMob = interpreter.Usr;
                message = arguments.OrderedArguments[0].GetValueAsString();
            }

            DreamObject clientObject;
            if (recipientMob != null && recipientMob.GetVariable("client").TryGetValueAsDreamObjectOfType(DreamPath.Client, out clientObject)) {
                DreamConnection connection = Program.ClientToConnection[clientObject];
                Task<DreamValue> promptTask = connection.Prompt(types, message);

                promptTask.Wait();
                interpreter.Push(promptTask.Result);
            }
        }

        public static void LocateCoord(DreamProcInterpreter interpreter) {
            int z = interpreter.PopDreamValue().GetValueAsInteger();
            int y = interpreter.PopDreamValue().GetValueAsInteger();
            int x = interpreter.PopDreamValue().GetValueAsInteger();

            interpreter.Push(new DreamValue(Program.DreamMap.GetTurfAt(x, y, z)));
        }
        
        public static void Locate(DreamProcInterpreter interpreter) {
            DreamObject container = interpreter.PopDreamValue().GetValueAsDreamObject();
            DreamValue value = interpreter.PopDreamValue();

            DreamList containerList;
            if (container != null && container.IsSubtypeOf(DreamPath.Atom)) {
                containerList = container.GetVariable("contents").GetValueAsDreamList();
            } else {
                containerList = container as DreamList;
            }

            if (value.TryGetValueAsString(out string refString)) {
                int refID = int.Parse(refString);

                interpreter.Push(new DreamValue(DreamObject.GetFromReferenceID(refID)));
            } else if (value.TryGetValueAsPath(out DreamPath type)) {
                if (containerList == null) {
                    interpreter.Push(DreamValue.Null);

                    return;
                }

                foreach (DreamValue containerItem in containerList.GetValues()) {
                    if (!containerItem.TryGetValueAsDreamObject(out DreamObject dmObject)) continue;

                    if (dmObject.IsSubtypeOf(type)) {
                        interpreter.Push(containerItem);

                        return;
                    }
                }

                interpreter.Push(DreamValue.Null);
            } else {
                if (containerList == null) {
                    interpreter.Push(DreamValue.Null);

                    return;
                }

                foreach (DreamValue containerItem in containerList.GetValues()) {
                    if (IsEqual(containerItem, value)) {
                        interpreter.Push(containerItem);

                        return;
                    }
                }

                interpreter.Push(DreamValue.Null);
            }
        }
        #endregion Others

        #region Helpers
        private static bool IsTruthy(DreamValue value) {
            if (value.Type == DreamValue.DreamValueType.DreamObject) {
                return (value.GetValueAsDreamObject() != null);
            } else if (value.Type == DreamValue.DreamValueType.DreamResource) {
                return true;
            } else if (value.Type == DreamValue.DreamValueType.DreamPath) {
                return true;
            } else if (value.Type == DreamValue.DreamValueType.Integer) {
                return (value.GetValueAsInteger() != 0);
            } else if (value.Type == DreamValue.DreamValueType.Float) {
                return (value.GetValueAsFloat() != 0);
            } else if (value.Type == DreamValue.DreamValueType.String) {
                return (value.GetValueAsString() != "");
            } else if (value.Type == DreamValue.DreamValueType.DreamProc) {
                return value.Value != null;
            } else {
                throw new NotImplementedException("Truthy evaluation for " + value.Type + " is not implemented");
            }
        }

        private static bool IsEqual(DreamValue first, DreamValue second) {
            if (first.Type == DreamValue.DreamValueType.DreamObject && second.Type == DreamValue.DreamValueType.DreamObject) {
                return first.GetValueAsDreamObject() == second.GetValueAsDreamObject();
            } else if (first.Type == DreamValue.DreamValueType.DreamObject && second.Type == DreamValue.DreamValueType.String) {
                return false;
            } else if (first.Type == DreamValue.DreamValueType.DreamObject && second.Type == DreamValue.DreamValueType.Integer) {
                return false;
            } else if (first.Type == DreamValue.DreamValueType.DreamObject && second.Type == DreamValue.DreamValueType.Float) {
                return false;
            } else if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Integer) {
                return first.GetValueAsInteger() == second.GetValueAsInteger();
            } else if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Float) {
                return first.GetValueAsInteger() == second.GetValueAsFloat();
            } else if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.DreamObject) {
                return false;
            } else if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.String) {
                return false;
            } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Float) {
                return first.GetValueAsFloat() == second.GetValueAsFloat();
            } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Integer) {
                return first.GetValueAsFloat() == second.GetValueAsInteger();
            } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.DreamObject) {
                return false;
            } else if (first.Type == DreamValue.DreamValueType.String && second.Type == DreamValue.DreamValueType.Integer) {
                return false;
            } else if (first.Type == DreamValue.DreamValueType.String && second.Type == DreamValue.DreamValueType.String) {
                return first.GetValueAsString() == second.GetValueAsString();
            } else if (first.Type == DreamValue.DreamValueType.String && second.Type == DreamValue.DreamValueType.DreamObject) {
                return false;
            } else if (first.Type == DreamValue.DreamValueType.DreamPath && second.Type == DreamValue.DreamValueType.DreamPath) {
                return first.GetValueAsPath().Equals(second.GetValueAsPath());
            } else if (first.Type == DreamValue.DreamValueType.DreamPath && second.Type == DreamValue.DreamValueType.DreamObject) {
                return false;
            } else if (first.Type == DreamValue.DreamValueType.DreamPath && second.Type == DreamValue.DreamValueType.String) {
                return false;
            } else if (first.Type == DreamValue.DreamValueType.DreamResource && second.Type == DreamValue.DreamValueType.DreamResource) {
                return first.GetValueAsDreamResource().ResourcePath == second.GetValueAsDreamResource().ResourcePath;
            } else if (first.Type == DreamValue.DreamValueType.DreamResource && second.Type != DreamValue.DreamValueType.DreamResource) {
                return false;
            } else if (first.Value == null) {
                return second.Value == null;
            } else {
                throw new NotImplementedException("Equal comparison for " + first + " and " + second + " is not implemented");
            }
        }

        private static bool IsGreaterThan(DreamValue first, DreamValue second) {
            if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Integer) {
                return first.GetValueAsInteger() > second.GetValueAsInteger();
            } else if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Float) {
                return first.GetValueAsInteger() > second.GetValueAsFloat();
            } else if (first.Type == DreamValue.DreamValueType.Integer && second.Value == null) {
                return first.GetValueAsInteger() > 0;
            } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Float) {
                return first.GetValueAsFloat() > second.GetValueAsFloat();
            } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Integer) {
                return first.GetValueAsFloat() > second.GetValueAsInteger();
            } else if (first.Value == null && second.Type == DreamValue.DreamValueType.Integer) {
                return 0 > second.GetValueAsInteger();
            } else if (first.Value == null && second.Type == DreamValue.DreamValueType.Float) {
                return 0 > second.GetValueAsFloat();
            } else {
                throw new Exception("Invalid greater than comparison on " + first + " and " + second);
            }
        }

        private static bool IsLessThan(DreamValue first, DreamValue second) {
            if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Integer) {
                return first.GetValueAsInteger() < second.GetValueAsInteger();
            } else if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Float) {
                return first.GetValueAsInteger() < second.GetValueAsFloat();
            } else if (first.Type == DreamValue.DreamValueType.Integer && second.Value == null) {
                return first.GetValueAsInteger() < 0;
            } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Integer) {
                return first.GetValueAsFloat() < second.GetValueAsInteger();
            } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Float) {
                return first.GetValueAsFloat() < second.GetValueAsFloat();
            } else if (first.Type == DreamValue.DreamValueType.Float && second.Value == null) {
                return first.GetValueAsFloat() < 0;
            } else if (first.Value == null && second.Type == DreamValue.DreamValueType.Integer) {
                return 0 < second.GetValueAsInteger();
            } else {
                throw new Exception("Invalid less than comparison between " + first + " and " + second);
            }
        }
        #endregion Helpers
    }
}
