using System.Diagnostics.CodeAnalysis;
using DMCompiler.Bytecode;
using DMCompiler.Compiler;

namespace DMCompiler.DM.Expressions;

internal abstract class LValue(Location location, DreamPath? path) : DMExpression(location) {
    public override DreamPath? Path { get; } = path;

    public override void EmitPushValue(ExpressionContext ctx) {
        if (TryAsConstant(ctx.Compiler, out var constant)) { // BYOND also seems to push consts instead of references when possible
            constant.EmitPushValue(ctx);
            return;
        }

        EmitPushValueNoConstant(ctx);
    }

    public void EmitPushValueNoConstant(ExpressionContext ctx) {
        string endLabel = ctx.Proc.NewLabelName();

        DMReference reference = EmitReference(ctx, endLabel);
        ctx.Proc.PushReferenceValue(reference);

        ctx.Proc.AddLabel(endLabel);
    }

    public abstract void EmitPushInitial(ExpressionContext ctx);
}

/// <summary>
/// Used when there was an error regarding L-Values
/// </summary>
/// <remarks>Emit an error code before creating!</remarks>
internal sealed class BadLValue(Location location) : LValue(location, null) {
    public override void EmitPushValue(ExpressionContext ctx) {
        // It's normal to have this expression exist when there are errors in the code
        // But in the runtime we say it's a compiler bug because the compiler should never have output it
        ctx.Proc.PushString("Encountered a bad LValue (compiler bug!)");
        ctx.Proc.Throw();
    }

    public override void EmitPushInitial(ExpressionContext ctx) {
        ctx.Compiler.Emit(WarningCode.BadExpression, Location, "Can't get initial value of a bad LValue (compiler bug!)");
        ctx.Proc.Throw();
    }
}

// global
internal class Global(Location location) : LValue(location, null) {
    public override DMReference EmitReference(ExpressionContext ctx, string endLabel,
        ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
        ctx.Compiler.Emit(WarningCode.BadExpression, Location, "attempt to use `global` as a reference");
        return DMReference.Invalid;
    }

    public override void EmitPushInitial(ExpressionContext ctx) {
        ctx.Compiler.Emit(WarningCode.BadExpression, Location, "Can't get initial value of `global`");
        ctx.Proc.PushNullAndError();
    }
}

// src
internal sealed class Src(Location location, DreamPath? path) : LValue(location, path) {
    public override DMComplexValueType ValType => DMValueType.Anything;

    public override DMReference EmitReference(ExpressionContext ctx, string endLabel,
        ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
        return DMReference.Src;
    }

    public override void EmitPushInitial(ExpressionContext ctx) {
        ctx.Compiler.Emit(WarningCode.PointlessBuiltinCall, Location, "calling initial() on `src` returns the current value");
        EmitPushValue(ctx);
    }

    public override string GetNameof(ExpressionContext ctx) => "src";
}

// usr
internal sealed class Usr(Location location) : LValue(location, DreamPath.Mob) {
    //According to the docs, Usr is a mob. But it will get set to null by coders to clear refs.
    public override DMComplexValueType ValType => (DMValueType.Mob | DMValueType.Null);

    public override DMReference EmitReference(ExpressionContext ctx, string endLabel,
        ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
        return DMReference.Usr;
    }

    public override void EmitPushInitial(ExpressionContext ctx) {
        ctx.Compiler.Emit(WarningCode.PointlessBuiltinCall, Location, "calling initial() on `usr` returns the current value");
        EmitPushValue(ctx);
    }

    public override string GetNameof(ExpressionContext ctx) => "usr";
}

// args
internal sealed class Args(Location location) : LValue(location, DreamPath.List) {
    public override DMReference EmitReference(ExpressionContext ctx, string endLabel,
        ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
        return DMReference.Args;
    }

    public override void EmitPushInitial(ExpressionContext ctx) {
        ctx.Compiler.Emit(WarningCode.PointlessBuiltinCall, Location, "calling initial() on `args` returns the current value");
        EmitPushValue(ctx);
    }

    public override string GetNameof(ExpressionContext ctx) => "args";
}

// callee
internal sealed class Callee(Location location) : LValue(location, DreamPath.Callee) {
    public override DMReference EmitReference(ExpressionContext ctx, string endLabel,
        ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
        return DMReference.Callee;
    }

    public override void EmitPushInitial(ExpressionContext ctx) {
        // This happens silently in BYOND
        ctx.Compiler.Emit(WarningCode.PointlessBuiltinCall, Location, "calling initial() on constant var `callee` is pointless");
        EmitPushValue(ctx);
    }

    public override string GetNameof(ExpressionContext ctx) => "callee";
}

// caller
internal sealed class Caller(Location location) : LValue(location, DreamPath.Callee) {
    public override DMReference EmitReference(ExpressionContext ctx, string endLabel,
        ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
        return DMReference.Caller;
    }

    public override void EmitPushInitial(ExpressionContext ctx) {
        // This happens silently in BYOND
        ctx.Compiler.Emit(WarningCode.PointlessBuiltinCall, Location, "calling initial() on constant var `caller` is pointless");
        EmitPushValue(ctx);
    }

