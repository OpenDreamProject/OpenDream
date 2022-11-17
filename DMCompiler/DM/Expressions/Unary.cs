using OpenDreamShared.Compiler;
using OpenDreamShared.Dream.Procs;

namespace DMCompiler.DM.Expressions {
    abstract class UnaryOp : DMExpression {
        protected DMExpression Expr { get; }

        public UnaryOp(Location location, DMExpression expr) : base(location) {
            Expr = expr;
        }
    }

    // -x
    class Negate : UnaryOp {
        public Negate(Location location, DMExpression expr)
            : base(location, expr)
        {}

        public override bool TryAsConstant(out Constant constant) {
            if (!Expr.TryAsConstant(out constant)) return false;

            constant = constant.Negate();
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            Expr.EmitPushValue(dmObject, proc);
            proc.Negate();
        }
    }

    // !x
    class Not : UnaryOp {
        public Not(Location location, DMExpression expr)
            : base(location, expr)
        {}

        public override bool TryAsConstant(out Constant constant) {
            if (!Expr.TryAsConstant(out constant)) return false;

            constant = constant.Not();
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            Expr.EmitPushValue(dmObject, proc);
            proc.Not();
        }
    }

    // ~x
    class BinaryNot : UnaryOp {
        public BinaryNot(Location location, DMExpression expr)
            : base(location, expr)
        {}

        public override bool TryAsConstant(out Constant constant) {
            if (!Expr.TryAsConstant(out constant)) return false;

            constant = constant.BinaryNot();
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            Expr.EmitPushValue(dmObject, proc);
            proc.BinaryNot();
        }
    }

    abstract class AssignmentUnaryOp : UnaryOp {
        public AssignmentUnaryOp(Location location, DMExpression expr)
            : base(location, expr) { }

        public abstract void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel);

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            string endLabel = proc.NewLabelName();

            DMReference reference = Expr.EmitReference(dmObject, proc, endLabel);
            EmitOp(dmObject, proc, reference, endLabel);

            proc.AddLabel(endLabel);
        }
    }

    // ++x
    class PreIncrement : AssignmentUnaryOp {
        public PreIncrement(Location location, DMExpression expr)
            : base(location, expr)
        {}

        public override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
            proc.PushFloat(1);
            proc.Append(reference);
        }
    }

    // x++
    class PostIncrement : AssignmentUnaryOp {
        public PostIncrement(Location location, DMExpression expr)
            : base(location, expr)
        {}

        public override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
            proc.Increment(reference);
        }
    }

    // --x
    class PreDecrement : AssignmentUnaryOp {
        public PreDecrement(Location location, DMExpression expr)
            : base(location, expr)
        {}

        public override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
            proc.PushFloat(1);
            proc.Remove(reference);
        }
    }

    // x--
    class PostDecrement : AssignmentUnaryOp {
        public PostDecrement(Location location, DMExpression expr)
            : base(location, expr)
        {}

        public override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
            proc.Decrement(reference);
        }
    }
}
