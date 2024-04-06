using DMCompiler.Bytecode;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DMCompiler.Compiler;
using DMCompiler.Compiler.DM.AST;
using DMCompiler.Json;

namespace DMCompiler.DM.Expressions {
    // "abc[d]"
    sealed class StringFormat : DMExpression {
        string Value { get; }
        DMExpression[] Expressions { get; }
        public override DMValueType ValType => DMValueType.Text;

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
    sealed class Arglist : DMExpression {
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
    sealed class New : DMExpression {
        private readonly DMExpression _expr;
        private readonly ArgumentList _arguments;
        public override DMValueType ValType => (_expr.ValType != DMValueType.Anything) ? _expr.ValType : (Path?.GetATOMType() ?? DMValueType.Anything);

        public override bool PathIsFuzzy => Path == null;

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
    internal sealed class NewPath : DMExpression {
        private readonly ConstantPath targetPath;
        private readonly ArgumentList arguments;
        public override DMValueType ValType => targetPath.Value.GetATOMType();

        public NewPath(Location location, ConstantPath targetPath, ArgumentList arguments)
            : base(location) {
            this.targetPath = targetPath;
            this.arguments = arguments;
        }

        public override DreamPath? Path => targetPath.Value;

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            if (!targetPath.TryResolvePath(out var pathInfo)) {
                proc.PushNull();
                return;
            }

            var argumentInfo = arguments.EmitArguments(dmObject, proc);

            switch (pathInfo.Value.Type) {
                case ConstantPath.PathType.TypeReference:
                    proc.PushType(pathInfo.Value.Id);
                    break;
                case ConstantPath.PathType.ProcReference: // "new /proc/new_verb(Destination)" is a thing
                    proc.PushProc(pathInfo.Value.Id);
                    break;
                case ConstantPath.PathType.ProcStub:
                case ConstantPath.PathType.VerbStub:
                    DMCompiler.Emit(WarningCode.BadExpression, Location, "Cannot use \"new\" with a proc stub");
                    proc.PushNull();
                    return;
            }

            proc.CreateObject(argumentInfo.Type, argumentInfo.StackSize);
        }
    }

    // locate()
    sealed class LocateInferred : DMExpression {
        private readonly DreamPath _path;
        private readonly DMExpression? _container;
        public override DMValueType ValType => DMValueType.Path;

