using System.Diagnostics.CodeAnalysis;
using DMCompiler.Bytecode;
using DMCompiler.Compiler;

namespace DMCompiler.DM.Expressions;

internal abstract class LValue(Location location, DreamPath? path) : DMExpression(location) {
    public override DreamPath? Path { get; } = path;

    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        string endLabel = proc.NewLabelName();

        DMReference reference = EmitReference(dmObject, proc, endLabel);
        proc.PushReferenceValue(reference);

        proc.AddLabel(endLabel);
    }

    public virtual void EmitPushInitial(DMObject dmObject, DMProc proc) {
        DMCompiler.Emit(WarningCode.BadExpression, Location, $"Can't get initial value of {this}");
        proc.Error();
    }
}

// global
internal class Global(Location location) : LValue(location, null) {
    public override DMReference EmitReference(DMObject dmObject, DMProc proc, string endLabel, ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
        DMCompiler.Emit(WarningCode.BadExpression, Location, "attempt to use `global` as a reference");
        return DMReference.Invalid;
    }
}

// src
internal sealed class Src(Location location, DreamPath? path) : LValue(location, path) {
    public override DMComplexValueType ValType => DMValueType.Anything;

    public override DMReference EmitReference(DMObject dmObject, DMProc proc, string endLabel, ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
        return DMReference.Src;
    }

    public override string GetNameof(DMObject dmObject) => "src";
}

// usr
internal sealed class Usr(Location location) : LValue(location, DreamPath.Mob) {
    //According to the docs, Usr is a mob. But it will get set to null by coders to clear refs.
    public override DMComplexValueType ValType => (DMValueType.Mob | DMValueType.Null);

    public override DMReference EmitReference(DMObject dmObject, DMProc proc, string endLabel, ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
        return DMReference.Usr;
    }

    public override string GetNameof(DMObject dmObject) => "usr";
}

// args
internal sealed class Args(Location location) : LValue(location, DreamPath.List) {
    public override DMReference EmitReference(DMObject dmObject, DMProc proc, string endLabel, ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
        return DMReference.Args;
    }

    public override string GetNameof(DMObject dmObject) => "args";
}

// Identifier of local variable
internal sealed class Local(Location location, DMProc.LocalVariable localVar) : LValue(location, localVar.Type) {
    public DMProc.LocalVariable LocalVar { get; }

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
        DMCompiler.Emit(WarningCode.PointlessBuiltinCall, Location, "calling initial() on a local variable returns the current value");
        EmitPushValue(dmObject, proc);
    }

    public override string GetNameof(DMObject dmObject) => LocalVar.Name;
}

// Identifier of field
internal sealed class Field(Location location, DMVariable variable) : LValue(location, variable.Type) {
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
internal sealed class GlobalField(Location location, DreamPath? path, int id) : LValue(location, path) {
    private int Id { get; } = id;

    public override DMComplexValueType ValType => valType;

    public override DMReference EmitReference(DMObject dmObject, DMProc proc, string endLabel, ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
        return DMReference.CreateGlobal(Id);
    }

    public override void EmitPushInitial(DMObject dmObject, DMProc proc) {
        // This happens silently in BYOND
        DMCompiler.Emit(WarningCode.PointlessBuiltinCall, Location, "calling initial() on a global returns the current value");
        EmitPushValue(dmObject, proc);
    }

    public override string GetNameof(DMObject dmObject) {
        DMVariable global = DMObjectTree.Globals[Id];
        return global.Name;
    }

    public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
        DMVariable global = DMObjectTree.Globals[Id];
        if (global.IsConst) {
            return global.Value.TryAsConstant(out constant);
        }

        constant = null;
        return false;
    }
}

internal sealed class GlobalVars(Location location) : LValue(location, null) {
    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        proc.PushGlobalVars();
    }

    public override string GetNameof(DMObject dmObject) => "vars";
}