    public override string GetNameof(ExpressionContext ctx) => "caller";
}

// world
internal sealed class World(Location location) : LValue(location, DreamPath.World) {
    public override DMReference EmitReference(ExpressionContext ctx, string endLabel,
        ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
        return DMReference.World;
    }

    public override void EmitPushInitial(ExpressionContext ctx) {
        // This happens silently in BYOND
        ctx.Compiler.Emit(WarningCode.PointlessBuiltinCall, Location, "calling initial() on constant var `world` is pointless");
        EmitPushValue(ctx);
    }

    public override string GetNameof(ExpressionContext ctx) => "world";
}

// Identifier of local variable
internal sealed class Local(Location location, DMProc.LocalVariable localVar) : LValue(location, localVar.Type) {
    public DMProc.LocalVariable LocalVar { get; } = localVar;

    // TODO: non-const local var static typing
    public override DMComplexValueType ValType => LocalVar.ExplicitValueType ?? DMValueType.Anything;

    public override DMReference EmitReference(ExpressionContext ctx, string endLabel,
        ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
        if (LocalVar.IsParameter) {
            return DMReference.CreateArgument(LocalVar.Id);
        } else {
            return DMReference.CreateLocal(LocalVar.Id);
        }
    }

    public override bool TryAsConstant(DMCompiler compiler, [NotNullWhen(true)] out Constant? constant) {
        if (LocalVar is DMProc.LocalConstVariable constVar) {
            constant = constVar.Value;
            return true;
        }

        constant = null;
        return false;
    }

    public override void EmitPushInitial(ExpressionContext ctx) {
        // This happens silently in BYOND
        ctx.Compiler.Emit(WarningCode.PointlessBuiltinCall, Location, "calling initial() on a local variable returns the current value");
        EmitPushValue(ctx);
    }

    public override string GetNameof(ExpressionContext ctx) => LocalVar.Name;

    public override void EmitPushIsSaved(ExpressionContext ctx) {
        ctx.Proc.PushFloat(0);
    }
}

// Identifier of field
internal sealed class Field(Location location, DMVariable variable, DMComplexValueType valType) : LValue(location, variable.Type) {
    public bool IsConst { get; } = variable.IsConst;
    public override DMComplexValueType ValType => valType;

    public override void EmitPushInitial(ExpressionContext ctx) {
        ctx.Proc.PushReferenceValue(DMReference.Src);
        ctx.Proc.PushString(variable.Name);
        ctx.Proc.Initial();

        if (variable.IsConst) {
            // This happens silently in BYOND
            ctx.Compiler.Emit(WarningCode.PointlessBuiltinCall, Location, "calling initial() on a constant variable is pointless");
        }
    }

    public override void EmitPushIsSaved(ExpressionContext ctx) {
        ctx.Proc.PushReferenceValue(DMReference.Src);
        ctx.Proc.PushString(variable.Name);
        ctx.Proc.IsSaved();
    }

    public override DMReference EmitReference(ExpressionContext ctx, string endLabel,
        ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
        return DMReference.CreateSrcField(variable.Name);
    }

    public override string GetNameof(ExpressionContext ctx) => variable.Name;

    public override bool TryAsConstant(DMCompiler compiler, [NotNullWhen(true)] out Constant? constant) {
        return variable.TryAsConstant(compiler, out constant);
    }

    public override string ToString() {
        return variable.Name;
    }
}

// Id of global field
internal sealed class GlobalField(Location location, DreamPath? path, int id,  DMComplexValueType valType) : LValue(location, path) {
    private int Id { get; } = id;

    public override DMComplexValueType ValType => valType;

    public override DMReference EmitReference(ExpressionContext ctx, string endLabel,
        ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
        return DMReference.CreateGlobal(Id);
    }

    public override void EmitPushInitial(ExpressionContext ctx) {
        // This happens silently in BYOND
        ctx.Compiler.Emit(WarningCode.PointlessBuiltinCall, Location, "calling initial() on a global returns the current value");
        EmitPushValue(ctx);
    }

    public override string GetNameof(ExpressionContext ctx) {
        DMVariable global = ctx.ObjectTree.Globals[Id];
        return global.Name;
    }

    public override bool TryAsConstant(DMCompiler compiler, [NotNullWhen(true)] out Constant? constant) {
        DMVariable global = compiler.DMObjectTree.Globals[Id];

        return global.TryAsConstant(compiler, out constant);
    }
}

internal sealed class GlobalVars(Location location) : LValue(location, null) {
    public override void EmitPushValue(ExpressionContext ctx) {
        ctx.Proc.PushGlobalVars();
    }

    public override void EmitPushInitial(ExpressionContext ctx) {
        // This happens silently in BYOND
        ctx.Compiler.Emit(WarningCode.PointlessBuiltinCall, Location, "calling initial() on `global.vars` returns the current value");
        EmitPushValue(ctx);
    }

    public override string GetNameof(ExpressionContext ctx) => "vars";
}
