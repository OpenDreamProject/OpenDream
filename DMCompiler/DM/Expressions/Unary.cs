using System.Diagnostics.CodeAnalysis;
using DMCompiler.Bytecode;

namespace DMCompiler.DM.Expressions {
    abstract class UnaryOp : DMExpression {
        protected DMExpression Expr { get; }

        protected UnaryOp(Location location, DMExpression expr) : base(location) {
            Expr = expr;
        }
    }

    // -x
    sealed class Negate : UnaryOp {
        public Negate(Location location, DMExpression expr) : base(location, expr) {
        }

        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
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
    sealed class Not : UnaryOp {
        public Not(Location location, DMExpression expr) : base(location, expr) {
        }

        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
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
    sealed class BinaryNot : UnaryOp {
        public BinaryNot(Location location, DMExpression expr) : base(location, expr) {
        }

        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
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
        protected AssignmentUnaryOp(Location location, DMExpression expr) : base(location, expr) {
        }

        protected abstract void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel);

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            string endLabel = proc.NewLabelName();

            DMReference reference = Expr.EmitReference(dmObject, proc, endLabel);
            EmitOp(dmObject, proc, reference, endLabel);

            proc.AddLabel(endLabel);
        }
    }

    // ++x
    sealed class PreIncrement : AssignmentUnaryOp {
        public PreIncrement(Location location, DMExpression expr) : base(location, expr) {
        }

        protected override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
            proc.PushFloat(1);
            proc.Append(reference);
        }
    }

    // x++
    sealed class PostIncrement : AssignmentUnaryOp {
        public PostIncrement(Location location, DMExpression expr) : base(location, expr) {
        }

        protected override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
            proc.Increment(reference);
        }
    }

    // --x
    sealed class PreDecrement : AssignmentUnaryOp {
        public PreDecrement(Location location, DMExpression expr)
            : base(location, expr) {
        }

        protected override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
            proc.PushFloat(1);
            proc.Remove(reference);
        }
    }

    // x--
    sealed class PostDecrement : AssignmentUnaryOp {
        public PostDecrement(Location location, DMExpression expr)
            : base(location, expr) {
        }

        protected override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
            proc.Decrement(reference);
        }
    }

    // &x
    internal sealed class PointerRef(Location location, DMExpression expr) : AssignmentUnaryOp(location, expr) {
        protected override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) { }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            Expr.EmitPushValue(dmObject, proc);
            DMCompiler.UnimplementedWarning(location, "Pointers are currently unimplemented and identifiers will be treated as normal variables.");
        }

        public override DMReference EmitReference(DMObject dmObject, DMProc proc, string endLabel, ShortCircuitMode shortCircuitMode) {
            return Expr.EmitReference(dmObject, proc, endLabel, shortCircuitMode);
        }
    }

    // *x
    internal sealed class PointerDeref(Location location, DMExpression expr) : AssignmentUnaryOp(location, expr) {
        protected override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) { }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            Expr.EmitPushValue(dmObject, proc);
            DMCompiler.UnimplementedWarning(location, "Pointers are currently unimplemented and identifiers will be treated as normal variables.");
        }

        public override DMReference EmitReference(DMObject dmObject, DMProc proc, string endLabel, ShortCircuitMode shortCircuitMode) {
            return Expr.EmitReference(dmObject, proc, endLabel, shortCircuitMode);
        }
    }
}
