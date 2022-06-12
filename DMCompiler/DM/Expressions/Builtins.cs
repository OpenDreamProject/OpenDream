using OpenDreamShared.Compiler;
using DMCompiler.Compiler.DM;
using OpenDreamShared.Dream;
using OpenDreamShared.Json;
using System.Collections.Generic;
using OpenDreamShared.Dream.Procs;

namespace DMCompiler.DM.Expressions {
    // "abc[d]"
    class StringFormat : DMExpression {
        string Value { get; }
        DMExpression[] Expressions { get; }

        public StringFormat(Location location, string value, DMExpression[] expressions) : base(location) {
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

        public Arglist(Location location, DMExpression expr) : base(location) {
            _expr = expr;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            throw new CompileErrorException(Location, "invalid use of arglist");
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

        public New(Location location, DMExpression expr, ArgumentList arguments) : base(location) {
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

        public NewPath(Location location, DreamPath targetPath, ArgumentList arguments) : base(location) {
            TargetPath = targetPath;
            Arguments = arguments;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            proc.PushPath(TargetPath);
            Arguments.EmitPushArguments(dmObject, proc);
            proc.CreateObject();
        }
    }

    sealed class NewMultidimensionalList : DMExpression {
        DMExpression[] Expressions;

        public NewMultidimensionalList(Location location, DMExpression[] expressions) : base(location) {
            Expressions = expressions;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            foreach (var expr in Expressions)
            {
                expr.EmitPushValue(dmObject, proc);
            }
            proc.CreateMultidimensionalList(Expressions.Length);
        }
    }

    // locate()
    class LocateInferred : DMExpression {
        DreamPath _path;
        DMExpression _container;

        public LocateInferred(Location location, DreamPath path, DMExpression container) : base(location) {
            _path = path;
            _container = container;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            proc.PushPath(_path);

            if (_container != null) {
                _container.EmitPushValue(dmObject, proc);
            } else {
                if (DMCompiler.Settings.NoStandard) {
                    throw new CompileErrorException(Location, "Implicit locate() container is not available with --no-standard");
                }

                DMReference world = DMReference.CreateGlobal(dmObject.GetGlobalVariableId("world").Value);
                proc.PushReferenceValue(world);
            }

            proc.Locate();
        }
    }

    // locate(x)
    class Locate : DMExpression {
        DMExpression _path;
        DMExpression _container;

        public Locate(Location location, DMExpression path, DMExpression container) : base(location) {
            _path = path;
            _container = container;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            _path.EmitPushValue(dmObject, proc);

            if (_container != null) {
                _container.EmitPushValue(dmObject, proc);
            } else {
                if (DMCompiler.Settings.NoStandard) {
                    throw new CompileErrorException(Location, "Implicit locate() container is not available with --no-standard");
                }

                DMReference world = DMReference.CreateGlobal(dmObject.GetGlobalVariableId("world").Value);
                proc.PushReferenceValue(world);
            }

            proc.Locate();
        }
    }

    // locate(x, y, z)
    class LocateCoordinates : DMExpression {
        DMExpression _x, _y, _z;

        public LocateCoordinates(Location location, DMExpression x, DMExpression y, DMExpression z) : base(location) {
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

        public Pick(Location location, PickValue[] values) : base(location) {
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
                    DMCompiler.Warning(new CompilerWarning(Location, "Weighted pick() with one argument"));
                }

                foreach (PickValue pickValue in _values) {
                    DMExpression weight = pickValue.Weight ?? DMExpression.Create(dmObject, proc, new DMASTConstantInteger(Location, 100)); //Default of 100

                    weight.EmitPushValue(dmObject, proc);
                    pickValue.Value.EmitPushValue(dmObject, proc);
                }

                proc.PickWeighted(_values.Length);
            } else {
                foreach (PickValue pickValue in _values)
                {
                    if (pickValue.Value is Arglist args)
                    {
                        args.EmitPushArglist(dmObject, proc);
                    }
                    else
                    {
                        pickValue.Value.EmitPushValue(dmObject, proc);
                    }
                }

                proc.PickUnweighted(_values.Length);
            }
        }
    }

    // addtext(...)
    // https://www.byond.com/docs/ref/#/proc/addtext
    class AddText : DMExpression
    {
        readonly DMExpression[] parameters;
        public AddText(Location location, DMExpression[] paras) : base(location)
        {
            parameters = paras;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc)
        {
            //We don't have to do any checking of our parameters since that was already done by VisitAddText(), hopefully. :)

            //Push addtext's arguments (in reverse, otherwise the strings will be concatenated in reverse, lol)
            for (int i = parameters.Length - 1; i >= 0; i--)
            {
                parameters[i].EmitPushValue(dmObject, proc);
            }
            proc.MassConcatenation(parameters.Length);
        }
    }

    // issaved(x)
    class IsSaved : DMExpression {
        DMExpression _expr;

        public IsSaved(Location location, DMExpression expr) : base(location) {
            _expr = expr;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            switch (_expr)
            {
                case Field field:
                    field.EmitPushIsSaved(proc);
                    return;
                case Dereference dereference:
                    dereference.EmitPushIsSaved(dmObject, proc);
                    return;
                case Local:
                    proc.PushFloat(0);
                    return;
                case ListIndex idx:
                    proc.PushFloat(0);
                    //TODO Support "vars" properly
                    idx.IsSaved();
                    return;
                default:
                    throw new CompileErrorException(Location, $"can't get saved value of {_expr}");
            }
        }
    }

    // istype(x, y)
    class IsType : DMExpression {
        DMExpression _expr;
        DMExpression _path;

        public IsType(Location location, DMExpression expr, DMExpression path) : base(location) {
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

        public IsTypeInferred(Location location, DMExpression expr, DreamPath path) : base(location) {
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

        public List(Location location, DMASTList astNode) : base(location) {
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

        public override bool TryAsJsonRepresentation(out object json) {
            List<object> list = new();
            Dictionary<string, object> associatedValues = new();

            foreach (DMASTCallParameter parameter in _astNode.Values) {
                if (!DMExpression.Create(null, null, parameter.Value).TryAsJsonRepresentation(out var value)) {
                    json = null;
                    return false;
                }

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
            json = jsonRepresentation;
            return true;
        }
    }

    // newlist(...)
    class NewList : DMExpression {
        DMExpression[] _parameters;

        public NewList(Location location, DMExpression[] parameters) : base(location) {
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

        public override bool TryAsJsonRepresentation(out object json) {
            json = null;
            DMCompiler.UnimplementedWarning(Location, "DMM overrides for newlist() are not implemented");
            return true; //TODO
        }
    }

    // input(...)
    class Input : DMExpression {
        // Lazy
        DMASTInput _astNode;

        public Input(Location location, DMASTInput astNode) : base(location) {
            _astNode = astNode;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            if (_astNode.Parameters.Length == 0 || _astNode.Parameters.Length > 4) throw new CompileErrorException(_astNode.Location,"Invalid input() parameter count");

            //Push input's four arguments, pushing null for the missing ones
            for (int i = 3; i >= 0; i--) {
                if (i < _astNode.Parameters.Length) {
                    DMASTCallParameter parameter = _astNode.Parameters[i];

                    if (parameter.Name != null) throw new CompileErrorException(parameter.Location,"input() does not take named arguments");
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

        public Initial(Location location, DMExpression expr) : base(location) {
            _expr = expr;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            if (_expr is LValue lValue) {
                lValue.EmitPushInitial(dmObject, proc);
                return;
            }

            throw new CompileErrorException(Location, $"can't get initial value of {_expr}");
        }
    }

    // call(...)(...)
    class CallStatement : DMExpression {
        DMExpression _a; // Procref, Object, LibName
        DMExpression _b; // ProcName, FuncName
        ArgumentList _procArgs;

        public CallStatement(Location location, DMExpression a, ArgumentList procArgs) : base(location) {
            _a = a;
            _procArgs = procArgs;
        }

        public CallStatement(Location location, DMExpression a, DMExpression b, ArgumentList procArgs) : base(location) {
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
