using System.Diagnostics.CodeAnalysis;
using OpenDreamShared.Compiler;

namespace DMCompiler.DM.Expressions {
    // x ? y : z
    sealed class Ternary : DMExpression {
        private readonly DMExpression _a, _b, _c;

        public override bool IsFuzzy => true;

        public Ternary(Location location, DMExpression a, DMExpression b, DMExpression c) : base(location) {
            _a = a;
            _b = b;
            _c = c;
        }

        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (!_a.TryAsConstant(out var a)) {
                constant = null;
                return false;
            }

            if (a.IsTruthy()) {
                return _b.TryAsConstant(out constant);
            }

            return _c.TryAsConstant(out constant);
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            string cLabel = proc.NewLabelName();
            string endLabel = proc.NewLabelName();

            _a.EmitPushValue(dmObject, proc);
            proc.JumpIfFalse(cLabel);
            _b.EmitPushValue(dmObject, proc);
            proc.Jump(endLabel);
            proc.AddLabel(cLabel);
            _c.EmitPushValue(dmObject, proc);
            proc.AddLabel(endLabel);
        }
    }

    // var in x to y
    sealed class InRange : DMExpression {
        private readonly DMExpression _var, _start, _end;

        public InRange(Location location, DMExpression var, DMExpression start, DMExpression end) : base(location) {
            _var = var;
            _start = start;
            _end = end;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            _var.EmitPushValue(dmObject, proc);
            _start.EmitPushValue(dmObject, proc);
            _end.EmitPushValue(dmObject, proc);
            proc.IsInRange();
        }
    }
}
