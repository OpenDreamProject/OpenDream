using OpenDreamShared.Compiler;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;

namespace DMCompiler.DM.Expressions {
    abstract class LValue : DMExpression {
        public override DreamPath? Path => _path;
        DreamPath? _path;

        public LValue(Location location, DreamPath? path) : base(location) {
            _path = path;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            (DMReference reference, bool conditional) = EmitReference(dmObject, proc);

            if (conditional) {
                string skipLabel = proc.NewLabelName();

                proc.JumpIfNullDereference(reference, skipLabel);
                proc.PushReferenceValue(reference);
                proc.AddLabel(skipLabel);
            } else {
                proc.PushReferenceValue(reference);
            }
        }

        public virtual void EmitPushInitial(DMObject dmObject, DMProc proc) {
            throw new CompileErrorException(Location, $"Can't get initial value of {this}");
        }
    }

    // src
    class Src : LValue {
        public Src(Location location, DreamPath? path)
            : base(location, path)
        {}

        public override (DMReference Reference, bool Conditional) EmitReference(DMObject dmObject, DMProc proc) {
            return (DMReference.Src, false);
        }
    }

    // usr
    class Usr : LValue {
        public Usr(Location location)
            : base(location, DreamPath.Mob)
        {}

        public override (DMReference Reference, bool Conditional) EmitReference(DMObject dmObject, DMProc proc) {
            return (DMReference.Usr, false);
        }
    }

    // args
    class Args : LValue {
        public Args(Location location)
            : base(location, DreamPath.List)
        {}

        public override (DMReference Reference, bool Conditional) EmitReference(DMObject dmObject, DMProc proc) {
            return (DMReference.Args, false);
        }
    }

    // Identifier of local variable
    class Local : LValue {
        DMProc.LocalVariable LocalVar { get; }

        public Local(Location location, DMProc.LocalVariable localVar)
            : base(location, localVar.Type) {
            LocalVar = localVar;
        }

        public override (DMReference Reference, bool Conditional) EmitReference(DMObject dmObject, DMProc proc) {
            return (DMReference.CreateLocal(LocalVar.Id), false);
        }

        public override bool TryAsConstant(out Constant constant) {
            if (LocalVar is DMProc.LocalConstVariable constVar) {
                constant = constVar.Value;
                return true;
            }

            constant = null;
            return false;
        }

        public override void EmitPushInitial(DMObject dmObject, DMProc proc) {
            // This happens silently in BYOND
            DMCompiler.Warning(new CompilerWarning(Location, "calling initial() on a local variable returns the current value"));
            EmitPushValue(dmObject, proc);
        }
    }

    // Identifier of field
    class Field : LValue {
        DMVariable Variable;

        public Field(Location location, DMVariable variable)
            : base(location, variable.Type) {
            Variable = variable;
        }

        public override void EmitPushInitial(DMObject dmObject, DMProc proc) {
            proc.PushReferenceValue(DMReference.Src);
            proc.Initial(Variable.Name);
        }

        public void EmitPushIsSaved(DMProc proc) {
            proc.PushReferenceValue(DMReference.Src);
            proc.IsSaved(Variable.Name);
        }

        public override (DMReference Reference, bool Conditional) EmitReference(DMObject dmObject, DMProc proc) {
            return (DMReference.CreateSrcField(Variable.Name), false);
        }

        public override bool TryAsConstant(out Constant constant) {
            if (Variable.IsConst && Variable.Value != null) {
                return Variable.Value.TryAsConstant(out constant);
            }

            constant = null;
            return false;
        }
    }

    // Id of global field
    class GlobalField : LValue {
        int Id { get; }

        public GlobalField(Location location, DreamPath? path, int id)
            : base(location, path) {
            Id = id;
        }

        public void EmitPushIsSaved(DMProc proc) {
            throw new CompileErrorException(Location, "issaved() on globals is unimplemented");
        }

        public override (DMReference Reference, bool Conditional) EmitReference(DMObject dmObject, DMProc proc) {
            return (DMReference.CreateGlobal(Id), false);
        }

        public override bool TryAsConstant(out Constant constant) {
            DMVariable global = DMObjectTree.Globals[Id];
            if (global.IsConst) {
                return global.Value.TryAsConstant(out constant);
            }

            constant = null;
            return false;
        }
    }

}
