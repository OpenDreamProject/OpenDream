using OpenDreamShared.Compiler;
using OpenDreamShared.Dream;

namespace DMCompiler.DM.Expressions {
    abstract class LValue : DMExpression {
        public override DreamPath? Path => _path;
        DreamPath? _path;

        public LValue(Location location, DreamPath? path) : base(location) {
            _path = path;
        }

        // At the moment this generally always matches EmitPushValue for any modifiable type
        public override IdentifierPushResult EmitIdentifier(DMObject dmObject, DMProc proc) {
            EmitPushValue(dmObject, proc);
            return IdentifierPushResult.Unconditional;
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

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            proc.PushSrc();
        }
    }

    // usr
    class Usr : LValue {
        public Usr(Location location)
            : base(location, DreamPath.Mob)
        {}

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            proc.PushUsr();
        }
    }

    // args
    class Args : LValue {
        public Args(Location location)
            : base(location, DreamPath.List)
        {}

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            proc.GetIdentifier("args");
        }
    }

    // Identifier of local variable
    class Local : LValue {
        string Name { get; }
        DMProc.LocalVariable LocalVar { get; }

        public Local(Location location, DMProc.LocalVariable localVar, string name)
            : base(location, localVar.Type) {
            Name = name;
            LocalVar = localVar;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            proc.PushLocalVariable(Name);
        }

        public override bool TryAsConstant(out Constant constant) {
            if (LocalVar is DMProc.LocalConstVariable constVar) {
                constant = constVar.Value;
                return true;
            }

            constant = null;
            return false;
        }
    }

    // Identifier of field
    class Field : LValue {
        DMVariable Variable;

        public Field(Location location, DMVariable variable)
            : base(location, variable.Type) {
            Variable = variable;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            proc.GetIdentifier(Variable.Name);
        }

        public override void EmitPushInitial(DMObject dmObject, DMProc proc) {
            proc.PushSrc();
            proc.Initial(Variable.Name);
        }

        public void EmitPushIsSaved(DMProc proc) {
            proc.PushSrc();
            proc.IsSaved(Variable.Name);
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

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            proc.GetGlobal(Id);
        }

        public void EmitPushIsSaved(DMProc proc) {
            throw new CompileErrorException(Location, "issaved() on globals is unimplemented");
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
