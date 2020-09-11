using OpenDreamServer.Dream.Objects;
using OpenDreamServer.Dream.Objects.MetaObjects;
using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

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

        private DreamProcArguments _arguments;
        private DreamProcScope _topScope;
        private DreamProc _selfProc;
        private byte[] _bytecode;
        private Stack<object> _stack = new Stack<object>();
        private Stack<DreamProcScope> _scopeStack = new Stack<DreamProcScope>();
        private Stack<DreamProcListEnumerator> _listEnumeratorStack = new Stack<DreamProcListEnumerator>();

        public DreamProcInterpreter(DreamProc selfProc, byte[] bytecode) {
            _selfProc = selfProc;
            _bytecode = bytecode;
        }

        public DreamValue Run(DreamProcScope scope, DreamProcArguments arguments) {
            MemoryStream bytecodeStream = new MemoryStream(_bytecode);
            _arguments = arguments;
            _topScope = scope;

            _scopeStack.Push(scope);
            while (bytecodeStream.Position < bytecodeStream.Length) {
                if (Step(bytecodeStream) == 1) break;
            }
            _scopeStack.Pop();

            return DefaultReturnValue;
        }

        private int Step(MemoryStream bytecodeStream) {
            DreamProcScope currentScope = _scopeStack.Peek();
            DreamProcOpcode opcode = (DreamProcOpcode)bytecodeStream.ReadByte();

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
                string identifierName = ReadString(bytecodeStream);

                if (identifierName == "args") {
                    DreamObject argsListObject = _arguments.CreateDreamList();
                    DreamList argsList = DreamMetaObjectList.DreamLists[argsListObject];

                    argsList.ValueAssigned += OnArgsListValueAssigned;
                    Push(new DreamValue(argsListObject));
                } else {
                    Push(new DreamProcIdentifierVariable(currentScope, identifierName));
                }
            } else if (opcode == DreamProcOpcode.PushString) {
                Push(new DreamValue(ReadString(bytecodeStream)));
            } else if (opcode == DreamProcOpcode.BuildString) {
                string stringResult = String.Empty;
                DreamProcOpcode buildStringOpcode;

                while ((buildStringOpcode = (DreamProcOpcode)bytecodeStream.ReadByte()) != DreamProcOpcode.BuildStringEnd) {
                    if (buildStringOpcode == DreamProcOpcode.BuildStringPartString) {
                        stringResult += ReadString(bytecodeStream);
                    } else if (buildStringOpcode == DreamProcOpcode.BuildStringPartExpression) {
                        while ((DreamProcOpcode)bytecodeStream.ReadByte() != DreamProcOpcode.BuildStringPartExpressionEnd) {
                            bytecodeStream.Seek(-1, SeekOrigin.Current);
                            Step(bytecodeStream);
                        }

                        DreamValue value = PopDreamValue();
                        if (stringResult.EndsWith("\\ref")) {
                            DreamObject dreamObject = value.GetValueAsDreamObject();

                            stringResult = stringResult.Substring(0, stringResult.Length - 4) + DreamObject.CreateReferenceID(dreamObject);
                        } else {
                            stringResult += value.Stringify();
                        }
                    }
                }

                Push(new DreamValue(stringResult));
            } else if (opcode == DreamProcOpcode.PushInt) {
                int value = ReadInt(bytecodeStream);

                Push(new DreamValue(value));
            } else if (opcode == DreamProcOpcode.DefineVariable) {
                string variableName = ReadString(bytecodeStream);
                DreamValue value = PopDreamValue();

                currentScope.CreateVariable(variableName, value);
            } else if (opcode == DreamProcOpcode.PushPath) {
                DreamPath path = new DreamPath(ReadString(bytecodeStream));

                Push(new DreamValue(path));
            } else if (opcode == DreamProcOpcode.Add) {
                DreamValue second = PopDreamValue();
                DreamValue first = PopDreamValue();

                if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Integer) {
                    Push(new DreamValue(first.GetValueAsInteger() + second.GetValueAsInteger()));
                } else if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Double) {
                    Push(new DreamValue(first.GetValueAsInteger() + second.GetValueAsDouble()));
                } else if (first.Type == DreamValue.DreamValueType.Double && second.Type == DreamValue.DreamValueType.Integer) {
                    Push(new DreamValue(first.GetValueAsDouble() + second.GetValueAsInteger()));
                } else if (first.Type == DreamValue.DreamValueType.Double && second.Type == DreamValue.DreamValueType.Double) {
                    Push(new DreamValue(first.GetValueAsDouble() + second.GetValueAsDouble()));
                } else if (first.Type == DreamValue.DreamValueType.String && second.Type == DreamValue.DreamValueType.Integer) {
                    Push(new DreamValue(first.GetValueAsString() + second.GetValueAsInteger()));
                } else if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.String) {
                    Push(new DreamValue(first.GetValueAsInteger() + second.GetValueAsString()));
                } else if (first.Type == DreamValue.DreamValueType.String && second.Type == DreamValue.DreamValueType.String) {
                    Push(new DreamValue(first.GetValueAsString() + second.GetValueAsString()));
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
            } else if (opcode == DreamProcOpcode.Call) {
                DreamProcInterpreterArguments arguments = PopArguments();
                IDreamProcIdentifier procIdentifier = PopIdentifier();

                if (procIdentifier is DreamProcIdentifierVariable) {
                    DreamProcIdentifierVariable identifier = (DreamProcIdentifierVariable)procIdentifier;

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
                        if (identifier.IdentifierName == ".." && procArguments.ArgumentCount == 0) procArguments = _arguments;

                        try {
                            Push(proc.Run(identifier.HoldingScope.DreamObject, procArguments));
                        } catch (Exception e) {
                            throw new Exception("Exception while running proc '" + identifier.IdentifierName + "' on object of type '" + identifier.HoldingScope.DreamObject.ObjectDefinition.Type + "': " + e.Message, e);
                        }
                    }
                } else if (procIdentifier is DreamProcIdentifierSelfProc) {
                    try {
                        Push(_selfProc.Run(currentScope.DreamObject, arguments.CreateProcArguments()));
                    } catch (Exception e) {
                        throw new Exception("Exception while running proc '.' on object of type '" + currentScope.DreamObject.ObjectDefinition.Type + "': " + e.Message, e);
                    }
                } else {
                    throw new Exception("Call on an invalid identifier");
                }
            } else if (opcode == DreamProcOpcode.Dereference) {
                DreamObject dreamObject = PopDreamValue().GetValueAsDreamObject();
                string identifierName = ReadString(bytecodeStream);

                if (dreamObject == null) throw new Exception("Cannot dereference '" + identifierName + "' on a null object");
                if (dreamObject.HasVariable(identifierName) || dreamObject.ObjectDefinition.HasGlobalVariable(identifierName) || dreamObject.HasProc(identifierName)) {
                    Push(new DreamProcIdentifierVariable(new DreamProcScope(dreamObject), identifierName));
                } else {
                    throw new Exception("Object " + dreamObject + " has no identifier named '" + identifierName + "'");
                }
            } else if (opcode == DreamProcOpcode.JumpIfFalse) {
                int position = ReadInt(bytecodeStream);
                DreamValue value = PopDreamValue();

                if (!IsTruthy(value)) {
                    bytecodeStream.Seek(position, SeekOrigin.Begin);
                }
            } else if (opcode == DreamProcOpcode.JumpIfTrue) {
                int position = ReadInt(bytecodeStream);
                DreamValue value = PopDreamValue();

                if (IsTruthy(value)) {
                    bytecodeStream.Seek(position, SeekOrigin.Begin);
                }
            } else if (opcode == DreamProcOpcode.Jump) {
                int position = ReadInt(bytecodeStream);

                bytecodeStream.Seek(position, SeekOrigin.Begin);
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
                int afterExpressionPosition = ReadInt(bytecodeStream);
                DreamValue leftValue = PopDreamValue();

                if (IsTruthy(leftValue)) {
                    while ((DreamProcOpcode)bytecodeStream.ReadByte() != DreamProcOpcode.BooleanExpressionEnd) {
                        bytecodeStream.Seek(-1, SeekOrigin.Current);
                        Step(bytecodeStream);
                    }

                    bytecodeStream.Seek(afterExpressionPosition, SeekOrigin.Begin);

                    DreamValue rightValue = PopDreamValue();
                    Push(new DreamValue(IsTruthy(rightValue) ? 1 : 0));
                } else {
                    bytecodeStream.Seek(afterExpressionPosition, SeekOrigin.Begin);
                    Push(new DreamValue(0));
                }
            } else if (opcode == DreamProcOpcode.BooleanNot) {
                DreamValue value = PopDreamValue();

                Push(new DreamValue(IsTruthy(value) ? 0 : 1));
            } else if (opcode == DreamProcOpcode.PushSuperProc) {
                Push(new DreamProcIdentifierVariable(currentScope, ".."));
            } else if (opcode == DreamProcOpcode.Negate) {
                DreamValue value = PopDreamValue();

                if (value.Type == DreamValue.DreamValueType.Integer) {
                    Push(new DreamValue(-value.GetValueAsInteger()));
                } else if (value.Type == DreamValue.DreamValueType.Double) {
                    Push(new DreamValue(-value.GetValueAsDouble()));
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
                } else if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Integer) {
                    identifier.Assign(new DreamValue(first.GetValueAsInteger() + second.GetValueAsInteger()));
                } else if (first.Type == DreamValue.DreamValueType.Double && second.Type == DreamValue.DreamValueType.Integer) {
                    identifier.Assign(new DreamValue(first.GetValueAsDouble() + second.GetValueAsInteger()));
                } else if (first.Type == DreamValue.DreamValueType.String && second.Type == DreamValue.DreamValueType.String) {
                    identifier.Assign(new DreamValue(first.GetValueAsString() + second.GetValueAsString()));
                } else {
                    throw new Exception("Invalid append operation on " + first + " and " + second);
                }
            } else if (opcode == DreamProcOpcode.CreateScope) {
                _scopeStack.Push(new DreamProcScope(currentScope));
            } else if (opcode == DreamProcOpcode.DestroyScope) {
                _scopeStack.Pop();
            } else if (opcode == DreamProcOpcode.CompareLessThanOrEqual) {
                DreamValue second = PopDreamValue();
                DreamValue first = PopDreamValue();

                Push(new DreamValue((IsEqual(first, second) || IsLessThan(first, second)) ? 1 : 0));
            } else if (opcode == DreamProcOpcode.IndexList) {
                DreamValue index = PopDreamValue();
                DreamObject list = PopDreamValue().GetValueAsDreamObjectOfType(DreamPath.List);

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
                } else {
                    throw new Exception("Invalid remove operation on " + first + " and " + second);
                }
            } else if (opcode == DreamProcOpcode.DeleteObject) {
                DreamObject dreamObject = PopDreamValue().GetValueAsDreamObject();

                if (dreamObject != null) {
                    dreamObject.Delete();
                } else {
                    throw new Exception("Cannot delete a null value");
                }
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
                            Push(proc.Run(dreamObject, arguments.CreateProcArguments()));
                        } catch (Exception e) {
                            throw new Exception("Exception while running proc " + procId + " on object of type '" + dreamObject.ObjectDefinition.Type + "': " + e.Message, e);
                        }
                    } else {

                        throw new Exception("Invalid proc (" + procId + ")");
                    }
                } else {
                    throw new Exception("Call statement has an invalid source (" + source + ")");
                }
            } else if (opcode == DreamProcOpcode.BitAnd) {
                int second = PopDreamValue().GetValueAsInteger();
                int first = PopDreamValue().GetValueAsInteger();

                Push(new DreamValue(first & second));
            } else if (opcode == DreamProcOpcode.CompareNotEquals) {
                DreamValue second = PopDreamValue();
                DreamValue first = PopDreamValue();

                Push(new DreamValue(IsEqual(first, second) ? 0 : 1));
            } else if (opcode == DreamProcOpcode.Divide) {
                DreamValue second = PopDreamValue();
                DreamValue first = PopDreamValue();

                if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Integer) {
                    Push(new DreamValue(first.GetValueAsInteger() / second.GetValueAsInteger()));
                } else if (first.Type == DreamValue.DreamValueType.Double && second.Type == DreamValue.DreamValueType.Integer) {
                    Push(new DreamValue(first.GetValueAsDouble() / second.GetValueAsInteger()));
                } else {
                    throw new Exception("Invalid divide operation on " + first + " and " + second);
                }
            } else if (opcode == DreamProcOpcode.Multiply) {
                DreamValue second = PopDreamValue();
                DreamValue first = PopDreamValue();

                if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Integer) {
                    Push(new DreamValue(first.GetValueAsInteger() * second.GetValueAsInteger()));
                } else if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Double) {
                    Push(new DreamValue(first.GetValueAsInteger() * second.GetValueAsDouble()));
                } else if (first.Type == DreamValue.DreamValueType.Double && second.Type == DreamValue.DreamValueType.Integer) {
                    Push(new DreamValue(first.GetValueAsDouble() * second.GetValueAsInteger()));
                } else if (first.Type == DreamValue.DreamValueType.Double && second.Type == DreamValue.DreamValueType.Double) {
                    Push(new DreamValue(first.GetValueAsDouble() * second.GetValueAsDouble()));
                } else {
                    throw new Exception("Invalid multiply operation on " + first + " and " + second);
                }
            } else if (opcode == DreamProcOpcode.PushSelf) {
                Push(new DreamProcIdentifierSelfProc(_selfProc, this));
            } else if (opcode == DreamProcOpcode.CreateObject) {
                DreamProcInterpreterArguments arguments = PopArguments();
                DreamPath objectPath = PopDreamValue().GetValueAsPath();

                if (objectPath.Type == DreamPath.PathType.Other && objectPath.Elements.Length == 1) {
                    objectPath = currentScope.GetValue(objectPath.LastElement).GetValueAsPath();
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
                } else if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Integer) {
                    identifier.Assign(new DreamValue(first.GetValueAsInteger() | second.GetValueAsInteger()));
                } else if (first.Value == null) {
                    identifier.Assign(second);
                } else {
                    throw new Exception("Invalid combine operation on " + first + " and " + second);
                }
            } else if (opcode == DreamProcOpcode.BooleanOr) {
                int afterExpressionPosition = ReadInt(bytecodeStream);
                DreamValue leftValue = PopDreamValue();

                if (IsTruthy(leftValue)) {
                    bytecodeStream.Seek(afterExpressionPosition, SeekOrigin.Begin);
                    Push(leftValue);
                } else {
                    while (bytecodeStream.Position < afterExpressionPosition) {
                        Step(bytecodeStream);
                    }

                    DreamValue rightValue = PopDreamValue();
                    Push(rightValue);
                }
            } else if (opcode == DreamProcOpcode.PushArgumentList) {
                DreamObject argListObject = PopDreamValue().GetValueAsDreamObjectOfType(DreamPath.List);
                DreamList argList = DreamMetaObjectList.DreamLists[argListObject];
                List<DreamValue> argListValues = argList.GetValues();
                Dictionary<object, DreamValue> argListNamedValues = argList.GetAssociativeValues();
                DreamProcInterpreterArguments arguments = new DreamProcInterpreterArguments(new List<object>(), new Dictionary<string, object>());

                foreach (DreamValue value in argListValues) {
                    if (!argListNamedValues.ContainsKey(value.Value)) {
                        arguments.OrderedArguments.Add(value);
                    }
                }

                foreach (KeyValuePair<object, DreamValue> namedValue in argListNamedValues) {
                    string name = namedValue.Key as string;

                    if (name != null) {
                        arguments.NamedArguments.Add(name, namedValue.Value);
                    } else {
                        throw new Exception("List contains a non-string key, and cannot be used as an arglist");
                    }
                }

                Push(arguments);
            } else if (opcode == DreamProcOpcode.CompareGreaterThanOrEqual) {
                DreamValue second = PopDreamValue();
                DreamValue first = PopDreamValue();

                Push(new DreamValue((IsEqual(first, second) || IsGreaterThan(first, second)) ? 1 : 0));
            } else if (opcode == DreamProcOpcode.BranchSwitch) {
                int afterSwitchPosition = ReadInt(bytecodeStream);
                int elsePosition = ReadInt(bytecodeStream);
                int caseCount = ReadInt(bytecodeStream);
                bool caseFound = false;
                DreamValue value = PopDreamValue();

                for (int i = 0; i < caseCount; i++) {
                    while ((DreamProcOpcode)bytecodeStream.ReadByte() != DreamProcOpcode.BranchSwitchCaseExpressionEnd) {
                        bytecodeStream.Seek(-1, SeekOrigin.Current);
                        Step(bytecodeStream);
                    }

                    int caseBodyPosition = ReadInt(bytecodeStream);
                    DreamValue testValue = PopDreamValue();
                    if (IsEqual(value, testValue)) {
                        caseFound = true;
                        bytecodeStream.Seek(caseBodyPosition, SeekOrigin.Begin);
                        break;
                    }
                }

                if (caseFound) {
                    while ((DreamProcOpcode)bytecodeStream.ReadByte() != DreamProcOpcode.BranchSwitchCaseEnd) {
                        bytecodeStream.Seek(-1, SeekOrigin.Current);
                        Step(bytecodeStream);
                    }
                } else {
                    bytecodeStream.Seek(elsePosition, SeekOrigin.Begin);

                    while (bytecodeStream.Position < afterSwitchPosition) {
                        Step(bytecodeStream);
                    }
                }

                bytecodeStream.Seek(afterSwitchPosition, SeekOrigin.Begin);
            } else if (opcode == DreamProcOpcode.Mask) {
                DreamValue second = PopDreamValue();
                IDreamProcIdentifier identifier = PopIdentifier();
                DreamValue first = identifier.GetValue();

                if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Integer) {
                    identifier.Assign(new DreamValue(first.GetValueAsInteger() & second.GetValueAsInteger()));
                } else {
                    throw new Exception("Invalid mask operation on " + first + " and " + second);
                }
            } else if (opcode == DreamProcOpcode.Ternary) {
                DreamValue testValue = PopDreamValue();
                int rightExpressionPosition = ReadInt(bytecodeStream);
                int afterTernaryPosition = ReadInt(bytecodeStream);

                if (IsTruthy(testValue)) {
                    while (bytecodeStream.Position < rightExpressionPosition) {
                        Step(bytecodeStream);
                    }

                    bytecodeStream.Seek(afterTernaryPosition, SeekOrigin.Begin);
                } else {
                    bytecodeStream.Seek(rightExpressionPosition, SeekOrigin.Begin);
                }
            } else if (opcode == DreamProcOpcode.IsInList) {
                DreamValue listValue = PopDreamValue();
                DreamValue value = PopDreamValue();

                if (listValue.Value != null) {
                    DreamObject listObject = listValue.GetValueAsDreamObject();
                    DreamList list;

                    if (listObject.IsSubtypeOf(DreamPath.List)) {
                        list = DreamMetaObjectList.DreamLists[listObject];
                    } else if (listObject.IsSubtypeOf(DreamPath.Atom) || listObject.IsSubtypeOf(DreamPath.World)) {
                        DreamObject contents = listObject.GetVariable("contents").GetValueAsDreamObjectOfType(DreamPath.List);

                        list = DreamMetaObjectList.DreamLists[contents];
                    } else {
                        throw new Exception("Value " + listValue + " is not a " + DreamPath.List + ", " + DreamPath.Atom + " or " + DreamPath.World);
                    }

                    Push(new DreamValue(list.ContainsValue(value) ? 1 : 0));
                } else {
                    Push(new DreamValue(0));
                }
            } else if (opcode == DreamProcOpcode.PushArguments) {
                DreamProcInterpreterArguments arguments = new DreamProcInterpreterArguments(new List<object>(), new Dictionary<string, object>());
                int argumentCount = ReadInt(bytecodeStream);

                for (int i = 0; i < argumentCount; i++) {
                    DreamProcOpcode argumentType = (DreamProcOpcode)bytecodeStream.ReadByte();

                    if (argumentType == DreamProcOpcode.ParameterNamed) {
                        string argumentName = ReadString(bytecodeStream);

                        while ((DreamProcOpcode)bytecodeStream.ReadByte() != DreamProcOpcode.ParameterEnd) {
                            bytecodeStream.Seek(-1, SeekOrigin.Current);
                            Step(bytecodeStream);
                        }

                        arguments.NamedArguments[argumentName] = _stack.Pop();
                    } else if (argumentType == DreamProcOpcode.ParameterUnnamed) {
                        while ((DreamProcOpcode)bytecodeStream.ReadByte() != DreamProcOpcode.ParameterEnd) {
                            bytecodeStream.Seek(-1, SeekOrigin.Current);
                            Step(bytecodeStream);
                        }

                        arguments.OrderedArguments.Add(_stack.Pop());
                    } else {
                        throw new Exception("Invalid argument type (" + argumentType + ")");
                    }
                }

                Push(arguments);
            } else if (opcode == DreamProcOpcode.PushDouble) {
                BinaryReader bytecodeBinaryReader = new BinaryReader(bytecodeStream);

                Push(new DreamValue(bytecodeBinaryReader.ReadDouble()));
            } else if (opcode == DreamProcOpcode.PushSrc) {
                Push(new DreamValue(currentScope.DreamObject));
            } else if (opcode == DreamProcOpcode.CreateListEnumerator) {
                DreamObject listObject = PopDreamValue().GetValueAsDreamObject();
                DreamList list;

                if (listObject.IsSubtypeOf(DreamPath.List)) {
                    list = DreamMetaObjectList.DreamLists[listObject].CreateCopy();
                } else if (listObject.IsSubtypeOf(DreamPath.Atom) || listObject.IsSubtypeOf(DreamPath.World)) {
                    DreamObject contents = listObject.GetVariable("contents").GetValueAsDreamObjectOfType(DreamPath.List);

                    list = DreamMetaObjectList.DreamLists[contents].CreateCopy();
                } else {
                    throw new Exception("Object " + listObject + " is not a " + DreamPath.List + ", " + DreamPath.Atom + " or " + DreamPath.World);
                }

                _listEnumeratorStack.Push(new DreamProcListEnumerator(list));
            } else if (opcode == DreamProcOpcode.EnumerateList) {
                string outputVarName = ReadString(bytecodeStream);
                DreamProcListEnumerator listEnumerator = _listEnumeratorStack.Peek();
                bool successfulEnumeration = listEnumerator.TryMoveNext(out DreamValue newValue);

                Push(new DreamValue(successfulEnumeration ? 1 : 0));
                if (successfulEnumeration) {
                    currentScope.AssignValue(outputVarName, newValue);
                }
            } else if (opcode == DreamProcOpcode.DestroyListEnumerator) {
                _listEnumeratorStack.Pop();
            } else {
                throw new Exception("Invalid opcode (" + opcode + ")");
            }

            return 0;
        }

        private string ReadString(MemoryStream bytecodeStream) {
            string value = String.Empty;
            int lastByte;

            while ((lastByte = bytecodeStream.ReadByte()) != 0 && bytecodeStream.Position < bytecodeStream.Length) {
                value += (char)lastByte;
            }

            if (lastByte != 0) {
                throw new Exception("String was not null-terminated");
            }

            return value;
        }

        private int ReadInt(MemoryStream bytecodeStream) {
            int value = (bytecodeStream.ReadByte() << 24);
            value |= (bytecodeStream.ReadByte() << 16);
            value |= (bytecodeStream.ReadByte() << 8);
            value |= bytecodeStream.ReadByte();

            return value;
        }

        private void Push(object value) {
            if (!(value is DreamValue || value is IDreamProcIdentifier || value is DreamProcInterpreterArguments)) {
                throw new Exception("Value being pushed onto the stack must be a " + nameof(DreamValue) + ", " + nameof(IDreamProcIdentifier) + " or " + nameof(DreamProcInterpreterArguments));
            }

            _stack.Push(value);
        }

        private IDreamProcIdentifier PopIdentifier() {
            IDreamProcIdentifier value = _stack.Pop() as IDreamProcIdentifier;

            if (value == null) {
                throw new Exception("Last object on stack was not an identifier");
            }

            return value;
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
            object value = _stack.Pop();

            if (value is DreamProcInterpreterArguments) {
                return (DreamProcInterpreterArguments)value;
            } else {
                throw new Exception("Last object on stack was not " + nameof(DreamProcInterpreterArguments));
            }
        }

        private bool IsTruthy(DreamValue value) {
            if (value.Type == DreamValue.DreamValueType.DreamObject) {
                return (value.GetValueAsDreamObject() != null);
            } else if (value.Type == DreamValue.DreamValueType.DreamPath) {
                return true;
            } else if (value.Type == DreamValue.DreamValueType.Integer) {
                return (value.GetValueAsInteger() != 0);
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
            } else if (first.Type == DreamValue.DreamValueType.DreamObject && second.Type == DreamValue.DreamValueType.Double) {
                return false;
            } else if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Integer) {
                return first.GetValueAsInteger() == second.GetValueAsInteger();
            } else if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.DreamObject) {
                return false;
            } else if (first.Type == DreamValue.DreamValueType.Double && second.Type == DreamValue.DreamValueType.Double) {
                return first.GetValueAsDouble() == second.GetValueAsDouble();
            } else if (first.Type == DreamValue.DreamValueType.Double && second.Type == DreamValue.DreamValueType.Integer) {
                return first.GetValueAsDouble() == second.GetValueAsInteger();
            } else if (first.Type == DreamValue.DreamValueType.Double && second.Type == DreamValue.DreamValueType.DreamObject) {
                return false;
            } else if (first.Type == DreamValue.DreamValueType.String && second.Type == DreamValue.DreamValueType.Integer) {
                return false;
            } else if (first.Type == DreamValue.DreamValueType.String && second.Type == DreamValue.DreamValueType.String) {
                return first.GetValueAsString() == second.GetValueAsString();
            } else if (first.Type == DreamValue.DreamValueType.String && second.Type == DreamValue.DreamValueType.DreamObject) {
                return false;
            } else if (first.Type == DreamValue.DreamValueType.DreamPath && second.Type == DreamValue.DreamValueType.DreamPath) {
                return first.GetValueAsPath().Equals(second.GetValueAsPath());
            } else if (first.Type == DreamValue.DreamValueType.DreamPath && second.Type == DreamValue.DreamValueType.String) {
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
            } else if (first.Type == DreamValue.DreamValueType.Double && second.Type == DreamValue.DreamValueType.Integer) {
                return first.GetValueAsDouble() < second.GetValueAsInteger();
            } else {
                throw new Exception("Invalid less than comparison between " + first + " and " + second);
            }
        }

        private bool IsGreaterThan(DreamValue first, DreamValue second) {
            if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Integer) {
                return first.GetValueAsInteger() > second.GetValueAsInteger();
            } else if (first.Type == DreamValue.DreamValueType.Integer && second.Type == DreamValue.DreamValueType.Double) {
                return first.GetValueAsInteger() > second.GetValueAsDouble();
            } else if (first.Type == DreamValue.DreamValueType.Double && second.Type == DreamValue.DreamValueType.Integer) {
                return first.GetValueAsDouble() > second.GetValueAsInteger();
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
    }
}
