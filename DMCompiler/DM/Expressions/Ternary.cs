using System.Diagnostics.CodeAnalysis;
using DMCompiler.Compiler;

namespace DMCompiler.DM.Expressions;

// x ? y : z
internal sealed class Ternary : DMExpression {
    private readonly DMExpression _a, _b, _c;

    public override bool PathIsFuzzy => true;
    public override DMComplexValueType ValType { get; }

    public Ternary(Location location, DMExpression a, DMExpression b, DMExpression c) : base(location) {
        _a = a;
        _b = b;
        _c = c;

        if (b.ValType.TypePath != null && c.ValType.TypePath != null) {
            DMCompiler.Emit(WarningCode.LostTypeInfo, Location,
                $"Ternary has type paths {b.ValType.TypePath} and {c.ValType.TypePath} but a value can only have one type path. Using {b.ValType.TypePath}.");
        }

        ValType = new(b.ValType.Type | c.ValType.Type, b.ValType.TypePath ?? c.ValType.TypePath);
    }

    public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
        if (!_a.TryAsConstant(out var constant1)) {
            constant = null;
            return false;
        }

        if (constant1.IsTruthy()) {
            return _b.TryAsConstant(out constant);
        }

        return _c.TryAsConstant(out constant);
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

// var in x to y
internal sealed class InRange(Location location, DMExpression var, DMExpression start, DMExpression end) : DMExpression(location) {
    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        var.EmitPushValue(dmObject, proc);
        start.EmitPushValue(dmObject, proc);
        end.EmitPushValue(dmObject, proc);
        proc.IsInRange();
    }
}
