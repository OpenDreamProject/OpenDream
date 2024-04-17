using DMCompiler.Bytecode;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DMCompiler.Compiler;
using DMCompiler.Compiler.DM.AST;
using DMCompiler.Json;

namespace DMCompiler.DM.Expressions {
    /// <summary>
    /// Used when there was an error generating an expression
    /// </summary>
    /// <remarks>Emit an error code before creating!</remarks>
    internal sealed class BadExpression(Location location) : DMExpression(location) {
        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            // It's normal to have this expression exist when there are errors in the code
            // But in the runtime we say it's a compiler bug because the compiler should never have output it
            proc.PushString("Encountered a bad expression (compiler bug!)");
            proc.Throw();
        }
    }

    // "abc[d]"
    internal sealed class StringFormat(Location location, string value, DMExpression[] expressions) : DMExpression(location) {
        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            foreach (DMExpression expression in expressions) {
                expression.EmitPushValue(dmObject, proc);
            }

            proc.FormatString(value);
        }
    }

    // arglist(...)
    internal sealed class Arglist(Location location, DMExpression expr) : DMExpression(location) {
        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            DMCompiler.Emit(WarningCode.BadExpression, Location, "invalid use of arglist");
        }

        public void EmitPushArglist(DMObject dmObject, DMProc proc) {
            expr.EmitPushValue(dmObject, proc);
        }
    }

    // new x (...)
    internal sealed class New(Location location, DMExpression expr, ArgumentList arguments) : DMExpression(location) {
        public override bool PathIsFuzzy => Path == null;

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            var argumentInfo = arguments.EmitArguments(dmObject, proc);

            expr.EmitPushValue(dmObject, proc);
            proc.CreateObject(argumentInfo.Type, argumentInfo.StackSize);
        }
    }

    // new /x/y/z (...)
    internal sealed class NewPath(Location location, ConstantPath targetPath, ArgumentList arguments) : DMExpression(location) {
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
    internal sealed class LocateInferred(Location location, DreamPath path, DMExpression? container) : DMExpression(location) {
        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            if (!DMObjectTree.TryGetTypeId(path, out var typeId)) {
                DMCompiler.Emit(WarningCode.ItemDoesntExist, Location, $"Type {path} does not exist");

                return;
            }

            proc.PushType(typeId);

            if (container != null) {
                container.EmitPushValue(dmObject, proc);
            } else {
                if (DMCompiler.Settings.NoStandard) {
                    DMCompiler.Emit(WarningCode.BadExpression, Location, "Implicit locate() container is not available with --no-standard");
                    proc.Error();
                    return;
                }

                DMReference world = DMReference.CreateGlobal(dmObject.GetGlobalVariableId("world").Value);
                proc.PushReferenceValue(world);
            }

            proc.Locate();
        }
    }

    // locate(x)
    internal sealed class Locate(Location location, DMExpression path, DMExpression? container) : DMExpression(location) {
        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            path.EmitPushValue(dmObject, proc);

            if (container != null) {
                container.EmitPushValue(dmObject, proc);
            } else {
                if (DMCompiler.Settings.NoStandard) {
                    DMCompiler.Emit(WarningCode.BadExpression, Location, "Implicit locate() container is not available with --no-standard");
                    proc.Error();
                    return;
                }

                DMReference world = DMReference.CreateGlobal(dmObject.GetGlobalVariableId("world").Value);
                proc.PushReferenceValue(world);
            }

            proc.Locate();
        }
    }

    // locate(x, y, z)
    internal sealed class LocateCoordinates(Location location, DMExpression x, DMExpression y, DMExpression z) : DMExpression(location) {
        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            x.EmitPushValue(dmObject, proc);
            y.EmitPushValue(dmObject, proc);
            z.EmitPushValue(dmObject, proc);
            proc.LocateCoordinates();
        }
    }

    // gradient(Gradient, index)
    // gradient(Item1, Item2, ..., index)
    internal sealed class Gradient(Location location, ArgumentList arguments) : DMExpression(location) {
        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            var argInfo = arguments.EmitArguments(dmObject, proc);

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
    internal sealed class Pick(Location location, Pick.PickValue[] values) : DMExpression(location) {
        public struct PickValue(DMExpression? weight, DMExpression value) {
            public readonly DMExpression? Weight = weight;
            public readonly DMExpression Value = value;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            bool weighted = false;
            foreach (PickValue pickValue in values) {
                if (pickValue.Weight != null) {
                    weighted = true;
                    break;
                }
            }

            if (weighted) {
                if (values.Length == 1) {
                    DMCompiler.ForcedWarning(Location, "Weighted pick() with one argument");
                }

                foreach (PickValue pickValue in values) {
                    DMExpression weight = pickValue.Weight ?? DMExpression.Create(dmObject, proc, new DMASTConstantInteger(Location, 100)); //Default of 100

                    weight.EmitPushValue(dmObject, proc);
                    pickValue.Value.EmitPushValue(dmObject, proc);
                }

                proc.PickWeighted(values.Length);
            } else {
                foreach (PickValue pickValue in values) {
                    if (pickValue.Value is Arglist args) {
                        // This will just push a list which pick() accepts
                        // Really hacky and won't verify that the value is actually a list
                        args.EmitPushArglist(dmObject, proc);
                    } else {
                        pickValue.Value.EmitPushValue(dmObject, proc);
                    }
                }

                proc.PickUnweighted(values.Length);
            }
        }
    }

    // addtext(...)
    // https://www.byond.com/docs/ref/#/proc/addtext
    internal sealed class AddText(Location location, DMExpression[] paras) : DMExpression(location) {
        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            //We don't have to do any checking of our parameters since that was already done by VisitAddText(), hopefully. :)

            //Push addtext()'s arguments
            foreach (DMExpression parameter in paras) {
                parameter.EmitPushValue(dmObject, proc);
            }

            proc.MassConcatenation(paras.Length);
        }
    }

    // prob(P)
    internal sealed class Prob(Location location, DMExpression p) : DMExpression(location) {
        public readonly DMExpression P = p;

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            P.EmitPushValue(dmObject, proc);
            proc.Prob();
        }
    }

    // issaved(x)
    internal sealed class IsSaved(Location location, DMExpression expr) : DMExpression(location) {
        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            switch (expr) {
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
                    DMCompiler.Emit(WarningCode.BadArgument, expr.Location, $"can't get saved value of {expr}");
                    proc.Error();
                    return;
            }
        }
    }

    // istype(x, y)
    internal sealed class IsType(Location location, DMExpression expr, DMExpression path) : DMExpression(location) {
        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            expr.EmitPushValue(dmObject, proc);
            path.EmitPushValue(dmObject, proc);
            proc.IsType();
        }
    }

    // istype(x)
    internal sealed class IsTypeInferred(Location location, DMExpression expr, DreamPath path) : DMExpression(location) {
        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            if (!DMObjectTree.TryGetTypeId(path, out var typeId)) {
                DMCompiler.Emit(WarningCode.ItemDoesntExist, Location, $"Type {path} does not exist");

                return;
            }

            expr.EmitPushValue(dmObject, proc);
            proc.PushType(typeId);
            proc.IsType();
        }
    }

    // isnull(x)
    internal sealed class IsNull(Location location, DMExpression value) : DMExpression(location) {
        public override bool PathIsFuzzy => true;

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            value.EmitPushValue(dmObject, proc);
            proc.IsNull();
        }
    }

    // length(x)
    internal sealed class Length(Location location, DMExpression value) : DMExpression(location) {
        public override bool PathIsFuzzy => true;

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            value.EmitPushValue(dmObject, proc);
            proc.Length();
        }
    }

    // get_step(ref, dir)
    internal sealed class GetStep(Location location, DMExpression refValue, DMExpression dir) : DMExpression(location) {
        public override bool PathIsFuzzy => true;

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            refValue.EmitPushValue(dmObject, proc);
            dir.EmitPushValue(dmObject, proc);
            proc.GetStep();
        }
    }

    // get_dir(loc1, loc2)
    internal sealed class GetDir(Location location, DMExpression loc1, DMExpression loc2) : DMExpression(location) {
        public override bool PathIsFuzzy => true;

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            loc1.EmitPushValue(dmObject, proc);
            loc2.EmitPushValue(dmObject, proc);
            proc.GetDir();
        }
    }

    // list(...)
    internal sealed class List : DMExpression {
        private readonly (DMExpression? Key, DMExpression Value)[] _values;
        private readonly bool _isAssociative;

        public override bool PathIsFuzzy => true;

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
    internal sealed class DimensionalList(Location location, DMExpression[] sizes) : DMExpression(location) {
        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            // This basically emits new /list(1, 2, 3)

            if (!DMObjectTree.TryGetTypeId(DreamPath.List, out var listTypeId)) {
                DMCompiler.Emit(WarningCode.ItemDoesntExist, Location, "Could not get type ID of /list");
                return;
            }

            foreach (var size in sizes) {
                size.EmitPushValue(dmObject, proc);
            }

            proc.PushType(listTypeId);
            proc.CreateObject(DMCallArgumentsType.FromStack, sizes.Length);
        }
    }

    // newlist(...)
    internal sealed class NewList(Location location, DMExpression[] parameters) : DMExpression(location) {
        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            foreach (DMExpression parameter in parameters) {
                parameter.EmitPushValue(dmObject, proc);
                proc.CreateObject(DMCallArgumentsType.None, 0);
            }

            proc.CreateList(parameters.Length);
        }

        public override bool TryAsJsonRepresentation(out object? json) {
            json = null;
            DMCompiler.UnimplementedWarning(Location, "DMM overrides for newlist() are not implemented");
            return true; //TODO
        }
    }

    // input(...)
    internal sealed class Input(Location location, DMExpression[] arguments, DMValueType types, DMExpression? list)
        : DMExpression(location) {
        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            // Push input's four arguments, pushing null for the missing ones
            for (int i = 3; i >= 0; i--) {
                if (i < arguments.Length) {
                    arguments[i].EmitPushValue(dmObject, proc);
                } else {
                    proc.PushNull();
                }
            }

            // The list of values to be selected from (or null for none)
            if (list != null) {
                list.EmitPushValue(dmObject, proc);
            } else {
                proc.PushNull();
            }

            proc.Prompt(types);
        }
    }

    // initial(x)
    internal class Initial(Location location, DMExpression expr) : DMExpression(location) {
        protected DMExpression Expression { get; } = expr;

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            if (Expression is LValue lValue) {
                lValue.EmitPushInitial(dmObject, proc);
                return;
            }

            DMCompiler.Emit(WarningCode.BadArgument, Expression.Location, $"can't get initial value of {Expression}");
            proc.Error();
        }
    }

    // call(...)(...)
    internal sealed class CallStatement : DMExpression {
        private readonly DMExpression _a; // Proc-ref, Object, LibName
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
    internal sealed class ProcOwnerType(Location location) : DMExpression(location) {
        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            // BYOND returns null if this is called in a global proc
            if (dmObject.Path == DreamPath.Root) {
                proc.PushNull();
            } else {
                proc.PushType(dmObject.Id);
            }
        }

        public override string? GetNameof(DMObject dmObject) {
            if (dmObject.Path.LastElement != null) {
                return dmObject.Path.LastElement;
            }

            DMCompiler.Emit(WarningCode.BadArgument, Location, "Attempt to get nameof(__TYPE__) in global proc");
            return null;
        }
    }

    internal sealed class Sin(Location location, DMExpression expr) : DMExpression(location) {
        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (!expr.TryAsConstant(out constant)) {
                constant = null;
                return false;
            }

            if (constant is not Number {Value: var x}) {
                x = 0;
                DMCompiler.Emit(WarningCode.FallbackBuiltinArgument, expr.Location,
                    "Invalid value treated as 0, sin(0) will always be 0");
            }

            constant = new Number(Location, SharedOperations.Sin(x));
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            expr.EmitPushValue(dmObject, proc);
            proc.Sin();
        }
    }

    internal sealed class Cos(Location location, DMExpression expr) : DMExpression(location) {
        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (!expr.TryAsConstant(out constant)) {
                constant = null;
                return false;
            }

            if (constant is not Number {Value: var x}) {
                x = 0;
                DMCompiler.Emit(WarningCode.FallbackBuiltinArgument, expr.Location,
                    "Invalid value treated as 0, cos(0) will always be 1");
            }

            constant = new Number(Location, SharedOperations.Cos(x));
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            expr.EmitPushValue(dmObject, proc);
            proc.Cos();
        }
    }

    internal sealed class Tan(Location location, DMExpression expr) : DMExpression(location) {
        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (!expr.TryAsConstant(out constant)) {
                constant = null;
                return false;
            }

            if (constant is not Number {Value: var x}) {
                x = 0;
                DMCompiler.Emit(WarningCode.FallbackBuiltinArgument, expr.Location,
                    "Invalid value treated as 0, tan(0) will always be 0");
            }

            constant = new Number(Location, SharedOperations.Tan(x));
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            expr.EmitPushValue(dmObject, proc);
            proc.Tan();
        }
    }

    internal sealed class ArcSin(Location location, DMExpression expr) : DMExpression(location) {
        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (!expr.TryAsConstant(out constant)) {
                constant = null;
                return false;
            }

            if (constant is not Number {Value: var x}) {
                x = 0;
                DMCompiler.Emit(WarningCode.FallbackBuiltinArgument, expr.Location,
                    "Invalid value treated as 0, arcsin(0) will always be 0");
            }

            if (x is < -1 or > 1) {
                DMCompiler.Emit(WarningCode.BadArgument, expr.Location, $"Invalid value {x}, must be >= -1 and <= 1");
                x = 0;
            }

            constant = new Number(Location, SharedOperations.ArcSin(x));
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            expr.EmitPushValue(dmObject, proc);
            proc.ArcSin();
        }
    }

    internal sealed class ArcCos(Location location, DMExpression expr) : DMExpression(location) {
        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (!expr.TryAsConstant(out constant)) {
                constant = null;
                return false;
            }

            if (constant is not Number {Value: var x}) {
                x = 0;
                DMCompiler.Emit(WarningCode.FallbackBuiltinArgument, expr.Location,
                    "Invalid value treated as 0, arccos(0) will always be 1");
            }

            if (x is < -1 or > 1) {
                DMCompiler.Emit(WarningCode.BadArgument, expr.Location, $"Invalid value {x}, must be >= -1 and <= 1");
                x = 0;
            }

            constant = new Number(Location, SharedOperations.ArcCos(x));
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            expr.EmitPushValue(dmObject, proc);
            proc.ArcCos();
        }
    }

    internal sealed class ArcTan(Location location, DMExpression expr) : DMExpression(location) {
        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (!expr.TryAsConstant(out constant)) {
                constant = null;
                return false;
            }

            if (constant is not Number {Value: var a}) {
                a = 0;
                DMCompiler.Emit(WarningCode.FallbackBuiltinArgument, expr.Location,
                    "Invalid value treated as 0, arctan(0) will always be 0");
            }

            constant = new Number(Location, SharedOperations.ArcTan(a));
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            expr.EmitPushValue(dmObject, proc);
            proc.ArcTan();
        }
    }

    internal sealed class ArcTan2(Location location, DMExpression xExpr, DMExpression yExpr) : DMExpression(location) {
        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (!xExpr.TryAsConstant(out var xConst) || !yExpr.TryAsConstant(out var yConst)) {
                constant = null;
                return false;
            }

            if (xConst is not Number {Value: var x}) {
                x = 0;
                DMCompiler.Emit(WarningCode.FallbackBuiltinArgument, xExpr.Location, "Invalid x value treated as 0");
            }

            if (yConst is not Number {Value: var y}) {
                y = 0;
                DMCompiler.Emit(WarningCode.FallbackBuiltinArgument, xExpr.Location, "Invalid y value treated as 0");
            }

            constant = new Number(Location, SharedOperations.ArcTan(x, y));
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            xExpr.EmitPushValue(dmObject, proc);
            yExpr.EmitPushValue(dmObject, proc);
            proc.ArcTan2();
        }
    }

    internal sealed class Sqrt(Location location, DMExpression expr) : DMExpression(location) {
        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (!expr.TryAsConstant(out constant)) {
                constant = null;
                return false;
            }

            if (constant is not Number {Value: var a}) {
                a = 0;
                DMCompiler.Emit(WarningCode.FallbackBuiltinArgument, expr.Location,
                    "Invalid value treated as 0, sqrt(0) will always be 0");
            }

            if (a < 0) {
                DMCompiler.Emit(WarningCode.BadArgument, expr.Location,
                    $"Cannot get the square root of a negative number ({a})");
            }

            constant = new Number(Location, SharedOperations.Sqrt(a));
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            expr.EmitPushValue(dmObject, proc);
            proc.Sqrt();
        }
    }

    internal sealed class Log(Location location, DMExpression expr, DMExpression? baseExpr)
        : DMExpression(location) {
        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (!expr.TryAsConstant(out constant)) {
                constant = null;
                return false;
            }

            if (constant is not Number {Value: var value} || value <= 0) {
                value = 1;
                DMCompiler.Emit(WarningCode.BadArgument, expr.Location,
                    "Invalid value, must be a number greater than 0");
            }

            if (baseExpr == null) {
                constant = new Number(Location, SharedOperations.Log(value));
                return true;
            }

            if (!baseExpr.TryAsConstant(out var baseConstant)) {
                constant = null;
                return false;
            }

            if (baseConstant is not Number {Value: var baseValue} || baseValue <= 0) {
                baseValue = 10;
                DMCompiler.Emit(WarningCode.BadArgument, baseExpr.Location,
                    "Invalid base, must be a number greater than 0");
            }

            constant = new Number(Location, SharedOperations.Log(value, baseValue));
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            expr.EmitPushValue(dmObject, proc);
            if (baseExpr == null) {
                proc.LogE();
            } else {
                baseExpr.EmitPushValue(dmObject, proc);
                proc.Log();
            }
        }
    }

    internal sealed class Abs(Location location, DMExpression expr) : DMExpression(location) {
        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (!expr.TryAsConstant(out constant)) {
                constant = null;
                return false;
            }

            if (constant is not Number {Value: var a}) {
                a = 0;
                DMCompiler.Emit(WarningCode.FallbackBuiltinArgument, expr.Location,
                    "Invalid value treated as 0, abs(0) will always be 0");
            }

            constant = new Number(Location, SharedOperations.Abs(a));
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            expr.EmitPushValue(dmObject, proc);
            proc.Abs();
        }
    }
}
