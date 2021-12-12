using OpenDreamShared.Compiler;

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

        public override ProcPushResult EmitPushProc(DMObject dmObject, DMProc proc) {
            if (!dmObject.HasProc(_identifier)) {
                throw new CompileErrorException(Location, $"Type {dmObject.Path} does not have a proc named \"{_identifier}\"");
            }

            proc.GetProc(_identifier);
            return ProcPushResult.Unconditional;
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

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            proc.PushSelf();
        }

        public override ProcPushResult EmitPushProc(DMObject dmObject, DMProc proc) {
            proc.PushSelf();
            return ProcPushResult.Unconditional;
        }
    }

    // ..
    class ProcSuper : DMExpression {
        public ProcSuper(Location location) : base(location) { }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            throw new CompileErrorException(Location, "attempt to use proc as value");
        }

        public override ProcPushResult EmitPushProc(DMObject dmObject, DMProc proc) {
            proc.PushSuperProc();
            return ProcPushResult.Unconditional;
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

            var _procResult = _target.EmitPushProc(dmObject, proc);

            switch (_procResult) {
                case ProcPushResult.Unconditional:
                    if (_arguments.Length == 0 && _target is ProcSuper) {
                        proc.PushProcArguments();
                    } else {
                        _arguments.EmitPushArguments(dmObject, proc);
                    }
                    proc.Call();
                    break;

                case ProcPushResult.Conditional: {
                    var skipLabel = proc.NewLabelName();
                    var endLabel = proc.NewLabelName();
                    proc.JumpIfNullIdentifier(skipLabel);
                    if (_arguments.Length == 0 && _target is ProcSuper) {
                        proc.PushProcArguments();
                    } else {
                        _arguments.EmitPushArguments(dmObject, proc);
                    }
                    proc.Call();
                    proc.Jump(endLabel);
                    proc.AddLabel(skipLabel);
                    proc.Pop();
                    proc.PushNull();
                    proc.AddLabel(endLabel);
                    break;
                }
            }
        }
    }
}
