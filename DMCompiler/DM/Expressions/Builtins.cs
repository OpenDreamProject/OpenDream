using DMCompiler.Bytecode;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using DMCompiler.Compiler;
using DMCompiler.Json;

namespace DMCompiler.DM.Expressions;

/// <summary>
/// Used when there was an error generating an expression
/// </summary>
/// <remarks>Emit an error code before creating!</remarks>
internal sealed class BadExpression(Location location) : DMExpression(location) {
    public override void EmitPushValue(ExpressionContext ctx) {
        // It's normal to have this expression exist when there are errors in the code
        // But in the runtime we say it's a compiler bug because the compiler should never have output it
        ctx.Proc.PushString("Encountered a bad expression (compiler bug!)");
        ctx.Proc.Throw();
    }
}

internal sealed class UnknownReference(Location location, string message) : DMExpression(location) {
    public string Message => message;

    public override void EmitPushValue(ExpressionContext ctx) {
        // It's normal to have this expression exist when there's out-of-order definitions in the code
        // But in the runtime we say it's a compiler bug because the compiler should never have output it
        ctx.Proc.PushString("Encountered an unknown reference expression (compiler bug!)");
        ctx.Proc.Throw();
    }

    public void EmitCompilerError(DMCompiler compiler) {
        compiler.Emit(WarningCode.ItemDoesntExist, Location, message);
    }
}

// "abc[d]"
internal sealed class StringFormat(Location location, string value, DMExpression[] expressions) : DMExpression(location) {
    public override DMComplexValueType ValType => DMValueType.Text;

    public override void EmitPushValue(ExpressionContext ctx) {
        foreach (DMExpression expression in expressions) {
            expression.EmitPushValue(ctx);
        }

        ctx.Proc.FormatString(value);
    }
}

// arglist(...)
internal sealed class Arglist(Location location, DMExpression expr) : DMExpression(location) {
    public override void EmitPushValue(ExpressionContext ctx) {
        ctx.Compiler.Emit(WarningCode.BadExpression, Location, "invalid use of arglist");
        ctx.Proc.PushNullAndError();
    }

    public void EmitPushArglist(ExpressionContext ctx) {
        expr.EmitPushValue(ctx);
    }
}

// new x (...)
internal sealed class New(DMCompiler compiler, Location location, DMExpression expr, ArgumentList arguments) : DMExpression(location) {
    public override DreamPath? Path => expr.Path;
    public override bool PathIsFuzzy => Path == null;
    public override DMComplexValueType ValType => !expr.ValType.IsAnything ? expr.ValType : (Path?.GetAtomType(compiler) ?? DMValueType.Anything);

    public override void EmitPushValue(ExpressionContext ctx) {
        var argumentInfo = arguments.EmitArguments(ctx, null);

        ctx.Proc.PushNull();
        expr.EmitPushValue(ctx);
        ctx.Proc.CreateObject(argumentInfo.Type, argumentInfo.StackSize);
    }
}

// new /x/y/z (...)
internal sealed class NewPath(DMCompiler compiler, Location location, IConstantPath create,
    Dictionary<string, object?>? variableOverrides, ArgumentList arguments) : DMExpression(location) {
    public override DreamPath? Path => (create is ConstantTypeReference typeReference) ? typeReference.Path : null;
    public override DMComplexValueType ValType => Path?.GetAtomType(compiler) ?? DMValueType.Anything;

    public override void EmitPushValue(ExpressionContext ctx) {
        DMCallArgumentsType argumentsType;
        int stackSize;

        switch (create) {
            case ConstantTypeReference typeReference:
                // ctx: This might give us null depending on how definition order goes
                var newProc = ctx.ObjectTree.GetNewProc(typeReference.Value.Id);

                (argumentsType, stackSize) = arguments.EmitArguments(ctx, newProc);
                if (variableOverrides is null || variableOverrides.Count == 0) {
                    ctx.Proc.PushNull();
                } else {
                    ctx.Proc.PushString(JsonSerializer.Serialize(variableOverrides));
                }

                ctx.Proc.PushType(typeReference.Value.Id);
                break;
            case ConstantProcReference procReference: // "new /proc/new_verb(Destination)" is a thing
                (argumentsType, stackSize) = arguments.EmitArguments(ctx, ctx.ObjectTree.AllProcs[procReference.Value.Id]);
                if(variableOverrides is not null && variableOverrides.Count > 0) {
                    ctx.Compiler.Emit(WarningCode.BadExpression, Location, "Cannot add a Var Override to a proc");
                    ctx.Proc.Error();
                    return;
                }

                ctx.Proc.PushNull();
                ctx.Proc.PushProc(procReference.Value.Id);
                break;
            default:
                ctx.Compiler.Emit(WarningCode.BadExpression, Location, $"Cannot instantiate {create}");
                ctx.Proc.PushNull();
                return;
        }

        ctx.Proc.CreateObject(argumentsType, stackSize);
    }
}

