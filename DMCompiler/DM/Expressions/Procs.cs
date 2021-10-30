using OpenDreamShared.Compiler;
using System.Collections.Generic;

namespace DMCompiler.DM.Expressions {
    // x() (only the identifier)
    class Proc : DMExpression {
        public readonly string _identifier;

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
    }

    // .
    // This is an LValue _and_ a proc
    class ProcSelf : LValue {
        public ProcSelf()
            : base(null)
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

        static public HashSet<string> const_procs = new() { "rgb", "matrix" };
        static public bool ConstProc(string s) {
            return const_procs.Contains(s);
        }

        public ProcCall(DMExpression target, ArgumentList arguments) {
            _target = target;
            _arguments = arguments;
            IsConst = true;
            foreach (var arg_exprs in arguments.Expressions) {
                if (!arg_exprs.Expr.IsConst) {
                    IsConst = false;
                    break;
                }
            }
            if (target is Proc proc_expr && !ConstProc(proc_expr._identifier)) {
                IsConst = false;
            }

        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            switch (_target) {
                case Proc procTarget: procTarget.UnimplementedCheck(dmObject); break;
                case DereferenceProc derefTarget: derefTarget.UnimplementedCheck(); break;
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
