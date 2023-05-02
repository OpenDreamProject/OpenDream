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
        private readonly DMExpression _expr;

        public Arglist(Location location, DMExpression expr) : base(location) {
            _expr = expr;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            DMCompiler.Emit(WarningCode.BadExpression, Location, "invalid use of arglist");
        }

        public void EmitPushArglist(DMObject dmObject, DMProc proc) {
            _expr.EmitPushValue(dmObject, proc);
        }
    }

    // new x (...)
    class New : DMExpression {
        private readonly DMExpression _expr;
        private readonly ArgumentList _arguments;

        public New(Location location, DMExpression expr, ArgumentList arguments) : base(location) {
            _expr = expr;
            _arguments = arguments;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            var argumentInfo = _arguments.EmitArguments(dmObject, proc);

            _expr.EmitPushValue(dmObject, proc);
            proc.CreateObject(argumentInfo.Type, argumentInfo.StackSize);
        }
    }

    // new /x/y/z (...)
    class NewPath : DMExpression {
        private readonly DreamPath _type;
        private readonly ArgumentList _arguments;

        public NewPath(Location location, DreamPath type, ArgumentList arguments) : base(location) {
            _type = type;
            _arguments = arguments;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            if (!DMObjectTree.TryGetTypeId(_type, out var typeId)) {
                DMCompiler.Emit(WarningCode.ItemDoesntExist, Location, $"Type {_type} does not exist");

                return;
            }

            var argumentInfo = _arguments.EmitArguments(dmObject, proc);

            proc.PushType(typeId);
            proc.CreateObject(argumentInfo.Type, argumentInfo.StackSize);
        }
    }

    // locate()
    class LocateInferred : DMExpression {
        private readonly DreamPath _path;
        private readonly DMExpression _container;

        public LocateInferred(Location location, DreamPath path, DMExpression container) : base(location) {
            _path = path;
            _container = container;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            if (!DMObjectTree.TryGetTypeId(_path, out var typeId)) {
                DMCompiler.Emit(WarningCode.ItemDoesntExist, Location, $"Type {_path} does not exist");

                return;
            }

            proc.PushType(typeId);

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
        private readonly DMExpression _path;
        private readonly DMExpression _container;

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
        private readonly DMExpression _x, _y, _z;

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

    // gradient(Gradient, index)
    // gradient(Item1, Item2, ..., index)
    class Gradient : DMExpression {
        private readonly ArgumentList _arguments;

        public Gradient(Location location, ArgumentList arguments) : base(location) {
            _arguments = arguments;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            var argInfo = _arguments.EmitArguments(dmObject, proc);

            proc.Gradient(argInfo.Type, argInfo.StackSize);
        }
    }

    // pick(prob(50);x, prob(200);y)
    // pick(50;x, 200;y)
    // pick(x, y)
    class Pick : DMExpression {
        public struct PickValue {
            public readonly DMExpression Weight;
            public readonly DMExpression Value;

            public PickValue(DMExpression weight, DMExpression value) {
                Weight = weight;
                Value = value;
            }
        }

        private readonly PickValue[] _values;

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
                    DMCompiler.ForcedWarning(Location, "Weighted pick() with one argument");
                }

                foreach (PickValue pickValue in _values) {
                    DMExpression weight = pickValue.Weight ?? DMExpression.Create(dmObject, proc, new DMASTConstantInteger(Location, 100)); //Default of 100

                    weight.EmitPushValue(dmObject, proc);
                    pickValue.Value.EmitPushValue(dmObject, proc);
                }

                proc.PickWeighted(_values.Length);
            } else {
                foreach (PickValue pickValue in _values) {
                    if (pickValue.Value is Arglist args) {
                        // This will just push a list which pick() accepts
                        // Really hacky and won't verify that the value is actually a list
                        args.EmitPushArglist(dmObject, proc);
                    } else {
                        pickValue.Value.EmitPushValue(dmObject, proc);
                    }
                }

                proc.PickUnweighted(_values.Length);
            }
        }
    }

    // addtext(...)
    // https://www.byond.com/docs/ref/#/proc/addtext
    class AddText : DMExpression {
        readonly DMExpression[] _parameters;

        public AddText(Location location, DMExpression[] paras) : base(location) {
            _parameters = paras;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            //We don't have to do any checking of our parameters since that was already done by VisitAddText(), hopefully. :)

            //Push addtext's arguments
            foreach (DMExpression parameter in _parameters) {
                parameter.EmitPushValue(dmObject, proc);
            }

            proc.MassConcatenation(_parameters.Length);
        }
    }

    // prob(P)
    class Prob : DMExpression {
        public readonly DMExpression P;

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
        private readonly DMExpression _expr;

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
        private readonly DMExpression _expr;
        private readonly DMExpression _path;

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
        private readonly DMExpression _expr;
        private readonly DreamPath _path;

        public IsTypeInferred(Location location, DMExpression expr, DreamPath path) : base(location) {
            _expr = expr;
            _path = path;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            if (!DMObjectTree.TryGetTypeId(_path, out var typeId)) {
                DMCompiler.Emit(WarningCode.ItemDoesntExist, Location, $"Type {_path} does not exist");

                return;
            }

            _expr.EmitPushValue(dmObject, proc);
            proc.PushType(typeId);
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
        private readonly DMExpression[] _parameters;

        public NewList(Location location, DMExpression[] parameters) : base(location) {
            _parameters = parameters;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            foreach (DMExpression parameter in _parameters) {
                parameter.EmitPushValue(dmObject, proc);
                proc.CreateObject(DMCallArgumentsType.None, 0);
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
        private readonly DMExpression _expr;

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

    // nameof(x)
    class Nameof : DMExpression {
        private readonly DMExpression _expr;

        public Nameof(Location location, DMExpression expr) : base(location) {
            _expr = expr;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            proc.PushString(_expr.GetNameof(dmObject, proc));
        }
    }

    // call(...)(...)
    class CallStatement : DMExpression {
        private readonly DMExpression _a; // Procref, Object, LibName
        private readonly DMExpression _b; // ProcName, FuncName
        private readonly ArgumentList _procArgs;

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
            var argumentInfo = _procArgs.EmitArguments(dmObject, proc);

            _b?.EmitPushValue(dmObject, proc);
            _a.EmitPushValue(dmObject, proc);
            proc.CallStatement(argumentInfo.Type, argumentInfo.StackSize);
        }
    }

    // __TYPE__
    class ProcOwnerType : DMExpression {
        public ProcOwnerType(Location location)
            : base(location)
        {}

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            // BYOND returns null if this is called in a global proc
            if (dmObject.Path == DreamPath.Root) {
                proc.PushNull();
            } else {
                proc.PushType(dmObject.Id);
            }
        }
    }

    // __PROC__
    class ProcType : DMExpression {
        public ProcType(Location location)
            : base(location)
        {}

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            proc.PushProc(proc.Id);
        }
    }
}