// locate()
internal sealed class LocateInferred(Location location, DreamPath path, DMExpression? container) : DMExpression(location) {
    public override DMComplexValueType ValType => path;

    public override void EmitPushValue(ExpressionContext ctx) {
        if (!ctx.ObjectTree.TryGetTypeId(path, out var typeId)) {
            ctx.Compiler.Emit(WarningCode.ItemDoesntExist, Location, $"Type {path} does not exist");
            ctx.Proc.PushNull(); // prevents a negative stack size error
            ctx.Proc.Error();
            return;
        }

        ctx.Proc.PushType(typeId);

        if (container != null) {
            container.EmitPushValue(ctx);
        } else {
            if (ctx.Compiler.Settings.NoStandard) {
                ctx.Compiler.Emit(WarningCode.BadExpression, Location, "Implicit locate() container is not available with --no-standard");
                ctx.Proc.Error();
                return;
            }

            ctx.Proc.PushReferenceValue(DMReference.World);
        }

        ctx.Proc.Locate();
    }
}

// locate(x)
internal sealed class Locate(Location location, DMExpression path, DMExpression? container) : DMExpression(location) {
    public override bool PathIsFuzzy => true;

    public override void EmitPushValue(ExpressionContext ctx) {
        path.EmitPushValue(ctx);

        if (container != null) {
            container.EmitPushValue(ctx);
        } else {
            if (ctx.Compiler.Settings.NoStandard) {
                ctx.Compiler.Emit(WarningCode.BadExpression, Location, "Implicit locate() container is not available with --no-standard");
                ctx.Proc.Error();
                return;
            }

            ctx.Proc.PushReferenceValue(DMReference.World);
        }

        ctx.Proc.Locate();
    }
}

// locate(x, y, z)
internal sealed class LocateCoordinates(Location location, DMExpression x, DMExpression y, DMExpression z) : DMExpression(location) {
    public override DMComplexValueType ValType => DMValueType.Turf;

    public override void EmitPushValue(ExpressionContext ctx) {
        x.EmitPushValue(ctx);
        y.EmitPushValue(ctx);
        z.EmitPushValue(ctx);
        ctx.Proc.LocateCoordinates();
    }
}

// gradient(Gradient, index)
// gradient(Item1, Item2, ..., index)
internal sealed class Gradient(Location location, ArgumentList arguments) : DMExpression(location) {
    public override void EmitPushValue(ExpressionContext ctx) {
        ctx.ObjectTree.TryGetGlobalProc("gradient", out var dmProc);
        var argInfo = arguments.EmitArguments(ctx, dmProc);

        ctx.Proc.Gradient(argInfo.Type, argInfo.StackSize);
    }
}

/// rgb(R, G, B)
/// rgb(R, G, B, A)
/// rgb(x, y, z, space)
/// rgb(x, y, z, a, space)
internal sealed class Rgb(Location location, ArgumentList arguments) : DMExpression(location) {
    public override void EmitPushValue(ExpressionContext ctx) {
        ctx.ObjectTree.TryGetGlobalProc("rgb", out var dmProc);
        var argInfo = arguments.EmitArguments(ctx, dmProc);

        ctx.Proc.Rgb(argInfo.Type, argInfo.StackSize);
    }

