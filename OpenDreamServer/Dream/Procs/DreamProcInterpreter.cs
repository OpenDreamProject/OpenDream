using OpenDreamServer.Dream.Objects;
using OpenDreamServer.Dream.Objects.MetaObjects;
using OpenDreamServer.Net;
using OpenDreamServer.Resources;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace OpenDreamServer.Dream.Procs {
    class DreamProcInterpreter {
        private struct DreamProcInterpreterArguments {
            public List<object> OrderedArguments;
            public Dictionary<string, object> NamedArguments;

            public DreamProcInterpreterArguments(List<object> orderedArguments, Dictionary<string, object> namedArguments) {
                OrderedArguments = orderedArguments;
                NamedArguments = namedArguments;
            }

            public DreamProcArguments CreateProcArguments() {
                List<DreamValue> procOrderedArguments = new List<DreamValue>();
                Dictionary<string, DreamValue> procNamedArguments = new Dictionary<string, DreamValue>();

                foreach (object orderedArgument in OrderedArguments) {
                    if (orderedArgument is DreamValue) {
                        procOrderedArguments.Add((DreamValue)orderedArgument);
                    } else if (orderedArgument is IDreamProcIdentifier) {
                        procOrderedArguments.Add(((IDreamProcIdentifier)orderedArgument).GetValue());
                    } else {
                        throw new Exception("Argument was not a " + nameof(DreamValue) + " or " + nameof(IDreamProcIdentifier) + "!");
                    }
                }

                foreach (KeyValuePair<string, object> namedArgument in NamedArguments) {
                    if (namedArgument.Value is DreamValue) {
                        procNamedArguments.Add(namedArgument.Key, (DreamValue)namedArgument.Value);
                    } else if (namedArgument.Value is IDreamProcIdentifier) {
                        procNamedArguments.Add(namedArgument.Key, ((IDreamProcIdentifier)namedArgument.Value).GetValue());
                    } else {
                        throw new Exception("Argument '" + namedArgument.Key + "' was not a " + nameof(DreamValue) + " or " + nameof(IDreamProcIdentifier) + "!");
                    }
                }

                return new DreamProcArguments(procOrderedArguments, procNamedArguments);
            }
        }

        public DreamValue DefaultReturnValue = new DreamValue((DreamObject)null);

        private MemoryStream _bytecodeStream;
        private BinaryReader _binaryReader;
        private DreamProc _selfProc;
        private DreamProcArguments _arguments;
        private DreamProcScope _topScope;
        private Stack<object> _stack = new();
        private Stack<DreamProcScope> _scopeStack = new();
        private DreamProcScope _currentScope = null;
        private Stack<DreamProcListEnumerator> _listEnumeratorStack = new();
        private Dictionary<int, DreamValue> _localVariables = new();

        public DreamProcInterpreter(DreamProc selfProc, byte[] bytecode) {
            _bytecodeStream = new MemoryStream(bytecode);
            _binaryReader = new BinaryReader(_bytecodeStream);
            _selfProc = selfProc;
        }

        public DreamValue Run(DreamProcScope scope, DreamProcArguments arguments) {
            _arguments = arguments;
            _topScope = scope;

            PushScope(scope);
            while (_bytecodeStream.Position < _bytecodeStream.Length) {
                if (Step() == 1) break;
            }

            DreamValue returnValue = DefaultReturnValue;

            ResetState();
            return returnValue;
        }

        private int Step() {
            DreamProcOpcode opcode = (DreamProcOpcode)_bytecodeStream.ReadByte();

            if (opcode == DreamProcOpcode.BitShiftLeft) {
                DreamValue second = PopDreamValue();
                DreamValue first = PopDreamValue();

                if (first.Type == DreamValue.DreamValueType.DreamObject && first.Value != null) { //Output operation
                    IDreamMetaObject metaObject = first.GetValueAsDreamObject().ObjectDefinition.MetaObject;

                    if (metaObject != null) {
                        Push(metaObject.OperatorOutput(first, second));
                    } else {
                        throw new Exception("Invalid output operation on " + first + " and " + second);
                    }
                } else if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Integer) {
                    Push(new DreamValue(first.GetValueAsInteger() << second.GetValueAsInteger()));
                } else {
                    throw new Exception("Invalid bit shift left operation on " + first + " and " + second);
                }
            } else if (opcode == DreamProcOpcode.GetIdentifier) {
                string identifierName = ReadString();

                if (identifierName == "args") {
                    DreamList argsList = _arguments.CreateDreamList();

                    argsList.ValueAssigned += OnArgsListValueAssigned;
                    Push(new DreamValue(argsList));
                } else if (identifierName == "usr") {
                    Push(new DreamValue(_currentScope.Usr));
                } else {
                    Push(new DreamProcIdentifierVariable(_currentScope, identifierName));
                }
            } else if (opcode == DreamProcOpcode.PushString) {
                Push(new DreamValue(ReadString()));
            } else if (opcode == DreamProcOpcode.FormatString) {
                string unformattedString = ReadString();
                string formattedString = String.Empty;

                for (int i = 0; i < unformattedString.Length; i++) {
                    char c = unformattedString[i];

                    if (c == (char)0xFF) {
                        c = unformattedString[++i];

                        switch ((StringFormatTypes)c) {
                            case StringFormatTypes.Stringify: {
                                DreamValue value = PopDreamValue();

                                formattedString += value.Stringify();
                                break;
                            }
                            case StringFormatTypes.Ref: {
                                DreamObject refObject = PopDreamValue().GetValueAsDreamObject();

                                formattedString += DreamObject.CreateReferenceID(refObject);
                                break;
                            }
                            default: throw new Exception("Invalid special character");
                        }
                    } else {
                        formattedString += c;
                    }
                }

                Push(new DreamValue(formattedString));
            } else if (opcode == DreamProcOpcode.PushInt) {
                int value = ReadInt();

                Push(new DreamValue(value));
            } else if (opcode == DreamProcOpcode.SetLocalVariable) {
                int variableId = ReadByte();
                DreamValue value = PopDreamValue();

                _localVariables[variableId] = value;
            } else if (opcode == DreamProcOpcode.PushPath) {
                DreamPath path = new DreamPath(ReadString());

                Push(new DreamValue(path));
            } else if (opcode == DreamProcOpcode.Add) {
                DreamValue second = PopDreamValue();
                DreamValue first = PopDreamValue();

                if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Integer) {
                    Push(new DreamValue(first.GetValueAsInteger() + second.GetValueAsInteger()));
                } else if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Float) {
                    Push(new DreamValue(first.GetValueAsInteger() + second.GetValueAsFloat()));
                } else if (first.Type == DreamValue.DreamValueType.Integer && second.Value == null) {
                    Push(new DreamValue(first.GetValueAsInteger()));
                } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Integer) {
                    Push(new DreamValue(first.GetValueAsFloat() + second.GetValueAsInteger()));
                } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Float) {
                    Push(new DreamValue(first.GetValueAsFloat() + second.GetValueAsFloat()));
                } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.String) {
                    Push(new DreamValue(first.GetValueAsFloat() + second.GetValueAsString()));
                } else if (first.Type == DreamValue.DreamValueType.String && second.Type == DreamValue.DreamValueType.String) {
                    Push(new DreamValue(first.GetValueAsString() + second.GetValueAsString()));
                } else if (first.Type == DreamValue.DreamValueType.String && second.Type == DreamValue.DreamValueType.Float) {
                    Push(new DreamValue(first.GetValueAsString() + second.GetValueAsFloat()));
                } else if (first.Type == DreamValue.DreamValueType.DreamObject && first.Value != null) {
                    IDreamMetaObject metaObject = first.GetValueAsDreamObject().ObjectDefinition.MetaObject;

                    if (metaObject != null) {
                        Push(metaObject.OperatorAdd(first, second));
                    } else {
                        throw new Exception("Invalid add operation on " + first + " and " + second);
                    }
                } else if (first.Value == null) {
                    Push(second);
                } else {
                    throw new Exception("Invalid add operation on " + first + " and " + second);
                }
            } else if (opcode == DreamProcOpcode.Assign) {
                DreamValue value = PopDreamValue();
                IDreamProcIdentifier identifier = PopIdentifier();

                identifier.Assign(value);
                Push(value);
            } else if (opcode == DreamProcOpcode.Call) {
                DreamProcInterpreterArguments arguments = PopArguments();
                IDreamProcIdentifier procIdentifier = PopIdentifier();

                if (procIdentifier is DreamProcIdentifierProc) {
                    DreamProcIdentifierProc identifier = (DreamProcIdentifierProc)procIdentifier;

                    if (identifier.IdentifierName == "initial") {
                        object argument = arguments.OrderedArguments[0];

                        if (argument is DreamProcIdentifierVariable) {
                            DreamProcIdentifierVariable varIdentifier = (DreamProcIdentifierVariable)argument;
                            DreamObjectDefinition objectDefinition = varIdentifier.HoldingScope.DreamObject.ObjectDefinition;

                            if (objectDefinition.Variables.ContainsKey(varIdentifier.IdentifierName)) {
                                Push(objectDefinition.Variables[varIdentifier.IdentifierName]);
                            } else {
                                throw new NotImplementedException("Initial is not implemented for variables not belonging to an object");
                            }
                        } else {
                            throw new Exception("Initial proc must be given a variable");
                        }
                    } else {
                        DreamProc proc = identifier.GetValue().GetValueAsProc();
                        DreamProcArguments procArguments = arguments.CreateProcArguments();
                        if (proc == _currentScope.SuperProc && procArguments.ArgumentCount == 0) procArguments = _arguments;

                        try {
                            Push(proc.Run(identifier.HoldingScope.DreamObject, procArguments, _currentScope.Usr));
                        } catch (Exception e) {
                            throw new Exception("Exception while running proc '" + identifier.IdentifierName + "' on object of type '" + identifier.HoldingScope.DreamObject.ObjectDefinition.Type + "': " + e.Message, e);
                        }
                    }
                } else if (procIdentifier is DreamProcIdentifierSelfProc) {
                    try {
                        Push(_selfProc.Run(_currentScope.DreamObject, arguments.CreateProcArguments(), _currentScope.Usr));
                    } catch (Exception e) {
                        throw new Exception("Exception while running proc '.' on object of type '" + _currentScope.DreamObject.ObjectDefinition.Type + "': " + e.Message, e);
                    }
                } else {
                    throw new Exception("Call on an invalid identifier");
                }
            } else if (opcode == DreamProcOpcode.Dereference) {
                DreamObject dreamObject = PopDreamValue().GetValueAsDreamObject();
                string identifierName = ReadString();

                if (dreamObject == null) throw new Exception("Cannot dereference '" + identifierName + "' on a null object");
                if (dreamObject.HasVariable(identifierName) || dreamObject.ObjectDefinition.HasGlobalVariable(identifierName) || dreamObject.HasProc(identifierName)) {
                    Push(new DreamProcIdentifierVariable(new DreamProcScope(dreamObject, _currentScope.Usr), identifierName));
                } else {
                    throw new Exception("Object " + dreamObject + " has no identifier named '" + identifierName + "'");
                }
            } else if (opcode == DreamProcOpcode.JumpIfFalse) {
                int position = ReadInt();
                DreamValue value = PopDreamValue();

                if (!IsTruthy(value)) {
                    _bytecodeStream.Seek(position, SeekOrigin.Begin);
                }
            } else if (opcode == DreamProcOpcode.JumpIfTrue) {
                int position = ReadInt();
                DreamValue value = PopDreamValue();

                if (IsTruthy(value)) {
                    _bytecodeStream.Seek(position, SeekOrigin.Begin);
                }
            } else if (opcode == DreamProcOpcode.Jump) {
                int position = ReadInt();

                _bytecodeStream.Seek(position, SeekOrigin.Begin);
            } else if (opcode == DreamProcOpcode.CompareEquals) {
                DreamValue second = PopDreamValue();
                DreamValue first = PopDreamValue();

                Push(new DreamValue(IsEqual(first, second) ? 1 : 0));
            } else if (opcode == DreamProcOpcode.Return) {
                DreamValue returnValue = PopDreamValue();

                DefaultReturnValue = returnValue;
                return 1;
            } else if (opcode == DreamProcOpcode.PushNull) {
                Push(new DreamValue((DreamObject)null));
            } else if (opcode == DreamProcOpcode.Subtract) {
                DreamValue second = PopDreamValue();
                DreamValue first = PopDreamValue();

                if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Integer) {
                    Push(new DreamValue(first.GetValueAsInteger() - second.GetValueAsInteger()));
                } else if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Float) {
                    Push(new DreamValue(first.GetValueAsInteger() - second.GetValueAsFloat()));
                } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Float) {
                    Push(new DreamValue(first.GetValueAsFloat() - second.GetValueAsFloat()));
                } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Integer) {
                    Push(new DreamValue(first.GetValueAsFloat() - second.GetValueAsInteger()));
                } else if (first.Value == null && second.Type == DreamValue.DreamValueType.Integer) {
                    Push(new DreamValue(-second.GetValueAsInteger()));
                } else if (first.Type == DreamValue.DreamValueType.DreamObject && first.Value != null) {
                    IDreamMetaObject metaObject = first.GetValueAsDreamObject().ObjectDefinition.MetaObject;

                    if (metaObject != null) {
                        Push(metaObject.OperatorSubtract(first, second));
                    } else {
                        throw new Exception("Invalid subtract operation on " + first + " and " + second);
                    }
                } else {
                    throw new Exception("Invalid subtract operation on " + first + " and " + second);
                }
            } else if (opcode == DreamProcOpcode.CompareLessThan) {
                DreamValue second = PopDreamValue();
                DreamValue first = PopDreamValue();

                Push(new DreamValue(IsLessThan(first, second) ? 1 : 0));
            } else if (opcode == DreamProcOpcode.CompareGreaterThan) {
                DreamValue second = PopDreamValue();
                DreamValue first = PopDreamValue();

                Push(new DreamValue(IsGreaterThan(first, second) ? 1 : 0));
            } else if (opcode == DreamProcOpcode.BooleanAnd) {
                DreamValue a = PopDreamValue();
                int jumpPosition = ReadInt();

                if (!IsTruthy(a)) {
                    Push(a);
                    _bytecodeStream.Seek(jumpPosition, SeekOrigin.Begin);
                }
            } else if (opcode == DreamProcOpcode.BooleanNot) {
                DreamValue value = PopDreamValue();

                Push(new DreamValue(IsTruthy(value) ? 0 : 1));
            } else if (opcode == DreamProcOpcode.PushSuperProc) {
                Push(new DreamProcIdentifierProc(_currentScope, ".."));
            } else if (opcode == DreamProcOpcode.Negate) {
                DreamValue value = PopDreamValue();

                if (value.Type == DreamValue.DreamValueType.Integer) {
                    Push(new DreamValue(-value.GetValueAsInteger()));
                } else if (value.Type == DreamValue.DreamValueType.Float) {
                    Push(new DreamValue(-value.GetValueAsFloat()));
                }
            } else if (opcode == DreamProcOpcode.Modulus) {
                DreamValue second = PopDreamValue();
                DreamValue first = PopDreamValue();

                if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Integer) {
                    Push(new DreamValue(first.GetValueAsInteger() % second.GetValueAsInteger()));
                } else {
                    throw new Exception("Invalid multiply operation on " + first + " and " + second);
                }
            } else if (opcode == DreamProcOpcode.Append) {
                DreamValue second = PopDreamValue();
                IDreamProcIdentifier identifier = PopIdentifier();
                DreamValue first = identifier.GetValue();

                if (first.Type == DreamValue.DreamValueType.DreamObject) {
                    if (first.Value != null) {
                        IDreamMetaObject metaObject = first.GetValueAsDreamObject().ObjectDefinition.MetaObject;

                        if (metaObject != null) {
                            Push(metaObject.OperatorAppend(first, second));
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
            } else if (opcode == DreamProcOpcode.CreateScope) {
                PushScope(new DreamProcScope(_currentScope));
            } else if (opcode == DreamProcOpcode.DestroyScope) {
                PopScope();
            } else if (opcode == DreamProcOpcode.CompareLessThanOrEqual) {
                DreamValue second = PopDreamValue();
                DreamValue first = PopDreamValue();

                Push(new DreamValue((IsEqual(first, second) || IsLessThan(first, second)) ? 1 : 0));
            } else if (opcode == DreamProcOpcode.IndexList) {
                DreamValue index = PopDreamValue();
                DreamList list = PopDreamValue().GetValueAsDreamList();

                Push(new DreamProcIdentifierListIndex(list, index));
            } else if (opcode == DreamProcOpcode.Remove) {
                DreamValue second = PopDreamValue();
                IDreamProcIdentifier identifier = PopIdentifier();
                DreamValue first = identifier.GetValue();

                if (first.Type == DreamValue.DreamValueType.DreamObject) {
                    if (first.Value != null) {
                        IDreamMetaObject metaObject = first.GetValueAsDreamObject().ObjectDefinition.MetaObject;

                        if (metaObject != null) {
                            Push(metaObject.OperatorRemove(first, second));
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
                } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Float) {
                    identifier.Assign(new DreamValue(first.GetValueAsFloat() - second.GetValueAsFloat()));
                } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Integer) {
                    identifier.Assign(new DreamValue(first.GetValueAsFloat() - second.GetValueAsInteger()));
                } else {
                    throw new Exception("Invalid remove operation on " + first + " and " + second);
                }
            } else if (opcode == DreamProcOpcode.DeleteObject) {
                DreamObject dreamObject = PopDreamValue().GetValueAsDreamObject();

                if (dreamObject != null) {
                    dreamObject.Delete();
                }
            } else if (opcode == DreamProcOpcode.PushResource) {
                string resourcePath = ReadString();

                Push(new DreamValue(Program.DreamResourceManager.LoadResource(resourcePath)));
            } else if (opcode == DreamProcOpcode.CreateList) {
                Push(new DreamValue(Program.DreamObjectTree.CreateObject(DreamPath.List)));
            } else if (opcode == DreamProcOpcode.CallStatement) {
                DreamProcInterpreterArguments arguments = PopArguments();
                DreamValue source = PopDreamValue();

                if (source.Type == DreamValue.DreamValueType.DreamObject) {
                    DreamObject dreamObject = source.GetValueAsDreamObject();
                    DreamValue procId = PopDreamValue();
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
                            Push(proc.Run(dreamObject, arguments.CreateProcArguments(), _currentScope.Usr));
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
                    DreamProc proc = _topScope.GetProc(procName).GetValueAsProc();

                    try {
                        Push(proc.Run(_topScope.DreamObject, arguments.CreateProcArguments(), _currentScope.Usr));
                    } catch (Exception e) {
                        throw new Exception("Exception while running proc " + fullProcPath + " on object of type '" + _topScope.DreamObject.ObjectDefinition.Type + "': " + e.Message, e);
                    }
                } else {
                    throw new Exception("Call statement has an invalid source (" + source + ")");
                }
            } else if (opcode == DreamProcOpcode.BitAnd) {
                DreamValue second = PopDreamValue();
                DreamValue first = PopDreamValue();

                if (first.TryGetValueAsDreamList(out DreamList list)) {
                    DreamList newList = Program.DreamObjectTree.CreateList();

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

                    Push(new DreamValue(newList));
                } else if (first.Value != null && second.Value != null) {
                    Push(new DreamValue(first.GetValueAsInteger() & second.GetValueAsInteger()));
                } else {
                    Push(new DreamValue(0));
                }
            } else if (opcode == DreamProcOpcode.CompareNotEquals) {
                DreamValue second = PopDreamValue();
                DreamValue first = PopDreamValue();

                Push(new DreamValue(IsEqual(first, second) ? 0 : 1));
            } else if (opcode == DreamProcOpcode.ListAppend) {
                DreamValue value = PopDreamValue();
                DreamList list = PopDreamValue().GetValueAsDreamList();

                list.AddValue(value);
                Push(new DreamValue(list));
            } else if (opcode == DreamProcOpcode.Divide) {
                DreamValue second = PopDreamValue();
                DreamValue first = PopDreamValue();

                if (first.Value == null) {
                    Push(new DreamValue(0));
                } else if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Integer) {
                    Push(new DreamValue(first.GetValueAsNumber() / second.GetValueAsNumber()));
                } else if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Float) {
                    Push(new DreamValue(first.GetValueAsNumber() / second.GetValueAsNumber()));
                } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Integer) {
                    Push(new DreamValue(first.GetValueAsNumber() / second.GetValueAsNumber()));
                } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Float) {
                    Push(new DreamValue(first.GetValueAsNumber() / second.GetValueAsNumber()));
                } else {
                    throw new Exception("Invalid divide operation on " + first + " and " + second);
                }
            } else if (opcode == DreamProcOpcode.Multiply) {
                DreamValue second = PopDreamValue();
                DreamValue first = PopDreamValue();

                if (first.Value == null || second.Value == null) {
                    Push(new DreamValue(0));
                } else if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Integer) {
                    Push(new DreamValue(first.GetValueAsInteger() * second.GetValueAsInteger()));
                } else if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Float) {
                    Push(new DreamValue(first.GetValueAsInteger() * second.GetValueAsFloat()));
                } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Integer) {
                    Push(new DreamValue(first.GetValueAsFloat() * second.GetValueAsInteger()));
                } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Float) {
                    Push(new DreamValue(first.GetValueAsFloat() * second.GetValueAsFloat()));
                } else {
                    throw new Exception("Invalid multiply operation on " + first + " and " + second);
                }
            } else if (opcode == DreamProcOpcode.PushSelf) {
                Push(new DreamProcIdentifierSelfProc(_selfProc, this));
            } else if (opcode == DreamProcOpcode.CreateObject) {
                DreamProcInterpreterArguments arguments = PopArguments();
                DreamPath objectPath = PopDreamValue().GetValueAsPath();

                if (objectPath.Type == DreamPath.PathType.Relative && objectPath.Elements.Length == 1) {
                    objectPath = _currentScope.GetValue(objectPath.LastElement).GetValueAsPath();
                }

                DreamObject newObject = Program.DreamObjectTree.CreateObject(objectPath, arguments.CreateProcArguments());
                Push(new DreamValue(newObject));
            } else if (opcode == DreamProcOpcode.BitXor) {
                int second = PopDreamValue().GetValueAsInteger();
                int first = PopDreamValue().GetValueAsInteger();

                Push(new DreamValue(first ^ second));
            } else if (opcode == DreamProcOpcode.BitOr) {
                int second = PopDreamValue().GetValueAsInteger();
                int first = PopDreamValue().GetValueAsInteger();

                Push(new DreamValue(first | second));
            } else if (opcode == DreamProcOpcode.BitNot) {
                int value = PopDreamValue().GetValueAsInteger();

                Push(new DreamValue((~value) & 0xFFFFFF));
            } else if (opcode == DreamProcOpcode.Combine) {
                DreamValue second = PopDreamValue();
                IDreamProcIdentifier identifier = PopIdentifier();
                DreamValue first = identifier.GetValue();

                if (first.Type == DreamValue.DreamValueType.DreamObject) {
                    if (first.Value != null) {
                        IDreamMetaObject metaObject = first.GetValueAsDreamObject().ObjectDefinition.MetaObject;

                        if (metaObject != null) {
                            Push(metaObject.OperatorCombine(first, second));
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
            } else if (opcode == DreamProcOpcode.BooleanOr) {
                DreamValue a = PopDreamValue();
                int jumpPosition = ReadInt();

                if (IsTruthy(a)) {
                    Push(a);
                    _bytecodeStream.Seek(jumpPosition, SeekOrigin.Begin);
                }
            } else if (opcode == DreamProcOpcode.PushArgumentList) {
                DreamProcInterpreterArguments arguments = new DreamProcInterpreterArguments(new List<object>(), new Dictionary<string, object>());
                DreamValue argListValue = PopDreamValue();

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

                Push(arguments);
            } else if (opcode == DreamProcOpcode.CompareGreaterThanOrEqual) {
                DreamValue second = PopDreamValue();
                DreamValue first = PopDreamValue();

                Push(new DreamValue((IsEqual(first, second) || IsGreaterThan(first, second)) ? 1 : 0));
            } else if (opcode == DreamProcOpcode.SwitchCase) {
                int casePosition = ReadInt();
                DreamValue testValue = PopDreamValue();
                DreamValue value = PopDreamValue();

                if (IsEqual(value, testValue)) {
                    _bytecodeStream.Seek(casePosition, SeekOrigin.Begin);
                } else {
                    Push(value);
                }
            } else if (opcode == DreamProcOpcode.Mask) {
                DreamValue second = PopDreamValue();
                IDreamProcIdentifier identifier = PopIdentifier();
                DreamValue first = identifier.GetValue();

                if (first.Type == DreamValue.DreamValueType.DreamObject) {
                    if (first.Value != null) {
                        IDreamMetaObject metaObject = first.GetValueAsDreamObject().ObjectDefinition.MetaObject;

                        if (metaObject != null) {
                            Push(metaObject.OperatorMask(first, second));
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
            } else if (opcode == DreamProcOpcode.ListAppendAssociated) {
                DreamValue index = PopDreamValue();
                DreamValue value = PopDreamValue();
                DreamList list = PopDreamValue().GetValueAsDreamList();

                list.SetValue(index, value);
                Push(new DreamValue(list));
            } else if (opcode == DreamProcOpcode.Error) {
                throw new Exception("Reached an error opcode");
            } else if (opcode == DreamProcOpcode.IsInList) {
                DreamValue listValue = PopDreamValue();
                DreamValue value = PopDreamValue();

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

                    Push(new DreamValue(list.ContainsValue(value) ? 1 : 0));
                } else {
                    Push(new DreamValue(0));
                }
            } else if (opcode == DreamProcOpcode.PushArguments) {
                DreamProcInterpreterArguments arguments = new DreamProcInterpreterArguments(new List<object>(), new Dictionary<string, object>());
                int argumentCount = ReadInt();
                object[] argumentValues = new object[argumentCount];

                for (int i = argumentCount - 1; i >= 0; i--) {
                    argumentValues[i] = _stack.Pop();
                }

                for (int i = 0; i < argumentCount; i++) {
                    DreamProcOpcodeParameterType argumentType = (DreamProcOpcodeParameterType)_bytecodeStream.ReadByte();

                    if (argumentType == DreamProcOpcodeParameterType.Named) {
                        string argumentName = ReadString();

                        arguments.NamedArguments[argumentName] = argumentValues[i];
                    } else if (argumentType == DreamProcOpcodeParameterType.Unnamed) {
                        arguments.OrderedArguments.Add(argumentValues[i]);
                    } else {
                        throw new Exception("Invalid argument type (" + argumentType + ")");
                    }
                }

                Push(arguments);
            } else if (opcode == DreamProcOpcode.PushFloat) {
                Push(new DreamValue(_binaryReader.ReadSingle()));
            } else if (opcode == DreamProcOpcode.PushSrc) {
                Push(new DreamValue(_currentScope.DreamObject));
            } else if (opcode == DreamProcOpcode.CreateListEnumerator) {
                DreamObject listObject = PopDreamValue().GetValueAsDreamObject();
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

                if (list != null) list = list.CreateCopy();
                _listEnumeratorStack.Push(new DreamProcListEnumerator(list));
            } else if (opcode == DreamProcOpcode.EnumerateList) {
                int outputVarId = ReadByte();
                DreamProcListEnumerator listEnumerator = _listEnumeratorStack.Peek();
                bool successfulEnumeration = listEnumerator.TryMoveNext(out DreamValue newValue);

                Push(new DreamValue(successfulEnumeration ? 1 : 0));
                if (successfulEnumeration) {
                    _localVariables[outputVarId] = newValue;
                }
            } else if (opcode == DreamProcOpcode.DestroyListEnumerator) {
                _listEnumeratorStack.Pop();
            } else if (opcode == DreamProcOpcode.Browse) {
                string options = PopDreamValue().GetValueAsString();
                DreamValue body = PopDreamValue();
                DreamObject receiver = PopDreamValue().GetValueAsDreamObject();

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
            } else if (opcode == DreamProcOpcode.BrowseResource) {
                DreamValue filename = PopDreamValue();
                DreamResource file = PopDreamValue().GetValueAsDreamResource();
                DreamObject receiver = PopDreamValue().GetValueAsDreamObject();

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
            } else if (opcode == DreamProcOpcode.OutputControl) {
                string control = PopDreamValue().GetValueAsString();
                DreamValue message = PopDreamValue();
                DreamObject receiver = PopDreamValue().GetValueAsDreamObject();

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
            } else if (opcode == DreamProcOpcode.BitShiftRight) {
                DreamValue second = PopDreamValue();
                DreamValue first = PopDreamValue();

                if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Integer) {
                    Push(new DreamValue(first.GetValueAsInteger() >> second.GetValueAsInteger()));
                } else {
                    throw new Exception("Invalid bit shift right operation on " + first + " and " + second);
                }
            } else if (opcode == DreamProcOpcode.GetLocalVariable) {
                int localVariableId = ReadByte();

                Push(new DreamProcIdentifierLocalVariable(_localVariables, localVariableId));
            } else if (opcode == DreamProcOpcode.Power) {
                DreamValue second = PopDreamValue();
                DreamValue first = PopDreamValue();

                if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Float) {
                    Push(new DreamValue((float)Math.Pow(first.GetValueAsInteger(), second.GetValueAsFloat())));
                } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Float) {
                    Push(new DreamValue((float)Math.Pow(first.GetValueAsFloat(), second.GetValueAsFloat())));
                } else {
                    throw new Exception("Invalid power operation on " + first + " and " + second);
                }
            } else if (opcode == DreamProcOpcode.DereferenceProc) {
                DreamObject dreamObject = PopDreamValue().GetValueAsDreamObject();
                string identifierName = ReadString();

                if (dreamObject == null) throw new Exception("Cannot dereference '" + identifierName + "' on a null object");
                if (dreamObject.HasProc(identifierName)) {
                    Push(new DreamProcIdentifierProc(new DreamProcScope(dreamObject, _currentScope.Usr), identifierName));
                } else {
                    throw new Exception("Object " + dreamObject + " has no proc named '" + identifierName + "'");
                }
            } else if (opcode == DreamProcOpcode.GetProc) {
                string identifierName = ReadString();

                Push(new DreamProcIdentifierProc(_currentScope, identifierName));
            } else if (opcode == DreamProcOpcode.Prompt) {
                DMValueType types = (DMValueType)ReadInt();
                DreamProcArguments arguments = PopArguments().CreateProcArguments();
                DreamValue firstArg = arguments.OrderedArguments[0];
                DreamObject recipientMob;
                string message;

                if (firstArg.TryGetValueAsDreamObjectOfType(DreamPath.Mob, out recipientMob)) {
                    message = arguments.OrderedArguments[1].GetValueAsString();
                } else {
                    recipientMob = _topScope.Usr;
                    message = arguments.OrderedArguments[0].GetValueAsString();
                }

                DreamObject clientObject;
                if (recipientMob != null && recipientMob.GetVariable("client").TryGetValueAsDreamObjectOfType(DreamPath.Client, out clientObject)) {
                    DreamConnection connection = Program.ClientToConnection[clientObject];
                    Task<DreamValue> promptTask = connection.Prompt(types, message);

                    promptTask.Start();
                    promptTask.Wait();
                    Push(promptTask.Result);
                }
            } else {
                throw new Exception("Invalid opcode (" + opcode + ")");
            }

            return 0;
        }

        private string ReadString() {
            return _binaryReader.ReadString();
        }

        private int ReadByte() {
            return _bytecodeStream.ReadByte();
        }

        private int ReadInt() {
            return _binaryReader.ReadInt32();
        }

        private void PushScope(DreamProcScope scope) {
            _scopeStack.Push(scope);
            _currentScope = scope;
        }

        private void PopScope() {
            _scopeStack.Pop();
            _currentScope = _scopeStack.Peek();
        }

        private void Push(DreamValue value) {
            _stack.Push(value);
        }

        private void Push(IDreamProcIdentifier value) {
            _stack.Push(value);
        }

        private void Push(DreamProcInterpreterArguments value) {
            _stack.Push(value);
        }

        private IDreamProcIdentifier PopIdentifier() {
            return (IDreamProcIdentifier)_stack.Pop();
        }

        private DreamValue PopDreamValue() {
            object value = _stack.Pop();

            if (value is IDreamProcIdentifier) {
                return ((IDreamProcIdentifier)value).GetValue();
            } else if (value is DreamValue) {
                return (DreamValue)value;
            } else {
                throw new Exception("Last object on stack was not a dream value or identifier");
            }
        }

        private DreamProcInterpreterArguments PopArguments() {
            return (DreamProcInterpreterArguments)_stack.Pop();
        }

        private bool IsTruthy(DreamValue value) {
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
            } else {
                throw new NotImplementedException("Truthy evaluation for " + value.Type + " is not implemented");
            }
        }

        private bool IsEqual(DreamValue first, DreamValue second) {
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
            } else if (first.Type == DreamValue.DreamValueType.DreamResource && second.Type != DreamValue.DreamValueType.DreamResource) {
                return false;
            } else if (first.Value == null) {
                return second.Value == null;
            } else {
                throw new NotImplementedException("Equal comparison for " + first + " and " + second + " is not implemented");
            }
        }

        private bool IsLessThan(DreamValue first, DreamValue second) {
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

        private bool IsGreaterThan(DreamValue first, DreamValue second) {
            if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Integer) {
                return first.GetValueAsInteger() > second.GetValueAsInteger();
            } else if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Float) {
                return first.GetValueAsInteger() > second.GetValueAsFloat();
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

        private void OnArgsListValueAssigned(DreamList argsList, DreamValue key, DreamValue value) {
            if (key.Type == DreamValue.DreamValueType.String) {
                string argumentName = key.GetValueAsString();

                _arguments.NamedArguments[argumentName] = value;
                _topScope.AssignValue(argumentName, value);
            } else if (key.Type == DreamValue.DreamValueType.Integer) {
                _arguments.OrderedArguments[key.GetValueAsInteger() - 1] = value;
                //TODO: _topScope.AssignValue(argName, value);
            } else {
                throw new Exception("Invalid key used on an args list");
            }
        }

        private void ResetState() {
            _bytecodeStream.Seek(0, SeekOrigin.Begin);
            DefaultReturnValue = new DreamValue((DreamObject)null);
            _arguments = default;
            _topScope = null;
            _scopeStack.Clear();
            _currentScope = null;
            _stack.Clear();
            _listEnumeratorStack.Clear();
            _localVariables.Clear();
        }
    }
}
