using OpenDreamShared.Compiler;
using OpenDreamShared.Compiler.DM;
using OpenDreamShared.Dream;
using OpenDreamShared.Json;
using System.Collections.Generic;

namespace DMCompiler.DM.Expressions {
    // "abc[d]"
    class StringFormat : DMExpression {
        string Value { get; }
        DMExpression[] Expressions { get; }

        public StringFormat(string value, DMExpression[] expressions) {
            Value = value;
            Expressions = expressions;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            for (int i = Expressions.Length - 1; i >= 0; i--) {
                Expressions[i].EmitPushValue(dmObject, proc);
            }

            proc.FormatString(Value);
        }
    }

    // arglist(...)
    class Arglist : DMExpression {
        DMExpression _expr;

        public Arglist(DMExpression expr) {
            _expr = expr;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            throw new CompileErrorException("invalid use of `arglist`");
        }

        public void EmitPushArglist(DMObject dmObject, DMProc proc) {
            _expr.EmitPushValue(dmObject, proc);
            proc.PushArgumentList();
        }
    }

    // new x (...)
    class New : DMExpression {
        DMExpression Expr;
        ArgumentList Arguments;

        public New(DMExpression expr, ArgumentList arguments) {
            Expr = expr;
            Arguments = arguments;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            Expr.EmitPushValue(dmObject, proc);
            Arguments.EmitPushArguments(dmObject, proc);
            proc.CreateObject();
        }
    }

    // new /x/y/z (...)
    class NewPath : DMExpression {
        DreamPath TargetPath;
        ArgumentList Arguments;

        public NewPath(DreamPath targetPath, ArgumentList arguments) {
            TargetPath = targetPath;
            Arguments = arguments;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            proc.PushPath(TargetPath);
            Arguments.EmitPushArguments(dmObject, proc);
            proc.CreateObject();
        }
    }

    // locate()
    class LocateInferred : DMExpression {
        DreamPath _path;
        DMExpression _container;

        public LocateInferred(DreamPath path, DMExpression container) {
            _path = path;
            _container = container;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            proc.PushPath(_path);

            if (_container != null) {
                _container.EmitPushValue(dmObject, proc);
            } else {
                proc.GetIdentifier("world");
            }

            proc.Locate();
        }
    }

    // locate(x)
    class Locate : DMExpression {
        DMExpression _path;
        DMExpression _container;

        public Locate(DMExpression path, DMExpression container) {
            _path = path;
            _container = container;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            _path.EmitPushValue(dmObject, proc);

            if (_container != null) {
                _container.EmitPushValue(dmObject, proc);
            } else {
                proc.GetIdentifier("world");
            }

            proc.Locate();
        }
    }

    // locate(x, y, z)
    class LocateCoordinates : DMExpression {
        DMExpression _x, _y, _z;

        public LocateCoordinates(DMExpression x, DMExpression y, DMExpression z) {
            _x = x;
            _y = y;
            _z = z;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            _x.EmitPushValue(dmObject, proc);
            _y.EmitPushValue(dmObject, proc);
            _z.EmitPushValue(dmObject, proc);
            proc.LocateCoordinates();
        }
    }

    // pick(50;x, 200;y)
    // pick(x, y)
    class Pick : DMExpression {
        public struct PickValue {
            public DMExpression Weight;
            public DMExpression Value;

            public PickValue(DMExpression weight, DMExpression value) {
                Weight = weight;
                Value = value;
            }
        }

        PickValue[] _values;

        public Pick(PickValue[] values) {
            _values = values;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            bool weighted = false;
            foreach (PickValue pickValue in _values) {
                if (pickValue.Weight != null) {
                    weighted = true;
                    break;
                }
            }

            if (weighted) {
                if (_values.Length == 1) {
                    Program.Warning(new CompilerWarning(null, "Weighted pick() with one argument"));
                }

                foreach (PickValue pickValue in _values) {
                    DMExpression weight = pickValue.Weight ?? DMExpression.Create(dmObject, proc, new DMASTConstantInteger(100)); //Default of 100

                    weight.EmitPushValue(dmObject, proc);
                    pickValue.Value.EmitPushValue(dmObject, proc);
                }

                proc.PickWeighted(_values.Length);
            } else {
                foreach (PickValue pickValue in _values) {
                    pickValue.Value.EmitPushValue(dmObject, proc);
                }

                proc.PickUnweighted(_values.Length);
            }
        }
    }

    // issaved(x)
    class IsSaved : DMExpression {
        DMExpression _expr;

        public IsSaved(DMExpression expr) {
            _expr = expr;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            if (_expr is Field field) {
                field.EmitPushIsSaved(proc);
                return;
            }

            if (_expr is Dereference dereference) {
                dereference.EmitPushIsSaved(dmObject, proc);
                return;
            }

            if (_expr is Local)
            {
                proc.PushFloat(0);
                return;
            }

            throw new CompileErrorException($"can't get saved value of {_expr}");
        }
    }

    // istype(x, y)
    class IsType : DMExpression {
        DMExpression _expr;
        DMExpression _path;

        public IsType(DMExpression expr, DMExpression path) {
            _expr = expr;
            _path = path;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            _expr.EmitPushValue(dmObject, proc);
            _path.EmitPushValue(dmObject, proc);
            proc.IsType();
        }
    }

