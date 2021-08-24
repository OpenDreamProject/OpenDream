using System;
using OpenDreamShared.Compiler;
using OpenDreamShared.Dream.Procs;

namespace DMCompiler.DM.Expressions {
    // x() (only the identifier)
    class Proc : DMExpression {
        string _identifier;

        public Proc(string identifier) {
            _identifier = identifier;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            throw new CompileErrorException("attempt to use proc as value");
        }

        public override ProcPushResult EmitPushProc(DMObject dmObject, DMProc proc) {
            if (!dmObject.HasProc(_identifier)) {
                throw new CompileErrorException($"Type {dmObject.Path} does not have a proc named `{_identifier}`");
            }

            proc.GetProc(_identifier);
            return ProcPushResult.Unconditional;
        }

        public void UnimplementedCheck(DMObject dmObject) {
            if (dmObject.IsProcUnimplemented(_identifier)) {
                Program.Warning(new CompilerWarning(null, $"{dmObject.Path}.{_identifier}() is not implemented"));
            }
        }

        public DMValueType GetReturnType(DMObject dmObject)
        {
            return dmObject.GetReturnType(_identifier);
        }
    }

    // .
    // This is an LValue _and_ a proc
    class ProcSelf : LValue
    {
        public ProcSelf() : base(null)
        {
            ValType = DMValueType.Null;
        }

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
        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            throw new CompileErrorException("attempt to use proc as value");
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

        public ProcCall(DMExpression target, ArgumentList arguments) {
            _target = target;
            _arguments = arguments;
            if (_target is DereferenceProc derefTarget)
            {
                ValType = derefTarget.GetReturnType();
            }
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            switch (_target) {
                case Proc procTarget:
                    procTarget.UnimplementedCheck(dmObject);
                    ValType = procTarget.GetReturnType(dmObject);
                    break;
                case DereferenceProc derefTarget:
                    derefTarget.UnimplementedCheck();
                    ValType = derefTarget.GetReturnType();
                    break;
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
