using OpenDreamShared.Compiler;
using OpenDreamShared.Dream.Procs;

namespace DMCompiler.DM.Expressions {
    // x() (only the identifier)
    class Proc : DMExpression {
        string _identifier;

        public Proc(Location location, string identifier) : base(location) {
            _identifier = identifier;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            throw new CompileErrorException(Location, "attempt to use proc as value");
        }

        public override (DMReference Reference, bool Conditional) EmitReference(DMObject dmObject, DMProc proc) {
            if (!dmObject.HasProc(_identifier)) {
                throw new CompileErrorException(Location, $"Type {dmObject.Path} does not have a proc named \"{_identifier}\"");
            }

            return (DMReference.CreateSrcProc(_identifier), false);
        }

        public DMProc GetProc(DMObject dmObject) {
            return dmObject.GetProcs(_identifier)?[^1];
        }
    }

    // .
    // This is an LValue _and_ a proc
    class ProcSelf : LValue {
        public ProcSelf(Location location)
            : base(location, null)
        {}

        public override (DMReference Reference, bool Conditional) EmitReference(DMObject dmObject, DMProc proc) {
            return (DMReference.Self, false);
        }
    }

    // ..
    class ProcSuper : DMExpression {
        public ProcSuper(Location location) : base(location) { }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            throw new CompileErrorException(Location, "attempt to use proc as value");
        }

        public override (DMReference Reference, bool Conditional) EmitReference(DMObject dmObject, DMProc proc) {
            return (DMReference.SuperProc, false);
        }
    }

    // x(y, z, ...)
    class ProcCall : DMExpression {
        DMExpression _target;
        ArgumentList _arguments;

        public ProcCall(Location location, DMExpression target, ArgumentList arguments) : base(location) {
            _target = target;
            _arguments = arguments;
        }

        public (DMObject ProcOwner, DMProc Proc) GetTargetProc(DMObject dmObject) {
            return _target switch {
                Proc procTarget => (dmObject, procTarget.GetProc(dmObject)),
                DereferenceProc derefTarget => derefTarget.GetProc(),
                _ => (null, null)
            };
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            (DMObject procOwner, DMProc targetProc) = GetTargetProc(dmObject);
            if (!DMCompiler.Settings.SuppressUnimplementedWarnings && targetProc?.Unimplemented == true) {
                DMCompiler.Warning(new CompilerWarning(Location, $"{procOwner.Path}.{targetProc.Name}() is not implemented"));
            }

            (DMReference procRef, bool conditional) = _target.EmitReference(dmObject, proc);

            if (conditional) {
                var skipLabel = proc.NewLabelName();
                proc.JumpIfNullDereference(procRef, skipLabel);
                if (_arguments.Length == 0 && _target is ProcSuper) {
                    proc.PushProcArguments();
                } else {
                    _arguments.EmitPushArguments(dmObject, proc);
                }
                proc.Call(procRef);
                proc.AddLabel(skipLabel);
            } else {
                if (_arguments.Length == 0 && _target is ProcSuper) {
                    proc.PushProcArguments();
                } else {
                    _arguments.EmitPushArguments(dmObject, proc);
                }
                proc.Call(procRef);
            }
        }
    }
}
