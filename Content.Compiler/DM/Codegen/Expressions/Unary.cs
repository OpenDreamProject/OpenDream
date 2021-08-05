namespace Content.Compiler.DM.Expressions
{
    abstract class UnaryOp : DMExpression {
        protected DMExpression Expr { get; }

        public UnaryOp(DMExpression expr) {
            Expr = expr;
        }
    }

    // -x
    class Negate : UnaryOp {
        public Negate(DMExpression expr)
            : base(expr)
        {}

        public override Constant ToConstant()
        {
            return Expr.ToConstant().Negate();
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            Expr.EmitPushValue(dmObject, proc);
            proc.Negate();
        }
    }

    // !x
    class Not : UnaryOp {
        public Not(DMExpression expr)
            : base(expr)
        {}

        public override Constant ToConstant()
        {
            return Expr.ToConstant().Not();
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            Expr.EmitPushValue(dmObject, proc);
            proc.Not();
        }
    }

    // ~x
    class BinaryNot : UnaryOp {
        public BinaryNot(DMExpression expr)
            : base(expr)
        {}

        public override Constant ToConstant()
        {
            return Expr.ToConstant().BinaryNot();
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            Expr.EmitPushValue(dmObject, proc);
            proc.BinaryNot();
        }
    }

    // ++x
    class PreIncrement : UnaryOp {
        public PreIncrement(DMExpression expr)
            : base(expr)
        {}

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            Expr.EmitPushValue(dmObject, proc);
            proc.PushFloat(1);
            proc.Append();
        }
    }

    // x++
    class PostIncrement : UnaryOp {
        public PostIncrement(DMExpression expr)
            : base(expr)
        {}

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            // TODO: THIS IS WRONG! We have to just keep the value on the stack instead of running our LHS twice
            Expr.EmitPushValue(dmObject, proc);
            Expr.EmitPushValue(dmObject, proc);
            proc.PushFloat(1);
            proc.Append();
            proc.Pop();
        }
    }

    // --x
    class PreDecrement : UnaryOp {
        public PreDecrement(DMExpression expr)
            : base(expr)
        {}

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            Expr.EmitPushValue(dmObject, proc);
            proc.PushFloat(1);
            proc.Remove();
        }
    }

    // x--
    class PostDecrement : UnaryOp {
        public PostDecrement(DMExpression expr)
            : base(expr)
        {}

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            // TODO: THIS IS WRONG! We have to just keep the value on the stack instead of running our LHS twice
            Expr.EmitPushValue(dmObject, proc);
            Expr.EmitPushValue(dmObject, proc);
            proc.PushFloat(1);
            proc.Remove();
            proc.Pop();
        }
    }
}