    // TODO: This needs to have full parity with the rgb opcode. This is a simplified implementation for the most common case rgb(R, G, B)
    public override bool TryAsConstant(DMCompiler compiler, [NotNullWhen(true)] out Constant? constant) {
        (string?, float?)[] values = new (string?, float?)[arguments.Length];

        bool validArgs = true;

        if (arguments.Length < 3 || arguments.Length > 5) {
            compiler.Emit(WarningCode.BadExpression, Location, $"rgb: expected 3 to 5 arguments (found {arguments.Length})");
            constant = null;
            return false;
        }

        for (var index = 0; index < arguments.Expressions.Length; index++) {
            var (name, expr) = arguments.Expressions[index];
            if (!expr.TryAsConstant(compiler, out var constExpr)) {
                constant = null;
                return false;
            }

            if (constExpr is not Number num) {
                validArgs = false;
                values[index] = (name, null);
                continue;
            }

            values[index] = (name, num.Value);
        }

        if (!validArgs) {
            compiler.Emit(WarningCode.FallbackBuiltinArgument, Location,
                "Non-numerical rgb argument(s) will always return \"00\"");
        }

        string result;
        try {
            result = SharedOperations.ParseRgb(values);
        } catch (Exception e) {
            compiler.Emit(WarningCode.BadExpression, Location, e.Message);
            constant = null;
            return false;
        }

        constant = new String(Location, result);

        return true;
    }
}

// Animate(...)
internal sealed class Animate(Location location, ArgumentList arguments) : DMExpression(location) {
    public override void EmitPushValue(ExpressionContext ctx) {
        ctx.ObjectTree.TryGetGlobalProc("animate", out var dmProc);
        var argInfo = arguments.EmitArguments(ctx, dmProc);

        ctx.Proc.Animate(argInfo.Type, argInfo.StackSize);
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

    public override void EmitPushValue(ExpressionContext ctx) {
        bool weighted = false;
        foreach (PickValue pickValue in values) {
            if (pickValue.Weight != null) {
                weighted = true;
                break;
            }
        }

        if (weighted) {
            if (values.Length == 1) {
                ctx.Compiler.Emit(WarningCode.InvalidArgumentCount, Location, "Weighted pick() with one argument"); // BYOND errors with "extra args"
            }

            ctx.Compiler.Emit(WarningCode.PickWeightedSyntax, Location, "Use of weighted pick() syntax");

            foreach (PickValue pickValue in values) {
                DMExpression weight = pickValue.Weight ?? new Number(Location.Internal, 100); //Default of 100

                weight.EmitPushValue(ctx);
                pickValue.Value.EmitPushValue(ctx);
            }

            ctx.Proc.PickWeighted(values.Length);
        } else {
            foreach (PickValue pickValue in values) {
                if (pickValue.Value is Arglist args) {
                    // This will just push a list which pick() accepts
                    // Really hacky and won't verify that the value is actually a list
                    args.EmitPushArglist(ctx);
                } else {
                    pickValue.Value.EmitPushValue(ctx);
                }
            }

            ctx.Proc.PickUnweighted(values.Length);
        }
    }
}

// addtext(...)
// https://www.byond.com/docs/ref/#/proc/addtext
internal sealed class AddText(Location location, DMExpression[] paras) : DMExpression(location) {
    public override DMComplexValueType ValType => DMValueType.Text;

    public override void EmitPushValue(ExpressionContext ctx) {
        //We don't have to do any checking of our parameters since that was already done by VisitAddText(), hopefully. :)

        //Push addtext()'s arguments
        foreach (DMExpression parameter in paras) {
            parameter.EmitPushValue(ctx);
        }

        ctx.Proc.MassConcatenation(paras.Length);
    }
}

// prob(P)
internal sealed class Prob(Location location, DMExpression p) : DMExpression(location) {
    public readonly DMExpression P = p;

    public override DMComplexValueType ValType => DMValueType.Num;

    public override void EmitPushValue(ExpressionContext ctx) {
        P.EmitPushValue(ctx);
        ctx.Proc.Prob();
    }
}

// issaved(x)
internal sealed class IsSaved(Location location, DMExpression expr) : DMExpression(location) {
    public override DMComplexValueType ValType => DMValueType.Num;

    public override void EmitPushValue(ExpressionContext ctx) {
        expr.EmitPushIsSaved(ctx);
    }
}

// astype(x, y)
internal sealed class AsType(Location location, DMExpression expr, DMExpression path) : DMExpression(location) {
    public override DreamPath? Path => path.Path;
    public override bool PathIsFuzzy => path is not ConstantTypeReference;

