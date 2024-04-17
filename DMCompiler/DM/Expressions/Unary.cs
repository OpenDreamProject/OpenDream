using System.Diagnostics.CodeAnalysis;
using DMCompiler.Bytecode;

namespace DMCompiler.DM.Expressions {
    internal abstract class UnaryOp(Location location, DMExpression expr) : DMExpression(location) {
        protected DMExpression Expr { get; } = expr;
    }

    // -x
    internal sealed class Negate(Location location, DMExpression expr) : UnaryOp(location, expr) {
        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (!Expr.TryAsConstant(out constant) || constant is not Number number)
                return false;

            constant = new Number(Location, -number.Value);
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            Expr.EmitPushValue(dmObject, proc);
            proc.Negate();
        }
    }

    // !x
    internal sealed class Not(Location location, DMExpression expr) : UnaryOp(location, expr) {
        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (!Expr.TryAsConstant(out constant)) return false;

            constant = new Number(Location, constant.IsTruthy() ? 0 : 1);
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            Expr.EmitPushValue(dmObject, proc);
            proc.Not();
        }
    }

    // ~x
    internal sealed class BinaryNot(Location location, DMExpression expr) : UnaryOp(location, expr) {
        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (!Expr.TryAsConstant(out constant) || constant is not Number constantNum)
                return false;

            constant = new Number(Location, ~(int)constantNum.Value);
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            Expr.EmitPushValue(dmObject, proc);
            proc.BinaryNot();
        }
    }

    internal abstract class AssignmentUnaryOp(Location location, DMExpression expr) : UnaryOp(location, expr) {
        protected abstract void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel);

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            string endLabel = proc.NewLabelName();

            DMReference reference = Expr.EmitReference(dmObject, proc, endLabel);
            EmitOp(dmObject, proc, reference, endLabel);

            proc.AddLabel(endLabel);
        }
    }

    // ++x
    internal sealed class PreIncrement(Location location, DMExpression expr) : AssignmentUnaryOp(location, expr) {
        protected override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
            proc.PushFloat(1);
            proc.Append(reference);
        }
    }

    // x++
    internal sealed class PostIncrement(Location location, DMExpression expr) : AssignmentUnaryOp(location, expr) {
        protected override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
            proc.Increment(reference);
        }
    }

    // --x
    internal sealed class PreDecrement(Location location, DMExpression expr) : AssignmentUnaryOp(location, expr) {
        protected override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
            proc.PushFloat(1);
            proc.Remove(reference);
        }
    }

    // x--
    internal sealed class PostDecrement(Location location, DMExpression expr) : AssignmentUnaryOp(location, expr) {
        protected override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
            proc.Decrement(reference);
        }
    }
}