    // istype(x)
    class IsTypeInferred : DMExpression {
        DMExpression _expr;
        DreamPath _path;

        public IsTypeInferred(DMExpression expr, DreamPath path) {
            _expr = expr;
            _path = path;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            _expr.EmitPushValue(dmObject, proc);
            proc.PushPath(_path);
            proc.IsType();
        }
    }

    // list(...)
    class List : DMExpression {
        // Lazy
        DMASTList _astNode;

        public List(DMASTList astNode) {
            _astNode = astNode;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            proc.CreateList();

            if (_astNode.Values != null) {
                foreach (DMASTCallParameter value in _astNode.Values) {
                    DMASTAssign associatedAssign = value.Value as DMASTAssign;

                    if (associatedAssign != null) {
                        DMExpression.Create(dmObject, proc, associatedAssign.Value).EmitPushValue(dmObject, proc);

                        if (associatedAssign.Expression is DMASTIdentifier identifier) {
                            proc.PushString(identifier.Identifier);
                            proc.ListAppendAssociated();
                        } else {
                            DMExpression.Create(dmObject, proc, associatedAssign.Expression).EmitPushValue(dmObject, proc);
                            proc.ListAppendAssociated();
                        }
                    } else {
                        DMExpression.Create(dmObject, proc, value.Value).EmitPushValue(dmObject, proc);

                        if (value.Name != null) {
                            proc.PushString(value.Name);
                            proc.ListAppendAssociated();
                        } else {
                            proc.ListAppend();
                        }
                    }
                }
            }
        }

        public override object ToJsonRepresentation() {
            List<object> list = new();
            Dictionary<string, object> associatedValues = new();

            foreach (DMASTCallParameter parameter in _astNode.Values) {
                object value = DMExpression.Create(null, null, parameter.Value).ToJsonRepresentation();
                DMASTAssign associatedAssign = parameter.Value as DMASTAssign;

                if (associatedAssign != null) {
                    if (associatedAssign.Expression is DMASTIdentifier identifier) {
                        associatedValues.Add(identifier.Identifier, value);
                    } else {
                        throw new System.Exception("Invalid associated value key");
                    }
                } else if (parameter.Name != null) {
                    associatedValues.Add(parameter.Name, value);
                } else {
                    list.Add(value);
                }
            }

            Dictionary<string, object> jsonRepresentation = new();
            jsonRepresentation.Add("type", JsonVariableType.List);
            if (list.Count > 0) jsonRepresentation.Add("values", list);
            if (associatedValues.Count > 0) jsonRepresentation.Add("associatedValues", associatedValues);
            return jsonRepresentation;
        }
    }

    // newlist(...)
    class NewList : DMExpression {
        DMExpression[] _parameters;

        public NewList(DMExpression[] parameters) {
            _parameters = parameters;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            proc.CreateList();

            foreach (DMExpression parameter in _parameters) {
                parameter.EmitPushValue(dmObject, proc);
                proc.PushArguments(0);
                proc.CreateObject();
                proc.ListAppend();
            }
        }

        public override object ToJsonRepresentation() {
            return null; //TODO
        }
    }

    // input(...)
    class Input : DMExpression {
        // Lazy
        DMASTInput _astNode;

        public Input(DMASTInput astNode) {
            _astNode = astNode;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            if (_astNode.Parameters.Length == 0 || _astNode.Parameters.Length > 4) throw new CompileErrorException("Invalid input() parameter count");

            //Push input's four arguments, pushing null for the missing ones
            for (int i = 3; i >= 0; i--) {
                if (i < _astNode.Parameters.Length) {
                    DMASTCallParameter parameter = _astNode.Parameters[i];

                    if (parameter.Name != null) throw new CompileErrorException("input() does not take named arguments");
                    DMExpression.Create(dmObject, proc, parameter.Value).EmitPushValue(dmObject, proc);
                } else {
                    proc.PushNull();
                }
            }

            proc.Prompt(_astNode.Types);
        }
    }

    // initial(x)
    class Initial : DMExpression {
        DMExpression _expr;

        public Initial(DMExpression expr) {
            _expr = expr;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            // TODO: Add EmitPushInitial to Base?

            if (_expr is Field field) {
                field.EmitPushInitial(proc);
                return;
            }

            if (_expr is Dereference dereference) {
                dereference.EmitPushInitial(dmObject, proc);
                return;
            }

            throw new CompileErrorException($"can't get initial value of {_expr}");
        }
    }

    // call(...)(...)
    class CallStatement : DMExpression {
        DMExpression _a; // Procref, Object, LibName
        DMExpression _b; // ProcName, FuncName
        ArgumentList _procArgs;

        public CallStatement(DMExpression a, ArgumentList procArgs) {
            _a = a;
            _procArgs = procArgs;
        }

        public CallStatement(DMExpression a, DMExpression b, ArgumentList procArgs) {
            _a = a;
            _b = b;
            _procArgs = procArgs;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            if (_b != null) {
                _b.EmitPushValue(dmObject, proc);
            }

            _a.EmitPushValue(dmObject, proc);
            _procArgs.EmitPushArguments(dmObject, proc);
            proc.CallStatement();
        }
    }
}
