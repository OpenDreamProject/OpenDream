using OpenDreamShared.Compiler;
using DMCompiler.Compiler.DM;
using OpenDreamShared.Dream;
using OpenDreamShared.Json;
using System.Collections.Generic;
using JetBrains.Annotations;
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
            foreach (DMExpression expression in Expressions) {
                expression.EmitPushValue(dmObject, proc);
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

    // pick(prob(50);x, prob(200);y)
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

            //Push addtext's arguments
            foreach (DMExpression parameter in parameters) {
                parameter.EmitPushValue(dmObject, proc);
            }

            proc.MassConcatenation(parameters.Length);
        }
    }

    // prob(P)
    class Prob : DMExpression {
        public DMExpression P;

        public Prob(Location location, DMExpression p) : base(location) {
            P = p;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            P.EmitPushValue(dmObject, proc);
            proc.Prob();
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
                    idx.EmitPushIsSaved(dmObject, proc);
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
        private readonly (DMExpression Key, DMExpression Value)[] _values;
        private readonly bool _isAssociative;

        public List(Location location, (DMExpression Key, DMExpression Value)[] values) : base(location) {
            _values = values;

            _isAssociative = false;
            foreach (var value in values) {
                if (value.Key != null) {
                    _isAssociative = true;
                    break;
                }
            }
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            foreach (var value in _values) {
                if (_isAssociative) {
                    if (value.Key == null) {
                        proc.PushNull();
                    } else {
                        value.Key.EmitPushValue(dmObject, proc);
                    }
                }

                value.Value.EmitPushValue(dmObject, proc);
            }

            if (_isAssociative) {
                proc.CreateAssociativeList(_values.Length);
            } else {
                proc.CreateList(_values.Length);
            }
        }

        public override bool TryAsJsonRepresentation(out object json) {
            List<object> list = new();
            Dictionary<string, object> associatedValues = new();

            foreach (var value in _values) {
                if (!value.Value.TryAsJsonRepresentation(out var jsonValue)) {
                    json = null;
                    return false;
                }

                if (value.Key != null) {
                    if (value.Key is not Expressions.String keyString) { //Only string keys are supported
                        json = null;
                        return false;
                    }

                    associatedValues.Add(keyString.Value, jsonValue);
                } else {
                    list.Add(jsonValue);
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
            foreach (DMExpression parameter in _parameters) {
                parameter.EmitPushValue(dmObject, proc);
                proc.PushArguments(0);
                proc.CreateObject();
            }

            proc.CreateList(_parameters.Length);
        }

        public override bool TryAsJsonRepresentation(out object json) {
            json = null;
            DMCompiler.UnimplementedWarning(Location, "DMM overrides for newlist() are not implemented");
            return true; //TODO
        }
    }

    // input(...)
    class Input : DMExpression {
        private readonly DMExpression[] _arguments;
        private readonly DMValueType _types;
        [CanBeNull] private readonly DMExpression _list;

        public Input(Location location, DMExpression[] arguments, DMValueType types,
            [CanBeNull] DMExpression list) : base(location) {
            if (arguments.Length is 0 or > 4) {
                throw new CompileErrorException(location, "input() must have 1 to 4 arguments");
            }

            _arguments = arguments;
            _types = types;
            _list = list;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            // Push input's four arguments, pushing null for the missing ones
            for (int i = 3; i >= 0; i--) {
                if (i < _arguments.Length) {
                    _arguments[i].EmitPushValue(dmObject, proc);
                } else {
                    proc.PushNull();
                }
            }

            // The list of values to be selected from (or null for none)
            if (_list != null) {
                _list.EmitPushValue(dmObject, proc);
            } else {
                proc.PushNull();
            }

            proc.Prompt(_types);
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
