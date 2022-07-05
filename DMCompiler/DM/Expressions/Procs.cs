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
            if (dmObject.HasProc(_identifier)) {
                return (DMReference.CreateSrcProc(_identifier), false);
            } else if (DMObjectTree.TryGetGlobalProc(_identifier, out DMProc globalProc)) {
                return (DMReference.CreateGlobalProc(globalProc.Id), false);
            }

            throw new CompileErrorException(Location, $"Type {dmObject.Path} does not have a proc named \"{_identifier}\"");
        }

        public DMProc GetProc(DMObject dmObject)
        {
            var procId = dmObject.GetProcs(_identifier)?[^1];
            return procId is null ? null : DMObjectTree.AllProcs[procId.Value];
        }
    }

    class GlobalProc : DMExpression {
        string _name;

        public GlobalProc(Location location, string name) : base(location) {
            _name = name;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            throw new CompileErrorException(Location, "attempt to use proc as value");
        }

        public override (DMReference Reference, bool Conditional) EmitReference(DMObject dmObject, DMProc proc) {
            DMProc globalProc = GetProc();

            return (DMReference.CreateGlobalProc(globalProc.Id), false);
        }

        public DMProc GetProc() {
            if (!DMObjectTree.TryGetGlobalProc(_name, out DMProc globalProc)) {
                throw new CompileErrorException(Location, $"No global proc named \"{_name}\"");
            }

            return globalProc;
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
            if ((proc.Attributes & ProcAttributes.IsOverride) != ProcAttributes.IsOverride)
            {
                DMCompiler.Warning(new CompilerWarning(Location, "Calling parents via ..() in a proc definition does nothing"));
            }
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
                GlobalProc procTarget => (null, procTarget.GetProc()),
                DereferenceProc derefTarget => derefTarget.GetProc(),
                _ => (null, null)
            };
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            (DMObject procOwner, DMProc targetProc) = GetTargetProc(dmObject);
            if ((targetProc?.Attributes & ProcAttributes.Unimplemented) == ProcAttributes.Unimplemented) {
                DMCompiler.UnimplementedWarning(Location, $"{procOwner?.Path.ToString() ?? "/"}.{targetProc.Name}() is not implemented");
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

        public override (DMReference Reference, bool Conditional) EmitReference(DMObject dmObject, DMProc proc) {
            _arguments.EmitPushArguments(dmObject, proc);
            (DMObject _, DMProc targetProc) = GetTargetProc(dmObject);
            if (dmObject.HasProc(targetProc.Name)) {
                return (DMReference.CreateSrcProc(targetProc.Name), false);
            } else if (DMObjectTree.TryGetGlobalProc(targetProc.Name, out _)) {
                return (DMReference.CreateGlobalProc(targetProc.Name), false);
            }

            throw new CompileErrorException(Location,
                $"Type {dmObject.Path} does not have a proc named \"{targetProc.Name}\"");
        }

        public override bool TryAsJsonRepresentation(out object json) {
            json = null;
            DMCompiler.UnimplementedWarning(Location, $"DMM overrides for expression {GetType()} are not implemented");
            return true; //TODO
        }
    }
}
