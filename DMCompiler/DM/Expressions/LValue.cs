using System.Diagnostics.CodeAnalysis;
using DMCompiler.Bytecode;
using DMCompiler.Compiler;

namespace DMCompiler.DM.Expressions;

internal abstract class LValue(DMCompiler compiler, Location location, DreamPath? path) : DMExpression(compiler, location) {
    public override DreamPath? Path { get; } = path;

    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        if (TryAsConstant(out var constant)) { // BYOND also seems to push consts instead of references when possible
            constant.EmitPushValue(dmObject, proc);
            return;
        }

        EmitPushValueNoConstant(dmObject, proc);
    }

    public void EmitPushValueNoConstant(DMObject dmObject, DMProc proc) {
        string endLabel = proc.NewLabelName();

        DMReference reference = EmitReference(dmObject, proc, endLabel);
        proc.PushReferenceValue(reference);

        proc.AddLabel(endLabel);
    }

    public virtual void EmitPushInitial(DMObject dmObject, DMProc proc) {
        Compiler.Emit(WarningCode.BadExpression, Location, $"Can't get initial value of {this}");
        proc.Error();
    }
}

// global
internal class Global(DMCompiler compiler, Location location) : LValue(compiler, location, null) {
    public override DMReference EmitReference(DMObject dmObject, DMProc proc, string endLabel, ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
        Compiler.Emit(WarningCode.BadExpression, Location, "attempt to use `global` as a reference");
        return DMReference.Invalid;
    }
}

// src
internal sealed class Src(DMCompiler compiler, Location location, DreamPath? path) : LValue(compiler, location, path) {
    public override DMComplexValueType ValType => DMValueType.Anything;

    public override DMReference EmitReference(DMObject dmObject, DMProc proc, string endLabel, ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
        return DMReference.Src;
    }

    public override string GetNameof(DMObject dmObject) => "src";
}

// usr
internal sealed class Usr(DMCompiler compiler, Location location) : LValue(compiler, location, DreamPath.Mob) {
    //According to the docs, Usr is a mob. But it will get set to null by coders to clear refs.
    public override DMComplexValueType ValType => (DMValueType.Mob | DMValueType.Null);

    public override DMReference EmitReference(DMObject dmObject, DMProc proc, string endLabel, ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
        return DMReference.Usr;
    }

    public override string GetNameof(DMObject dmObject) => "usr";
}

// args
internal sealed class Args(DMCompiler compiler, Location location) : LValue(compiler, location, DreamPath.List) {
    public override DMReference EmitReference(DMObject dmObject, DMProc proc, string endLabel, ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
        return DMReference.Args;
    }

    public override string GetNameof(DMObject dmObject) => "args";
}

// world
internal sealed class World(DMCompiler compiler, Location location) : LValue(compiler, location, DreamPath.World) {
    public override DMReference EmitReference(DMObject dmObject, DMProc proc, string endLabel, ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
        return DMReference.World;
    }

    public override string GetNameof(DMObject dmObject) => "world";
}

// Identifier of local variable
internal sealed class Local(DMCompiler compiler, Location location, DMProc.LocalVariable localVar) : LValue(compiler, location, localVar.Type) {
    public DMProc.LocalVariable LocalVar { get; } = localVar;

    // TODO: non-const local var static typing
    public override DMComplexValueType ValType => LocalVar.ExplicitValueType ?? DMValueType.Anything;

    public override DMReference EmitReference(DMObject dmObject, DMProc proc, string endLabel, ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
        if (LocalVar.IsParameter) {
            return DMReference.CreateArgument(LocalVar.Id);
        } else {
            return DMReference.CreateLocal(LocalVar.Id);
        }
    }

    public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
        if (LocalVar is DMProc.LocalConstVariable constVar) {
            constant = constVar.Value;
            return true;
        }

        constant = null;
        return false;
    }

    public override void EmitPushInitial(DMObject dmObject, DMProc proc) {
        // This happens silently in BYOND
        Compiler.Emit(WarningCode.PointlessBuiltinCall, Location, "calling initial() on a local variable returns the current value");
        EmitPushValue(dmObject, proc);
    }

    public override string GetNameof(DMObject dmObject) => LocalVar.Name;
}

// Identifier of field
internal sealed class Field(DMCompiler compiler, Location location, DMVariable variable, DMComplexValueType valType) : LValue(compiler, location, variable.Type) {
    public override DMComplexValueType ValType => valType;

    public override void EmitPushInitial(DMObject dmObject, DMProc proc) {
        proc.PushReferenceValue(DMReference.Src);
        proc.PushString(variable.Name);
        proc.Initial();
    }

    public void EmitPushIsSaved(DMProc proc) {
        proc.PushReferenceValue(DMReference.Src);
        proc.PushString(variable.Name);
        proc.IsSaved();
    }

    public override DMReference EmitReference(DMObject dmObject, DMProc proc, string endLabel, ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
        return DMReference.CreateSrcField(variable.Name);
    }

    public override string GetNameof(DMObject dmObject) => variable.Name;

    public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
        if (variable is { IsConst: true, Value: not null }) {
            return variable.Value.TryAsConstant(out constant);
        }

        constant = null;
        return false;
    }

    public override string ToString() {
        return variable.Name;
    }
}

// Id of global field
internal sealed class GlobalField(DMCompiler compiler, Location location, DreamPath? path, int id,  DMComplexValueType valType) : LValue(compiler, location, path) {
    private int Id { get; } = id;

    public override DMComplexValueType ValType => valType;

    public override DMReference EmitReference(DMObject dmObject, DMProc proc, string endLabel, ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
        return DMReference.CreateGlobal(Id);
    }

    public override void EmitPushInitial(DMObject dmObject, DMProc proc) {
        // This happens silently in BYOND
        Compiler.Emit(WarningCode.PointlessBuiltinCall, Location, "calling initial() on a global returns the current value");
        EmitPushValue(dmObject, proc);
    }

    public override string GetNameof(DMObject dmObject) {
        DMVariable global = Compiler.DMObjectTree.Globals[Id];
        return global.Name;
    }

    public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
        DMVariable global = Compiler.DMObjectTree.Globals[Id];
        if (global.IsConst) {
            return global.Value.TryAsConstant(out constant);
        }

        constant = null;
        return false;
    }
}

internal sealed class GlobalVars(DMCompiler compiler, Location location) : LValue(compiler, location, null) {
    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        proc.PushGlobalVars();
    }

    public override string GetNameof(DMObject dmObject) => "vars";
}
