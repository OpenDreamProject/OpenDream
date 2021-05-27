namespace DMCompiler.DM.Expressions {
    // x ? y : z
    class Ternary : DMExpression {
        DMExpression _a, _b, _c;

        public Ternary(DMExpression a, DMExpression b, DMExpression c) {
            _a = a;
            _b = b;
            _c = c;
        }

        public override Constant ToConstant()
        {
            var a = _a.ToConstant();

            if (a.IsTruthy()) {
                return _b.ToConstant();
            }

            return _c.ToConstant();
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
}
