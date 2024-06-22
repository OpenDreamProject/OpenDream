using System.Diagnostics.CodeAnalysis;
using DMCompiler.Bytecode;
using DMCompiler.Compiler;

namespace DMCompiler.DM.Expressions {
    abstract class LValue : DMExpression {
        public override DreamPath? Path { get; }

        protected LValue(Location location, DreamPath? path) : base(location) {
            Path = path;
        }

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
            throw new CompileErrorException(Location, $"Can't get initial value of {this}");
        }
    }

    // global
    class Global : LValue {
        public Global(Location location) : base(location, null) { }

        public override DMReference EmitReference(DMObject dmObject, DMProc proc, string endLabel, ShortCircuitMode shortCircuitMode) {
            throw new CompileErrorException(Location, "attempt to use `global` as a reference");
        }
    }

    // src
    sealed class Src(Location location, DreamPath? path) : LValue(location, path) {
        public override DMComplexValueType ValType => DMValueType.Anything;

        public override DMReference EmitReference(DMObject dmObject, DMProc proc, string endLabel, ShortCircuitMode shortCircuitMode) {
            return DMReference.Src;
        }

        public override string GetNameof(DMObject dmObject, DMProc proc) => "src";
    }

    // usr
    sealed class Usr : LValue {
        //According to the docs, Usr is a mob. But it will get set to null by coders to clear refs.
        public override DMComplexValueType ValType => (DMValueType.Mob | DMValueType.Null);

        public Usr(Location location)
            : base(location, DreamPath.Mob) { }

        public override DMReference EmitReference(DMObject dmObject, DMProc proc, string endLabel, ShortCircuitMode shortCircuitMode) {
            return DMReference.Usr;
        }

        public override string GetNameof(DMObject dmObject, DMProc proc) => "usr";
    }

    // args
    sealed class Args : LValue {
        public Args(Location location)
            : base(location, DreamPath.List)
        {}

        public override DMReference EmitReference(DMObject dmObject, DMProc proc, string endLabel, ShortCircuitMode shortCircuitMode) {
            return DMReference.Args;
        }

        public override string GetNameof(DMObject dmObject, DMProc proc) => "args";
    }

    // Identifier of local variable
    sealed class Local : LValue {
        public DMProc.LocalVariable LocalVar { get; }

        // TODO: non-const local var static typing
        public override DMComplexValueType ValType => LocalVar.ExplicitValueType ?? DMValueType.Anything;

        public Local(Location location, DMProc.LocalVariable localVar)
            : base(location, localVar.Type) {
            LocalVar = localVar;
        }

        public override DMReference EmitReference(DMObject dmObject, DMProc proc, string endLabel, ShortCircuitMode shortCircuitMode) {
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

        public override string GetNameof(DMObject dmObject, DMProc proc) => LocalVar.Name;
    }

    // Identifier of field
    sealed class Field(Location location, DMVariable variable, DMComplexValueType valType)
        : LValue(location, variable.Type) {
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

        public override DMReference EmitReference(DMObject dmObject, DMProc proc, string endLabel, ShortCircuitMode shortCircuitMode) {
            return DMReference.CreateSrcField(variable.Name);
        }

        public override string GetNameof(DMObject dmObject, DMProc proc) => variable.Name;

        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (variable.IsConst && variable.Value != null) {
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
    sealed class GlobalField(Location location, DreamPath? path, int id, DMComplexValueType valType) : LValue(location, path) {
        private int Id { get; } = id;

        public override DMComplexValueType ValType => valType;

        public void EmitPushIsSaved(DMProc proc) {
            throw new CompileErrorException(Location, "issaved() on globals is unimplemented");
        }

        public override DMReference EmitReference(DMObject dmObject, DMProc proc, string endLabel, ShortCircuitMode shortCircuitMode) {
            return DMReference.CreateGlobal(Id);
        }

        public override void EmitPushInitial(DMObject dmObject, DMProc proc) {
            // This happens silently in BYOND
            DMCompiler.Emit(WarningCode.PointlessBuiltinCall, Location, "calling initial() on a global returns the current value");
            EmitPushValue(dmObject, proc);
        }

        public override string GetNameof(DMObject dmObject, DMProc proc) {
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

    sealed class GlobalVars : LValue {
        public GlobalVars(Location location)
            : base(location, null) {
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            proc.PushGlobalVars();
        }

        public override string GetNameof(DMObject dmObject, DMProc proc) => "vars";
    }
}
