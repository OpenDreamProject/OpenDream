using System.Diagnostics.CodeAnalysis;
using DMCompiler.Bytecode;

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

    public override void EmitPushValue(DMCompiler compiler, DMObject dmObject, DMProc proc) {
        Expr.EmitPushValue(compiler, dmObject, proc);
        proc.Negate();
    }
}

// !x
internal sealed class Not(Location location, DMExpression expr) : UnaryOp(location, expr) {
    public override bool TryAsConstant(DMCompiler compiler, [NotNullWhen(true)] out Constant? constant) {
        if (!Expr.TryAsConstant(compiler, out constant)) return false;

        constant = new Number(Location, constant.IsTruthy() ? 0 : 1);
        return true;
    }

    public override void EmitPushValue(DMCompiler compiler, DMObject dmObject, DMProc proc) {
        Expr.EmitPushValue(compiler, dmObject, proc);
        proc.Not();
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

    public override void EmitPushValue(DMCompiler compiler, DMObject dmObject, DMProc proc) {
        Expr.EmitPushValue(compiler, dmObject, proc);
        proc.BinaryNot();
    }
}

internal abstract class AssignmentUnaryOp(Location location, DMExpression expr) : UnaryOp(location, expr) {
    protected abstract void EmitOp(DMProc proc, DMReference reference);

    public override void EmitPushValue(DMCompiler compiler, DMObject dmObject, DMProc proc) {
        string endLabel = proc.NewLabelName();

        DMReference reference = Expr.EmitReference(compiler, dmObject, proc, endLabel);
        EmitOp(proc, reference);

        proc.AddLabel(endLabel);
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
    public override void EmitPushValue(DMCompiler compiler, DMObject dmObject, DMProc proc) {
        Expr.EmitPushValue(compiler, dmObject, proc);
        compiler.UnimplementedWarning(Location, "Pointers are currently unimplemented and identifiers will be treated as normal variables.");
    }

    public override DMReference EmitReference(DMCompiler compiler, DMObject dmObject, DMProc proc, string endLabel, ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
        return Expr.EmitReference(compiler, dmObject, proc, endLabel, shortCircuitMode);
    }
}

// *x
internal sealed class PointerDeref(Location location, DMExpression expr) : UnaryOp(location, expr) {
    public override void EmitPushValue(DMCompiler compiler, DMObject dmObject, DMProc proc) {
        Expr.EmitPushValue(compiler, dmObject, proc);
        compiler.UnimplementedWarning(Location, "Pointers are currently unimplemented and identifiers will be treated as normal variables.");
    }

    public override DMReference EmitReference(DMCompiler compiler, DMObject dmObject, DMProc proc, string endLabel, ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
        return Expr.EmitReference(compiler, dmObject, proc, endLabel, shortCircuitMode);
    }
}
