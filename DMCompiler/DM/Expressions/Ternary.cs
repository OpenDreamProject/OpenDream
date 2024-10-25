using System.Diagnostics.CodeAnalysis;

namespace DMCompiler.DM.Expressions;

// x ? y : z
internal sealed class Ternary(Location location, DMExpression a, DMExpression b, DMExpression c)
    : DMExpression(location) {
    public override bool PathIsFuzzy => true;
    public override DMComplexValueType ValType { get; } = (b.ValType.IsAnything || c.ValType.IsAnything) ? DMValueType.Anything : new DMComplexValueType(b.ValType.Type | c.ValType.Type, b.ValType.TypePath ?? c.ValType.TypePath);

    public override bool TryAsConstant(DMCompiler compiler, [NotNullWhen(true)] out Constant? constant) {
        if (!a.TryAsConstant(compiler, out var constant1)) {
            constant = null;
            return false;
        }

        if (constant1.IsTruthy()) {
            return b.TryAsConstant(compiler, out constant);
        }

        return c.TryAsConstant(compiler, out constant);
    }

    public override void EmitPushValue(ExpressionContext ctx) {
        string cLabel = ctx.Proc.NewLabelName();
        string endLabel = ctx.Proc.NewLabelName();

        a.EmitPushValue(ctx);
        ctx.Proc.JumpIfFalse(cLabel);
        b.EmitPushValue(ctx);
        ctx.Proc.Jump(endLabel);
        ctx.Proc.AddLabel(cLabel);
        c.EmitPushValue(ctx);
        ctx.Proc.AddLabel(endLabel);
    }
}

// var in x to y
internal sealed class InRange(Location location, DMExpression var, DMExpression start, DMExpression end) : DMExpression(location) {
    public override void EmitPushValue(ExpressionContext ctx) {
        var.EmitPushValue(ctx);
        start.EmitPushValue(ctx);
        end.EmitPushValue(ctx);
        ctx.Proc.IsInRange();
    }
}