    public override void EmitPushValue(ExpressionContext ctx) {
        expr.EmitPushValue(ctx);
        path.EmitPushValue(ctx);
        ctx.Proc.AsType();
    }
}

// astype(x)
internal sealed class AsTypeInferred(Location location, DMExpression expr, DreamPath path) : DMExpression(location) {
    public override DreamPath? Path => path;

    public override void EmitPushValue(ExpressionContext ctx) {
        if (!ctx.ObjectTree.TryGetTypeId(path, out var typeId)) {
            ctx.Compiler.Emit(WarningCode.ItemDoesntExist, Location, $"Type {path} does not exist");
            ctx.Proc.PushNullAndError();
            return;
        }

        expr.EmitPushValue(ctx);
        ctx.Proc.PushType(typeId);
        ctx.Proc.AsType();
    }
}

// istype(x, y)
internal sealed class IsType(Location location, DMExpression expr, DMExpression path) : DMExpression(location) {
    public override DMComplexValueType ValType => DMValueType.Num;

    public override void EmitPushValue(ExpressionContext ctx) {
        expr.EmitPushValue(ctx);
        path.EmitPushValue(ctx);
        ctx.Proc.IsType();
    }
}

// istype(x)
internal sealed class IsTypeInferred(Location location, DMExpression expr, DreamPath path) : DMExpression(location) {
    public override DMComplexValueType ValType => DMValueType.Num;

    public override void EmitPushValue(ExpressionContext ctx) {
        if (!ctx.ObjectTree.TryGetTypeId(path, out var typeId)) {
            ctx.Compiler.Emit(WarningCode.ItemDoesntExist, Location, $"Type {path} does not exist");
            ctx.Proc.PushNullAndError();
            return;
        }

        expr.EmitPushValue(ctx);
        ctx.Proc.PushType(typeId);
        ctx.Proc.IsType();
    }
}

// isnull(x)
internal sealed class IsNull(Location location, DMExpression value) : DMExpression(location) {
    public override bool PathIsFuzzy => true;
    public override DMComplexValueType ValType => DMValueType.Num;

    public override void EmitPushValue(ExpressionContext ctx) {
        value.EmitPushValue(ctx);
        ctx.Proc.IsNull();
    }
}

// length(x)
internal sealed class Length(Location location, DMExpression value) : DMExpression(location) {
    public override bool PathIsFuzzy => true;
    public override DMComplexValueType ValType => DMValueType.Num;

    public override void EmitPushValue(ExpressionContext ctx) {
        value.EmitPushValue(ctx);
        ctx.Proc.Length();
    }
}

// get_step(ref, dir)
internal sealed class GetStep(Location location, DMExpression refValue, DMExpression dir) : DMExpression(location) {
    public override bool PathIsFuzzy => true;

    public override void EmitPushValue(ExpressionContext ctx) {
        refValue.EmitPushValue(ctx);
        dir.EmitPushValue(ctx);
        ctx.Proc.GetStep();
    }
}

// get_dir(loc1, loc2)
internal sealed class GetDir(Location location, DMExpression loc1, DMExpression loc2) : DMExpression(location) {
    public override bool PathIsFuzzy => true;

    public override void EmitPushValue(ExpressionContext ctx) {
        loc1.EmitPushValue(ctx);
        loc2.EmitPushValue(ctx);
        ctx.Proc.GetDir();
    }
}

// list(...)
internal sealed class List : DMExpression {
    private readonly (DMExpression? Key, DMExpression Value)[] _values;
    private readonly bool _isAssociative;

    public override bool PathIsFuzzy => true;
    public override DMComplexValueType ValType => DreamPath.List;

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

    public override void EmitPushValue(ExpressionContext ctx) {
        foreach (var value in _values) {
            if (_isAssociative) {
                if (value.Key == null) {
                    ctx.Proc.PushNull();
                } else {
                    value.Key.EmitPushValue(ctx);
                }
            }

            value.Value.EmitPushValue(ctx);
        }

        if (_isAssociative) {
            ctx.Proc.CreateAssociativeList(_values.Length);
        } else {
            ctx.Proc.CreateList(_values.Length);
        }
    }