        public LocateInferred(Location location, DreamPath path, DMExpression? container) : base(location) {
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
    sealed class Locate : DMExpression {
        private readonly DMExpression _path;
        private readonly DMExpression? _container;

        public Locate(Location location, DMExpression path, DMExpression? container) : base(location) {
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
    sealed class LocateCoordinates : DMExpression {
        private readonly DMExpression _x, _y, _z;
        public override DMValueType ValType => DMValueType.Turf;

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
    sealed class Gradient : DMExpression {
        private readonly ArgumentList _arguments;

        public Gradient(Location location, ArgumentList arguments) : base(location) {
            _arguments = arguments;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            var argInfo = _arguments.EmitArguments(dmObject, proc);

            proc.Gradient(argInfo.Type, argInfo.StackSize);
        }
    }

    /// rgb(R, G, B)
    /// rgb(R, G, B, A)
    /// rgb(x, y, z, space)
    /// rgb(x, y, z, a, space)
    internal sealed class Rgb(Location location, ArgumentList arguments) : DMExpression(location) {
        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            var argInfo = arguments.EmitArguments(dmObject, proc);

            proc.Rgb(argInfo.Type, argInfo.StackSize);
        }
    }

    // pick(prob(50);x, prob(200);y)
    // pick(50;x, 200;y)
    // pick(x, y)
    sealed class Pick : DMExpression {
        public struct PickValue {
            public readonly DMExpression? Weight;
            public readonly DMExpression Value;

            public PickValue(DMExpression? weight, DMExpression value) {
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
    sealed class AddText : DMExpression {
        private readonly DMExpression[] _parameters;
        public override DMValueType ValType => DMValueType.Text;

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
    sealed class Prob : DMExpression {
        public readonly DMExpression P;
        public override DMValueType ValType => DMValueType.Num;

        public Prob(Location location, DMExpression p) : base(location) {
            P = p;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            P.EmitPushValue(dmObject, proc);
            proc.Prob();
        }
    }

    // issaved(x)
    sealed class IsSaved : DMExpression {
        private readonly DMExpression _expr;
        public override DMValueType ValType => DMValueType.Num;

        public IsSaved(Location location, DMExpression expr) : base(location) {
            _expr = expr;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            switch (_expr) {
                case Dereference deref:
                    deref.EmitPushIsSaved(dmObject, proc);
                    return;
                case Field field:
                    field.EmitPushIsSaved(proc);
                    return;
                case Local:
                    proc.PushFloat(0);
                    return;
                default:
                    throw new CompileErrorException(Location, $"can't get saved value of {_expr}");
            }
        }
    }

    // istype(x, y)
    sealed class IsType : DMExpression {
        private readonly DMExpression _expr;
        private readonly DMExpression _path;
        public override DMValueType ValType => DMValueType.Num;

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
    sealed class IsTypeInferred : DMExpression {
        private readonly DMExpression _expr;
        private readonly DreamPath _path;
        public override DMValueType ValType => DMValueType.Num;

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

    // isnull(x)
    internal sealed class IsNull : DMExpression {
        private readonly DMExpression _value;

        public override bool PathIsFuzzy => true;
        public override DMValueType ValType => DMValueType.Num;

        public IsNull(Location location, DMExpression value) : base(location) {
            _value = value;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            _value.EmitPushValue(dmObject, proc);
            proc.IsNull();
        }
    }

    // length(x)
    internal sealed class Length : DMExpression {
        private readonly DMExpression _value;

        public override bool PathIsFuzzy => true;
        public override DMValueType ValType => DMValueType.Num;

        public Length(Location location, DMExpression value) : base(location) {
            _value = value;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            _value.EmitPushValue(dmObject, proc);
            proc.Length();
        }
    }

    // get_step(ref, dir)
    internal sealed class GetStep : DMExpression {
        private readonly DMExpression _ref;
        private readonly DMExpression _dir;

        public override bool PathIsFuzzy => true;

        public GetStep(Location location, DMExpression refValue, DMExpression dir) : base(location) {
            _ref = refValue;
            _dir = dir;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            _ref.EmitPushValue(dmObject, proc);
            _dir.EmitPushValue(dmObject, proc);
            proc.GetStep();
        }
    }

    // get_dir(loc1, loc2)
    internal sealed class GetDir : DMExpression {
        private readonly DMExpression _loc1;
        private readonly DMExpression _loc2;

        public override bool PathIsFuzzy => true;

        public GetDir(Location location, DMExpression loc1, DMExpression loc2) : base(location) {
            _loc1 = loc1;
            _loc2 = loc2;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            _loc1.EmitPushValue(dmObject, proc);
            _loc2.EmitPushValue(dmObject, proc);
            proc.GetDir();
        }
    }

    // list(...)
    sealed class List : DMExpression {
        private readonly (DMExpression? Key, DMExpression Value)[] _values;
        private readonly bool _isAssociative;

        public override bool PathIsFuzzy => true;
        public override DMValueType ValType => DMValueType.Path;

        public List(Location location, (DMExpression? Key, DMExpression Value)[] values) : base(location) {
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

        public override bool TryAsJsonRepresentation(out object? json) {
            List<object?> values = new();

            foreach (var value in _values) {
                if (!value.Value.TryAsJsonRepresentation(out var jsonValue)) {
                    json = null;
                    return false;
                }

                if (value.Key != null) {
                    // Null key is not supported here
                    if (!value.Key.TryAsJsonRepresentation(out var jsonKey) || jsonKey == null) {
                        json = null;
                        return false;
                    }

                    values.Add(new Dictionary<object, object?> {
                        { "key", jsonKey },
                        { "value", jsonValue }
                    });
                } else {
                    values.Add(jsonValue);
                }
            }

            json = new Dictionary<string, object> {
                { "type", JsonVariableType.List },
                { "values", values }
            };

            return true;
        }
    }

    // Value of var/list/L[1][2][3]
    internal sealed class DimensionalList : DMExpression {
        private readonly DMExpression[] _sizes;

        public DimensionalList(Location location, DMExpression[] sizes) : base(location) {
            _sizes = sizes;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            // This basically emits new /list(1, 2, 3)

            if (!DMObjectTree.TryGetTypeId(DreamPath.List, out var listTypeId)) {
                DMCompiler.Emit(WarningCode.ItemDoesntExist, Location, "Could not get type ID of /list");
                return;
            }

            foreach (var size in _sizes) {
                size.EmitPushValue(dmObject, proc);
            }

            proc.PushType(listTypeId);
            proc.CreateObject(DMCallArgumentsType.FromStack, _sizes.Length);
        }
    }

    // newlist(...)
    sealed class NewList : DMExpression {
        private readonly DMExpression[] _parameters;
        public override DMValueType ValType => DMValueType.Path;

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

        public override bool TryAsJsonRepresentation(out object? json) {
            json = null;
            DMCompiler.UnimplementedWarning(Location, "DMM overrides for newlist() are not implemented");
            return true; //TODO
        }
    }

    // input(...)
    sealed class Input : DMExpression {
        private readonly DMExpression[] _arguments;
        private readonly DMValueType _types;
        private readonly DMExpression? _list;
        public override DMValueType ValType => _types;

        public Input(Location location, DMExpression[] arguments, DMValueType types,
            DMExpression? list) : base(location) {
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
    internal class Initial(Location location, DMExpression expr) : DMExpression(location) {
        protected DMExpression Expression = expr;

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            if (Expression is LValue lValue) {
                lValue.EmitPushInitial(dmObject, proc);
                return;
            }

            throw new CompileErrorException(Location, $"can't get initial value of {Expression}");
        }
    }

    // call(...)(...)
    sealed class CallStatement : DMExpression {
        private readonly DMExpression _a; // Procref, Object, LibName
        private readonly DMExpression? _b; // ProcName, FuncName
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
    sealed class ProcOwnerType(Location location) : DMExpression(location) {
        public override DMValueType ValType => DMValueType.Null | DMValueType.Path;

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            // BYOND returns null if this is called in a global proc
            if (dmObject.Path == DreamPath.Root) {
                proc.PushNull();
            } else {
                proc.PushType(dmObject.Id);
            }
        }

        public override string? GetNameof(DMObject dmObject, DMProc proc) {
            if (dmObject.Path.LastElement != null) {
                return dmObject.Path.LastElement;
            }

            DMCompiler.Emit(WarningCode.BadArgument, Location, "Attempt to get nameof(__TYPE__) in global proc");
            return null;
        }
    }

    internal class Sin : DMExpression {
        private readonly DMExpression _expr;
        public override DMValueType ValType => DMValueType.Num;

        public Sin(Location location, DMExpression expr) : base(location) {
            _expr = expr;
        }

        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (!_expr.TryAsConstant(out constant)) {
                constant = null;
                return false;
            }

            if (constant is not Number {Value: var x}) {
                x = 0;
                DMCompiler.Emit(WarningCode.FallbackBuiltinArgument, _expr.Location,
                    "Invalid value treated as 0, sin(0) will always be 0");
            }

            constant = new Number(Location, SharedOperations.Sin(x));
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            _expr.EmitPushValue(dmObject, proc);
            proc.Sin();
        }
    }

    internal class Cos : DMExpression {
        private readonly DMExpression _expr;
        public override DMValueType ValType => DMValueType.Num;

        public Cos(Location location, DMExpression expr) : base(location) {
            _expr = expr;
        }

        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (!_expr.TryAsConstant(out constant)) {
                constant = null;
                return false;
            }

            if (constant is not Number {Value: var x}) {
                x = 0;
                DMCompiler.Emit(WarningCode.FallbackBuiltinArgument, _expr.Location,
                    "Invalid value treated as 0, cos(0) will always be 1");
            }

            constant = new Number(Location, SharedOperations.Cos(x));
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            _expr.EmitPushValue(dmObject, proc);
            proc.Cos();
        }
    }

    internal class Tan : DMExpression {
        private readonly DMExpression _expr;
        public override DMValueType ValType => DMValueType.Num;

        public Tan(Location location, DMExpression expr) : base(location) {
            _expr = expr;
        }

        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (!_expr.TryAsConstant(out constant)) {
                constant = null;
                return false;
            }

            if (constant is not Number {Value: var x}) {
                x = 0;
                DMCompiler.Emit(WarningCode.FallbackBuiltinArgument, _expr.Location,
                    "Invalid value treated as 0, tan(0) will always be 0");
            }

            constant = new Number(Location, SharedOperations.Tan(x));
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            _expr.EmitPushValue(dmObject, proc);
            proc.Tan();
        }
    }

    internal class ArcSin : DMExpression {
        private readonly DMExpression _expr;
        public override DMValueType ValType => DMValueType.Num;

        public ArcSin(Location location, DMExpression expr) : base(location) {
            _expr = expr;
        }

        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (!_expr.TryAsConstant(out constant)) {
                constant = null;
                return false;
            }

            if (constant is not Number {Value: var x}) {
                x = 0;
                DMCompiler.Emit(WarningCode.FallbackBuiltinArgument, _expr.Location,
                    "Invalid value treated as 0, arcsin(0) will always be 0");
            }

            if (x is < -1 or > 1) {
                DMCompiler.Emit(WarningCode.BadArgument, _expr.Location, $"Invalid value {x}, must be >= -1 and <= 1");
                x = 0;
            }

            constant = new Number(Location, SharedOperations.ArcSin(x));
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            _expr.EmitPushValue(dmObject, proc);
            proc.ArcSin();
        }
    }

    internal class ArcCos : DMExpression {
        private readonly DMExpression _expr;
        public override DMValueType ValType => DMValueType.Num;

        public ArcCos(Location location, DMExpression expr) : base(location) {
            _expr = expr;
        }

        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (!_expr.TryAsConstant(out constant)) {
                constant = null;
                return false;
            }

            if (constant is not Number {Value: var x}) {
                x = 0;
                DMCompiler.Emit(WarningCode.FallbackBuiltinArgument, _expr.Location,
                    "Invalid value treated as 0, arccos(0) will always be 1");
            }

            if (x is < -1 or > 1) {
                DMCompiler.Emit(WarningCode.BadArgument, _expr.Location, $"Invalid value {x}, must be >= -1 and <= 1");
                x = 0;
            }

            constant = new Number(Location, SharedOperations.ArcCos(x));
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            _expr.EmitPushValue(dmObject, proc);
            proc.ArcCos();
        }
    }

    internal class ArcTan : DMExpression {
        private readonly DMExpression _expr;
        public override DMValueType ValType => DMValueType.Num;

        public ArcTan(Location location, DMExpression expr) : base(location) {
            _expr = expr;
        }

        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (!_expr.TryAsConstant(out constant)) {
                constant = null;
                return false;
            }

            if (constant is not Number {Value: var a}) {
                a = 0;
                DMCompiler.Emit(WarningCode.FallbackBuiltinArgument, _expr.Location,
                    "Invalid value treated as 0, arctan(0) will always be 0");
            }

            constant = new Number(Location, SharedOperations.ArcTan(a));
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            _expr.EmitPushValue(dmObject, proc);
            proc.ArcTan();
        }
    }

    internal class ArcTan2 : DMExpression {
        private readonly DMExpression _xExpr;
        private readonly DMExpression _yExpr;
        public override DMValueType ValType => DMValueType.Num;

        public ArcTan2(Location location, DMExpression xExpr, DMExpression yExpr) : base(location) {
            _xExpr = xExpr;
            _yExpr = yExpr;
        }

        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (!_xExpr.TryAsConstant(out var xConst) || !_yExpr.TryAsConstant(out var yConst)) {
                constant = null;
                return false;
            }

            if (xConst is not Number {Value: var x}) {
                x = 0;
                DMCompiler.Emit(WarningCode.FallbackBuiltinArgument, _xExpr.Location, "Invalid x value treated as 0");
            }

            if (yConst is not Number {Value: var y}) {
                y = 0;
                DMCompiler.Emit(WarningCode.FallbackBuiltinArgument, _xExpr.Location, "Invalid y value treated as 0");
            }

            constant = new Number(Location, SharedOperations.ArcTan(x, y));
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            _xExpr.EmitPushValue(dmObject, proc);
            _yExpr.EmitPushValue(dmObject, proc);
            proc.ArcTan2();
        }
    }

    internal class Sqrt : DMExpression {
        private readonly DMExpression _expr;
        public override DMValueType ValType => DMValueType.Num;

        public Sqrt(Location location, DMExpression expr) : base(location) {
            _expr = expr;
        }

        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (!_expr.TryAsConstant(out constant)) {
                constant = null;
                return false;
            }

            if (constant is not Number {Value: var a}) {
                a = 0;
                DMCompiler.Emit(WarningCode.FallbackBuiltinArgument, _expr.Location,
                    "Invalid value treated as 0, sqrt(0) will always be 0");
            }

            if (a < 0) {
                DMCompiler.Emit(WarningCode.BadArgument, _expr.Location,
                    $"Cannot get the square root of a negative number ({a})");
            }

            constant = new Number(Location, SharedOperations.Sqrt(a));
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            _expr.EmitPushValue(dmObject, proc);
            proc.Sqrt();
        }
    }

    internal class Log : DMExpression {
        private readonly DMExpression _expr;
        private readonly DMExpression? _baseExpr;
        public override DMValueType ValType => DMValueType.Num;

        public Log(Location location, DMExpression expr, DMExpression? baseExpr) : base(location) {
            _expr = expr;
            _baseExpr = baseExpr;
        }

        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (!_expr.TryAsConstant(out constant)) {
                constant = null;
                return false;
            }

            if (constant is not Number {Value: var value} || value <= 0) {
                value = 1;
                DMCompiler.Emit(WarningCode.BadArgument, _expr.Location,
                    "Invalid value, must be a number greater than 0");
            }

            if (_baseExpr == null) {
                constant = new Number(Location, SharedOperations.Log(value));
                return true;
            }

            if (!_baseExpr.TryAsConstant(out var baseConstant)) {
                constant = null;
                return false;
            }

            if (baseConstant is not Number {Value: var baseValue} || baseValue <= 0) {
                baseValue = 10;
                DMCompiler.Emit(WarningCode.BadArgument, _baseExpr.Location,
                    "Invalid base, must be a number greater than 0");
            }

            constant = new Number(Location, SharedOperations.Log(value, baseValue));
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            _expr.EmitPushValue(dmObject, proc);
            if (_baseExpr == null) {
                proc.LogE();
            } else {
                _baseExpr.EmitPushValue(dmObject, proc);
                proc.Log();
            }
        }
    }

    internal class Abs : DMExpression {
        private readonly DMExpression _expr;
        public override DMValueType ValType => DMValueType.Num;

        public Abs(Location location, DMExpression expr) : base(location) {
            _expr = expr;
        }

        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (!_expr.TryAsConstant(out constant)) {
                constant = null;
                return false;
            }

            if (constant is not Number {Value: var a}) {
                a = 0;
                DMCompiler.Emit(WarningCode.FallbackBuiltinArgument, _expr.Location,
                    "Invalid value treated as 0, abs(0) will always be 0");
            }

            constant = new Number(Location, SharedOperations.Abs(a));
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            _expr.EmitPushValue(dmObject, proc);
            proc.Abs();
        }
    }
}
