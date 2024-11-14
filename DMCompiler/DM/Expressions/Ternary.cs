using System.Diagnostics.CodeAnalysis;
using DMCompiler.Compiler;

namespace DMCompiler.DM.Expressions;

// x ? y : z
internal sealed class Ternary(Location location, DMExpression a, DMExpression b, DMExpression c)
    : DMExpression(location) {
    public override bool PathIsFuzzy => true;
    public override DMComplexValueType ValType { get; } = new(b.ValType.Type | c.ValType.Type, b.ValType.TypePath ?? c.ValType.TypePath);

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

    public override void EmitPushValue(DMCompiler compiler, DMObject dmObject, DMProc proc) {
        string cLabel = proc.NewLabelName();
        string endLabel = proc.NewLabelName();

        a.EmitPushValue(compiler, dmObject, proc);
        proc.JumpIfFalse(cLabel);
        b.EmitPushValue(compiler, dmObject, proc);
        proc.Jump(endLabel);
        proc.AddLabel(cLabel);
        c.EmitPushValue(compiler, dmObject, proc);
        proc.AddLabel(endLabel);
    }
}

// var in x to y
internal sealed class InRange(Location location, DMExpression var, DMExpression start, DMExpression end) : DMExpression(location) {
    public override void EmitPushValue(DMCompiler compiler, DMObject dmObject, DMProc proc) {
        var.EmitPushValue(compiler, dmObject, proc);
        start.EmitPushValue(compiler, dmObject, proc);
        end.EmitPushValue(compiler, dmObject, proc);
        proc.IsInRange();
    }
}
