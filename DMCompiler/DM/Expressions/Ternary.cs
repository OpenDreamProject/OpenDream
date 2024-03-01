using System.Diagnostics.CodeAnalysis;

namespace DMCompiler.DM.Expressions;

// x ? y : z
internal sealed class Ternary(Location location, DMExpression a, DMExpression b, DMExpression c) : DMExpression(location) {
    public override bool PathIsFuzzy => true;

    public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
        if (!a.TryAsConstant(out var constant1)) {
            constant = null;
            return false;
        }

        if (constant1.IsTruthy()) {
            return b.TryAsConstant(out constant);
        }

        return c.TryAsConstant(out constant);
    }

    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        string cLabel = proc.NewLabelName();
        string endLabel = proc.NewLabelName();

        a.EmitPushValue(dmObject, proc);
        proc.JumpIfFalse(cLabel);
        b.EmitPushValue(dmObject, proc);
        proc.Jump(endLabel);
        proc.AddLabel(cLabel);
        c.EmitPushValue(dmObject, proc);
        proc.AddLabel(endLabel);
    }
}

// var in x to y
internal sealed class InRange(Location location, DMExpression var, DMExpression start, DMExpression end) : DMExpression(location) {
    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        var.EmitPushValue(dmObject, proc);
        start.EmitPushValue(dmObject, proc);
        end.EmitPushValue(dmObject, proc);
        proc.IsInRange();
    }
}
