using DMCompiler.Bytecode;
using System.Diagnostics.CodeAnalysis;
using DMCompiler.Compiler;
using DMCompiler.DM.Expressions;

namespace DMCompiler.DM;

internal abstract class DMExpression(Location location) {
    public Location Location = location;

    public virtual DMComplexValueType ValType => DMValueType.Anything;

    // Attempt to convert this expression into a Constant expression
    public virtual bool TryAsConstant(DMCompiler compiler, [NotNullWhen(true)] out Constant? constant) {
        constant = null;
        return false;
    }

    // Attempt to create a json-serializable version of this expression
    public virtual bool TryAsJsonRepresentation(DMCompiler compiler, out object? json) {
        // If this can be const-folded, then we can represent this as JSON
        if (TryAsConstant(compiler, out var constant))
            return constant.TryAsJsonRepresentation(compiler, out json);

        json = null;
        return false;
    }

    // Emits code that pushes the result of this expression to the proc's stack
    // May throw if this expression is unable to be pushed to the stack
    public abstract void EmitPushValue(ExpressionContext ctx);

    public enum ShortCircuitMode {
        // If a dereference is short-circuited due to a null conditional, the short-circuit label should be jumped to with null NOT on top of the stack
        PopNull,

        // If a dereference is short-circuited due to a null conditional, the short-circuit label should be jumped to with null still on the top of the stack
        KeepNull
    }

    public virtual bool CanReferenceShortCircuit() => false;

    // Emits a reference that is to be used in an opcode that assigns/gets a value
    // May throw if this expression is unable to be referenced
    // The emitted code will jump to endLabel after pushing `null` to the stack in the event of a short-circuit
    public virtual DMReference EmitReference(ExpressionContext ctx, string endLabel, ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
        ctx.Compiler.Emit(WarningCode.BadExpression, Location, "attempt to reference r-value");
        return DMReference.Invalid;
    }

    public virtual void EmitPushIsSaved(ExpressionContext ctx) {
        ctx.Compiler.Emit(WarningCode.BadArgument, Location, $"can't get issaved() value of {this}");
        ctx.Proc.PushNullAndError();
    }

    /// <summary>
    /// Gets the canonical name of the expression if it exists.
    /// </summary>
    /// <returns>The name of the expression, or <c>null</c> if it does not have one.</returns>
    public virtual string? GetNameof(ExpressionContext ctx) => null;

    /// <summary>
    /// Determines whether the expression returns an ambiguous path.
    /// </summary>
    /// <remarks>Dereferencing these expressions will always skip validation via the "expr:y" operation.</remarks>
    public virtual bool PathIsFuzzy => false;

    public virtual DreamPath? Path => null;

    public virtual DreamPath? NestedPath => Path;
}

// (a, b, c, ...)
// This isn't an expression, it's just a helper class for working with argument lists
internal sealed class ArgumentList(Location location, (string? Name, DMExpression Expr)[] expressions, bool isKeyed) {
    public readonly (string? Name, DMExpression Expr)[] Expressions = expressions;
    public int Length => Expressions.Length;
    public Location Location = location;

    public (DMCallArgumentsType Type, int StackSize) EmitArguments(ExpressionContext ctx, DMProc? targetProc) {
        if (Expressions.Length == 0) {
            return (DMCallArgumentsType.None, 0);
        }

        if (Expressions[0].Expr is Arglist arglist) {
            if (Expressions[0].Name != null)
                ctx.Compiler.Emit(WarningCode.BadArgument, arglist.Location, "arglist cannot be a named argument");

            arglist.EmitPushArglist(ctx);
            return (DMCallArgumentsType.FromArgumentList, 1);
        }

        // TODO: Named arguments must come after all ordered arguments
        int stackCount = 0;
        for (var index = 0; index < Expressions.Length; index++) {
            (string? name, DMExpression expr) = Expressions[index];

            if (targetProc != null)
                VerifyArgType(ctx.Compiler, targetProc, index, name, expr);

            if (isKeyed) {
                if (name != null) {
                    ctx.Proc.PushString(name);
                } else {
                    ctx.Proc.PushNull();
                }
            }

            expr.EmitPushValue(ctx);
            stackCount += isKeyed ? 2 : 1;
        }

        return (isKeyed ? DMCallArgumentsType.FromStackKeyed : DMCallArgumentsType.FromStack, stackCount);
    }

    private void VerifyArgType(DMCompiler compiler, DMProc targetProc, int index, string? name, DMExpression expr) {
        // TODO: See if the static typechecking can be improved
        // Also right now we don't care if the arg is Anything
        // TODO: Make a separate "UnsetStaticType" pragma for whether we should care if it's Anything
        // TODO: We currently silently avoid typechecking "call()()" and "new" args (NewPath is handled)
        // TODO: We currently don't handle variadic args (e.g. min())
        // TODO: Dereference.CallOperation does not pass targetProc

        DMProc.LocalVariable? param;
        if (name != null) {
            targetProc.TryGetParameterByName(name, out param);
        } else {
            targetProc.TryGetParameterAtIndex(index, out param);
        }

        if (param == null) {
            // TODO: Remove this check once variadic args are properly supported
            if (targetProc.Name != "animate" && index < targetProc.Parameters.Count) {
                compiler.Emit(WarningCode.InvalidVarType, expr.Location,
                    $"{targetProc.Name}(...): Unknown argument {(name is null ? $"at index {index}" : $"\"{name}\"")}, typechecking failed");
            }

            return;
        }

        DMComplexValueType paramType = param.ExplicitValueType ?? DMValueType.Anything;

        if (!expr.ValType.IsAnything && !paramType.MatchesType(compiler, expr.ValType)) {
            compiler.Emit(WarningCode.InvalidVarType, expr.Location,
                $"{targetProc.Name}(...) argument \"{param.Name}\": Invalid var value type {expr.ValType}, expected {paramType}");
        }
    }
}

internal readonly struct ExpressionContext(DMCompiler compiler, DMObject type, DMProc proc) {
    public readonly DMCompiler Compiler = compiler;
    public readonly DMObject Type = type;
    public readonly DMProc Proc = proc;

    public DMObjectTree ObjectTree => Compiler.DMObjectTree;
}
