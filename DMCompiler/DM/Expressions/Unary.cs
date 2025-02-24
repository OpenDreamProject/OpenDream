using System.Diagnostics.CodeAnalysis;
using OpenDreamShared.Common.Bytecode;

namespace DMCompiler.DM.Expressions;

internal abstract class UnaryOp(Location location, DMExpression expr) : DMExpression(location) {
    protected DMExpression Expr { get; } = expr;
}

// -x
internal sealed class Negate(Location location, DMExpression expr) : UnaryOp(location, expr) {
    public override bool TryAsConstant(DMCompiler compiler, [NotNullWhen(true)] out Constant? constant) {
        if (!Expr.TryAsConstant(compiler, out constant) || constant is not Number number)
            return false;

        constant = new Number(Location, -number.Value);
        return true;
    }

    public override void EmitPushValue(ExpressionContext ctx) {
        Expr.EmitPushValue(ctx);
        ctx.Proc.Negate();
    }
}

// !x
internal sealed class Not(Location location, DMExpression expr) : UnaryOp(location, expr) {
    public override bool TryAsConstant(DMCompiler compiler, [NotNullWhen(true)] out Constant? constant) {
        if (!Expr.TryAsConstant(compiler, out constant)) return false;

        constant = new Number(Location, constant.IsTruthy() ? 0 : 1);
        return true;
    }

    public override void EmitPushValue(ExpressionContext ctx) {
        Expr.EmitPushValue(ctx);
        ctx.Proc.Not();
    }
}

// ~x
internal sealed class BinaryNot(Location location, DMExpression expr) : UnaryOp(location, expr) {
    public override bool TryAsConstant(DMCompiler compiler, [NotNullWhen(true)] out Constant? constant) {
        if (!Expr.TryAsConstant(compiler, out constant) || constant is not Number constantNum)
            return false;

        constant = new Number(Location, ~(int)constantNum.Value);
        return true;
    }

    public override void EmitPushValue(ExpressionContext ctx) {
        Expr.EmitPushValue(ctx);
        ctx.Proc.BinaryNot();
    }
}

internal abstract class AssignmentUnaryOp(Location location, DMExpression expr) : UnaryOp(location, expr) {
    protected abstract void EmitOp(DMProc proc, DMReference reference);

    public override void EmitPushValue(ExpressionContext ctx) {
        string endLabel = ctx.Proc.NewLabelName();

        DMReference reference = Expr.EmitReference(ctx, endLabel);
        EmitOp(ctx.Proc, reference);

        ctx.Proc.AddLabel(endLabel);
    }
}

// ++x
internal sealed class PreIncrement(Location location, DMExpression expr) : AssignmentUnaryOp(location, expr) {
    protected override void EmitOp(DMProc proc, DMReference reference) {
        proc.PushFloat(1);
        proc.Append(reference);
    }
}

// x++
internal sealed class PostIncrement(Location location, DMExpression expr) : AssignmentUnaryOp(location, expr) {
    protected override void EmitOp(DMProc proc, DMReference reference) {
        proc.Increment(reference);
    }
}

// --x
internal sealed class PreDecrement(Location location, DMExpression expr) : AssignmentUnaryOp(location, expr) {
    protected override void EmitOp(DMProc proc, DMReference reference) {
        proc.PushFloat(1);
        proc.Remove(reference);
    }
}

// x--
internal sealed class PostDecrement(Location location, DMExpression expr) : AssignmentUnaryOp(location, expr) {
    protected override void EmitOp(DMProc proc, DMReference reference) {
        proc.Decrement(reference);
    }
}

// &x
internal sealed class PointerRef(Location location, DMExpression expr) : UnaryOp(location, expr) {
    public override void EmitPushValue(ExpressionContext ctx) {
        Expr.EmitPushValue(ctx);
        ctx.Compiler.UnimplementedWarning(Location, "Pointers are currently unimplemented and identifiers will be treated as normal variables.");
    }

    public override DMReference EmitReference(ExpressionContext ctx, string endLabel,
        ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
        return Expr.EmitReference(ctx, endLabel, shortCircuitMode);
    }
}

// *x
internal sealed class PointerDeref(Location location, DMExpression expr) : UnaryOp(location, expr) {
    public override void EmitPushValue(ExpressionContext ctx) {
        Expr.EmitPushValue(ctx);
        ctx.Compiler.UnimplementedWarning(Location, "Pointers are currently unimplemented and identifiers will be treated as normal variables.");
    }

    public override DMReference EmitReference(ExpressionContext ctx, string endLabel,
        ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
        return Expr.EmitReference(ctx, endLabel, shortCircuitMode);
    }
}
