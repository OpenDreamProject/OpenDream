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

        public Local(Location location, DreamPath? path, string name)
            : base(location, path) {
            Name = name;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            proc.PushLocalVariable(Name);
        }
    }

    // Identifier of field
    class Field : LValue {
        string Name { get; }

        public Field(Location location, DreamPath? path, string name)
            : base(location, path) {
            Name = name;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            proc.GetIdentifier(Name);
        }

        public void EmitPushInitial(DMProc proc) {
            proc.PushSrc();
            proc.Initial(Name);
        }

        public void EmitPushIsSaved(DMProc proc) {
            proc.PushSrc();
            proc.IsSaved(Name);
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

        public void EmitPushInitial(DMProc proc) {
            throw new CompileErrorException(Location, "initial() on globals is unimplemented");
        }

        public void EmitPushIsSaved(DMProc proc) {
            throw new CompileErrorException(Location, "issaved() on globals is unimplemented");
        }
    }

}