    public override bool TryAsJsonRepresentation(DMCompiler compiler, out object? json) {
        List<object?> values = new();

        foreach (var value in _values) {
            if (!value.Value.TryAsJsonRepresentation(compiler, out var jsonValue)) {
                json = null;
                return false;
            }

            if (value.Key != null) {
                // Null key is not supported here
                if (!value.Key.TryAsJsonRepresentation(compiler, out var jsonKey) || jsonKey == null) {
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

// alist(...)
internal sealed class AList(Location location, (DMExpression Key, DMExpression Value)[] values) : DMExpression(location) {
    public override bool PathIsFuzzy => true;
    public override DMComplexValueType ValType => DreamPath.AList;

    public override void EmitPushValue(ExpressionContext ctx) {
        foreach (var value in values) {
            value.Key.EmitPushValue(ctx);
            value.Value.EmitPushValue(ctx);
        }

        ctx.Proc.CreateStrictAssociativeList(values.Length);
    }

    public override bool TryAsJsonRepresentation(DMCompiler compiler, out object? json) {
        List<object?> values1 = new();

        foreach (var value in values) {
            if (!value.Value.TryAsJsonRepresentation(compiler, out var jsonValue)) {
                json = null;
                return false;
            }

            // Null key is not supported here
            if (!value.Key.TryAsJsonRepresentation(compiler, out var jsonKey) || jsonKey == null) {
                json = null;
                return false;
            }

            values1.Add(new Dictionary<object, object?> {
                { "key", jsonKey },
                { "value", jsonValue }
            });
        }

        json = new Dictionary<string, object> {
            { "type", JsonVariableType.AList },
            { "values", values1 }
        };

        return true;
    }
}

// Value of var/list/L[1][2][3]
internal sealed class DimensionalList(Location location, DMExpression[] sizes) : DMExpression(location) {
    public override void EmitPushValue(ExpressionContext ctx) {
        foreach (var size in sizes) {
            size.EmitPushValue(ctx);
        }

        // Should be equivalent to new /list(1, 2, 3)
        ctx.Proc.CreateMultidimensionalList(sizes.Length);
    }
}

// newlist(...)
internal sealed class NewList(Location location, DMExpression[] parameters) : DMExpression(location) {
    public override DMComplexValueType ValType => DreamPath.List;

    public override void EmitPushValue(ExpressionContext ctx) {
        foreach (DMExpression parameter in parameters) {
            ctx.Proc.PushNull();
            parameter.EmitPushValue(ctx);
            ctx.Proc.CreateObject(DMCallArgumentsType.None, 0);
        }

        ctx.Proc.CreateList(parameters.Length);
    }

    public override bool TryAsJsonRepresentation(DMCompiler compiler, out object? json) {
        json = null;
        compiler.UnimplementedWarning(Location, "DMM overrides for newlist() are not implemented");
        return true; //ctx
    }
}

// input(...)
internal sealed class Input(Location location, DMExpression[] arguments, DMValueType types, DMExpression? list)
    : DMExpression(location) {
    public override DMComplexValueType ValType => types;

    public override void EmitPushValue(ExpressionContext ctx) {
        // Push input's four arguments, pushing null for the missing ones
        for (int i = 3; i >= 0; i--) {
            if (i < arguments.Length) {
                arguments[i].EmitPushValue(ctx);
            } else {
                ctx.Proc.PushNull();
            }
        }

        // The list of values to be selected from (or null for none)
        if (list != null) {
            list.EmitPushValue(ctx);
        } else {
            ctx.Proc.PushNull();
        }

        ctx.Proc.Prompt(types);
    }
}

// initial(x)
internal class Initial(Location location, DMExpression expr) : DMExpression(location) {
    protected DMExpression Expression { get; } = expr;

    public override void EmitPushValue(ExpressionContext ctx) {
        if (Expression is LValue lValue) {
            lValue.EmitPushInitial(ctx);
            return;
        }

        if (Expression is Arglist arglist) {
            // This happens silently in BYOND
            ctx.Compiler.Emit(WarningCode.PointlessBuiltinCall, Location, "calling initial() on arglist() returns the current value");
            arglist.EmitPushArglist(ctx);
            return;
        }

        ctx.Compiler.Emit(WarningCode.BadArgument, Expression.Location, $"can't get initial value of {Expression}");
        ctx.Proc.PushNullAndError();
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

    public override void EmitPushValue(ExpressionContext ctx) {
        var argumentInfo = _procArgs.EmitArguments(ctx, null);

        _b?.EmitPushValue(ctx);
        _a.EmitPushValue(ctx);
        ctx.Proc.CallStatement(argumentInfo.Type, argumentInfo.StackSize);
    }
}

// __TYPE__
internal sealed class ProcOwnerType(Location location, DMObject owner) : DMExpression(location) {
    private DreamPath? OwnerPath => owner.Path == DreamPath.Root ? null : owner.Path;

    public override DMComplexValueType ValType => (OwnerPath != null) ? OwnerPath.Value : DMValueType.Null;

    public override void EmitPushValue(ExpressionContext ctx) {
        // BYOND returns null if this is called in a global proc
        if (ctx.Type.Path == DreamPath.Root) {
            ctx.Proc.PushNull();
        } else {
            ctx.Proc.PushType(ctx.Type.Id);
        }
    }

    public override string? GetNameof(ExpressionContext ctx) {
        if (ctx.Type.Path.LastElement != null) {
            return ctx.Type.Path.LastElement;
        }

        ctx.Compiler.Emit(WarningCode.BadArgument, Location, "Attempt to get nameof(__TYPE__) in global proc");
        return null;
    }
}

internal sealed class Sin(Location location, DMExpression expr) : DMExpression(location) {
    public override DMComplexValueType ValType => DMValueType.Num;

    public override bool TryAsConstant(DMCompiler compiler, [NotNullWhen(true)] out Constant? constant) {
        if (!expr.TryAsConstant(compiler, out constant)) {
            constant = null;
            return false;
        }

        if (constant is not Number {Value: var x}) {
            x = 0;
            compiler.Emit(WarningCode.FallbackBuiltinArgument, expr.Location,
                "Invalid value treated as 0, sin(0) will always be 0");
        }

        constant = new Number(Location, SharedOperations.Sin(x));
        return true;
    }

    public override void EmitPushValue(ExpressionContext ctx) {
        if (TryAsConstant(ctx.Compiler, out var constant)) {
            constant.EmitPushValue(ctx);
            return;
        }

        expr.EmitPushValue(ctx);
        ctx.Proc.Sin();
    }
}

internal sealed class Cos(Location location, DMExpression expr) : DMExpression(location) {
    public override DMComplexValueType ValType => DMValueType.Num;

    public override bool TryAsConstant(DMCompiler compiler, [NotNullWhen(true)] out Constant? constant) {
        if (!expr.TryAsConstant(compiler, out constant)) {
            constant = null;
            return false;
        }

        if (constant is not Number {Value: var x}) {
            x = 0;
            compiler.Emit(WarningCode.FallbackBuiltinArgument, expr.Location,
                "Invalid value treated as 0, cos(0) will always be 1");
        }

        constant = new Number(Location, SharedOperations.Cos(x));
        return true;
    }

    public override void EmitPushValue(ExpressionContext ctx) {
        if (TryAsConstant(ctx.Compiler, out var constant)) {
            constant.EmitPushValue(ctx);
            return;
        }

        expr.EmitPushValue(ctx);
        ctx.Proc.Cos();
    }
}

internal sealed class Tan(Location location, DMExpression expr) : DMExpression(location) {
    public override DMComplexValueType ValType => DMValueType.Num;

    public override bool TryAsConstant(DMCompiler compiler, [NotNullWhen(true)] out Constant? constant) {
        if (!expr.TryAsConstant(compiler, out constant)) {
            constant = null;
            return false;
        }

        if (constant is not Number {Value: var x}) {
            x = 0;
            compiler.Emit(WarningCode.FallbackBuiltinArgument, expr.Location,
                "Invalid value treated as 0, tan(0) will always be 0");
        }

        constant = new Number(Location, SharedOperations.Tan(x));
        return true;
    }

    public override void EmitPushValue(ExpressionContext ctx) {
        if (TryAsConstant(ctx.Compiler, out var constant)) {
            constant.EmitPushValue(ctx);
            return;
        }

        expr.EmitPushValue(ctx);
        ctx.Proc.Tan();
    }
}

internal sealed class ArcSin(Location location, DMExpression expr) : DMExpression(location) {
    public override DMComplexValueType ValType => DMValueType.Num;

    public override bool TryAsConstant(DMCompiler compiler, [NotNullWhen(true)] out Constant? constant) {
        if (!expr.TryAsConstant(compiler, out constant)) {
            constant = null;
            return false;
        }

        if (constant is not Number {Value: var x}) {
            x = 0;
            compiler.Emit(WarningCode.FallbackBuiltinArgument, expr.Location,
                "Invalid value treated as 0, arcsin(0) will always be 0");
        }

        if (x is < -1 or > 1) {
            compiler.Emit(WarningCode.BadArgument, expr.Location, $"Invalid value {x}, must be >= -1 and <= 1");
            x = 0;
        }

        constant = new Number(Location, SharedOperations.ArcSin(x));
        return true;
    }

    public override void EmitPushValue(ExpressionContext ctx) {
        if (TryAsConstant(ctx.Compiler, out var constant)) {
            constant.EmitPushValue(ctx);
            return;
        }

        expr.EmitPushValue(ctx);
        ctx.Proc.ArcSin();
    }
}

internal sealed class ArcCos(Location location, DMExpression expr) : DMExpression(location) {
    public override DMComplexValueType ValType => DMValueType.Num;

    public override bool TryAsConstant(DMCompiler compiler, [NotNullWhen(true)] out Constant? constant) {
        if (!expr.TryAsConstant(compiler, out constant)) {
            constant = null;
            return false;
        }

        if (constant is not Number {Value: var x}) {
            x = 0;
            compiler.Emit(WarningCode.FallbackBuiltinArgument, expr.Location,
                "Invalid value treated as 0, arccos(0) will always be 1");
        }

        if (x is < -1 or > 1) {
            compiler.Emit(WarningCode.BadArgument, expr.Location, $"Invalid value {x}, must be >= -1 and <= 1");
            x = 0;
        }

        constant = new Number(Location, SharedOperations.ArcCos(x));
        return true;
    }

    public override void EmitPushValue(ExpressionContext ctx) {
        if (TryAsConstant(ctx.Compiler, out var constant)) {
            constant.EmitPushValue(ctx);
            return;
        }

        expr.EmitPushValue(ctx);
        ctx.Proc.ArcCos();
    }
}

internal sealed class ArcTan(Location location, DMExpression expr) : DMExpression(location) {
    public override DMComplexValueType ValType => DMValueType.Num;

    public override bool TryAsConstant(DMCompiler compiler, [NotNullWhen(true)] out Constant? constant) {
        if (!expr.TryAsConstant(compiler, out constant)) {
            constant = null;
            return false;
        }

        if (constant is not Number {Value: var a}) {
            a = 0;
            compiler.Emit(WarningCode.FallbackBuiltinArgument, expr.Location,
                "Invalid value treated as 0, arctan(0) will always be 0");
        }

        constant = new Number(Location, SharedOperations.ArcTan(a));
        return true;
    }

    public override void EmitPushValue(ExpressionContext ctx) {
        if (TryAsConstant(ctx.Compiler, out var constant)) {
            constant.EmitPushValue(ctx);
            return;
        }

        expr.EmitPushValue(ctx);
        ctx.Proc.ArcTan();
    }
}

internal sealed class ArcTan2(Location location, DMExpression xExpr, DMExpression yExpr) : DMExpression(location) {
    public override DMComplexValueType ValType => DMValueType.Num;

    public override bool TryAsConstant(DMCompiler compiler, [NotNullWhen(true)] out Constant? constant) {
        if (!xExpr.TryAsConstant(compiler, out var xConst) || !yExpr.TryAsConstant(compiler, out var yConst)) {
            constant = null;
            return false;
        }

        if (xConst is not Number {Value: var x}) {
            x = 0;
            compiler.Emit(WarningCode.FallbackBuiltinArgument, xExpr.Location, "Invalid x value treated as 0");
        }

        if (yConst is not Number {Value: var y}) {
            y = 0;
            compiler.Emit(WarningCode.FallbackBuiltinArgument, xExpr.Location, "Invalid y value treated as 0");
        }

        constant = new Number(Location, SharedOperations.ArcTan(x, y));
        return true;
    }

    public override void EmitPushValue(ExpressionContext ctx) {
        if (TryAsConstant(ctx.Compiler, out var constant)) {
            constant.EmitPushValue(ctx);
            return;
        }

        xExpr.EmitPushValue(ctx);
        yExpr.EmitPushValue(ctx);
        ctx.Proc.ArcTan2();
    }
}

internal sealed class Sqrt(Location location, DMExpression expr) : DMExpression(location) {
    public override DMComplexValueType ValType => DMValueType.Num;

    public override bool TryAsConstant(DMCompiler compiler, [NotNullWhen(true)] out Constant? constant) {
        if (!expr.TryAsConstant(compiler, out constant)) {
            constant = null;
            return false;
        }

        if (constant is not Number {Value: var a}) {
            a = 0;
            compiler.Emit(WarningCode.FallbackBuiltinArgument, expr.Location,
                "Invalid value treated as 0, sqrt(0) will always be 0");
        }

        if (a < 0) {
            compiler.Emit(WarningCode.BadArgument, expr.Location,
                $"Cannot get the square root of a negative number ({a})");
        }

        constant = new Number(Location, SharedOperations.Sqrt(a));
        return true;
    }

    public override void EmitPushValue(ExpressionContext ctx) {
        if (TryAsConstant(ctx.Compiler, out var constant)) {
            constant.EmitPushValue(ctx);
            return;
        }

        expr.EmitPushValue(ctx);
        ctx.Proc.Sqrt();
    }
}

internal sealed class Log(Location location, DMExpression expr, DMExpression? baseExpr)
    : DMExpression(location) {
    public override DMComplexValueType ValType => DMValueType.Num;

    public override bool TryAsConstant(DMCompiler compiler, [NotNullWhen(true)] out Constant? constant) {
        if (!expr.TryAsConstant(compiler, out constant)) {
            constant = null;
            return false;
        }

        if (constant is not Number {Value: var value} || value <= 0) {
            value = 1;
            compiler.Emit(WarningCode.BadArgument, expr.Location,
                "Invalid value, must be a number greater than 0");
        }

        if (baseExpr == null) {
            constant = new Number(Location, SharedOperations.Log(value));
            return true;
        }

        if (!baseExpr.TryAsConstant(compiler, out var baseConstant)) {
            constant = null;
            return false;
        }

        if (baseConstant is not Number {Value: var baseValue} || baseValue <= 0) {
            baseValue = 10;
            compiler.Emit(WarningCode.BadArgument, baseExpr.Location,
                "Invalid base, must be a number greater than 0");
        }

        constant = new Number(Location, SharedOperations.Log(value, baseValue));
        return true;
    }

    public override void EmitPushValue(ExpressionContext ctx) {
        if (TryAsConstant(ctx.Compiler, out var constant)) {
            constant.EmitPushValue(ctx);
            return;
        }

        expr.EmitPushValue(ctx);
        if (baseExpr == null) {
            ctx.Proc.LogE();
        } else {
            baseExpr.EmitPushValue(ctx);
            ctx.Proc.Log();
        }
    }
}

internal sealed class Abs(Location location, DMExpression expr) : DMExpression(location) {
    public override DMComplexValueType ValType => DMValueType.Num;

    public override bool TryAsConstant(DMCompiler compiler, [NotNullWhen(true)] out Constant? constant) {
        if (!expr.TryAsConstant(compiler, out constant)) {
            constant = null;
            return false;
        }

        if (constant is not Number {Value: var a}) {
            a = 0;
            compiler.Emit(WarningCode.FallbackBuiltinArgument, expr.Location,
                "Invalid value treated as 0, abs(0) will always be 0");
        }

        constant = new Number(Location, SharedOperations.Abs(a));
        return true;
    }

    public override void EmitPushValue(ExpressionContext ctx) {
        if (TryAsConstant(ctx.Compiler, out var constant)) {
            constant.EmitPushValue(ctx);
            return;
        }

        expr.EmitPushValue(ctx);
        ctx.Proc.Abs();
    }
}
