using Content.Shared.Dream;

namespace Content.Compiler.DM.Expressions
{
    abstract class LValue : DMExpression {
        public override DreamPath? Path => _path;
        DreamPath? _path;

        public LValue(DreamPath? path) {
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
        public Src(DreamPath? path)
            : base(path)
        {}

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            proc.PushSrc();
        }
    }

    // usr
    class Usr : LValue {
        public Usr()
            : base(DreamPath.Mob)
        {}

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            proc.PushUsr();
        }
    }

    // args
    class Args : LValue {
        public Args()
            : base(DreamPath.List)
        {}

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            proc.GetIdentifier("args");
        }
    }

    // Identifier of local variable
    class Local : LValue {
        string Name { get; }

        public Local(DreamPath? path, string name)
            : base(path) {
            Name = name;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            proc.PushLocalVariable(Name);
        }
    }

    // Identifier of field (potentially a global variable)
    class Field : LValue {
        string Name { get; }

        public Field(DreamPath? path, string name)
            : base(path) {
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

}
