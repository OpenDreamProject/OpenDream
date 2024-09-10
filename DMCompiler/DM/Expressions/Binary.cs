using System.Diagnostics.CodeAnalysis;
using DMCompiler.Bytecode;
using DMCompiler.Compiler;

namespace DMCompiler.DM.Expressions;

internal abstract class BinaryOp(Location location, DMExpression lhs, DMExpression rhs) : DMExpression(location) {
    protected DMExpression LHS { get; } = lhs;
    protected DMExpression RHS { get; } = rhs;

    public override DMComplexValueType ValType => LHS.ValType;
    public override bool PathIsFuzzy => true;
}

#region Simple
// x + y
internal sealed class Add(Location location, DMExpression lhs, DMExpression rhs) : BinaryOp(location, lhs, rhs) {
    public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
        if (!LHS.TryAsConstant(out var lhs) || !RHS.TryAsConstant(out var rhs)) {
            constant = null;
            return false;
        }

        if (lhs is Number lhsNum && rhs is Number rhsNum) {
            constant = new Number(Location, lhsNum.Value + rhsNum.Value);
        } else if (lhs is String lhsString && rhs is String rhsString) {
            constant = new String(Location, lhsString.Value + rhsString.Value);
        } else {
            constant = null;
            return false;
        }

        return true;
    }

    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        LHS.EmitPushValue(dmObject, proc);
        RHS.EmitPushValue(dmObject, proc);
        proc.Add();
    }
}

// x - y
internal sealed class Subtract(Location location, DMExpression lhs, DMExpression rhs) : BinaryOp(location, lhs, rhs) {
    public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
        if (!LHS.TryAsConstant(out var lhs) || !RHS.TryAsConstant(out var rhs)) {
            constant = null;
            return false;
        }

        if (lhs is Number lhsNum && rhs is Number rhsNum) {
            constant = new Number(Location, lhsNum.Value - rhsNum.Value);
        } else {
            constant = null;
            return false;
        }

        return true;
    }

    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        LHS.EmitPushValue(dmObject, proc);
        RHS.EmitPushValue(dmObject, proc);
        proc.Subtract();
    }
}

// x * y
internal sealed class Multiply(Location location, DMExpression lhs, DMExpression rhs) : BinaryOp(location, lhs, rhs) {
    public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
        if (!LHS.TryAsConstant(out var lhs) || !RHS.TryAsConstant(out var rhs)) {
            constant = null;
            return false;
        }

        if (lhs is Number lhsNum && rhs is Number rhsNum) {
            constant = new Number(Location, lhsNum.Value * rhsNum.Value);
        } else {
            constant = null;
            return false;
        }

        return true;
    }

    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        LHS.EmitPushValue(dmObject, proc);
        RHS.EmitPushValue(dmObject, proc);
        proc.Multiply();
    }
}

// x / y
internal sealed class Divide(Location location, DMExpression lhs, DMExpression rhs) : BinaryOp(location, lhs, rhs) {
    public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
        if (!LHS.TryAsConstant(out var lhs) || !RHS.TryAsConstant(out var rhs)) {
            constant = null;
            return false;
        }

        if (lhs is Number lhsNum && rhs is Number rhsNum) {
            constant = new Number(Location, lhsNum.Value / rhsNum.Value);
        } else {
            constant = null;
            return false;
        }

        return true;
    }

    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        LHS.EmitPushValue(dmObject, proc);
        RHS.EmitPushValue(dmObject, proc);
        proc.Divide();
    }
}

// x % y
internal sealed class Modulo(Location location, DMExpression lhs, DMExpression rhs) : BinaryOp(location, lhs, rhs) {
    public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
        if (!LHS.TryAsConstant(out var lhs) || !RHS.TryAsConstant(out var rhs)) {
            constant = null;
            return false;
        }

        if (lhs is Number lhsNum && rhs is Number rhsNum) {
            constant = new Number(Location, lhsNum.Value % rhsNum.Value);
        } else {
            constant = null;
            return false;
        }

        return true;
    }

    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        LHS.EmitPushValue(dmObject, proc);
        RHS.EmitPushValue(dmObject, proc);
        proc.Modulus();
    }
}

// x %% y
internal sealed class ModuloModulo(Location location, DMExpression lhs, DMExpression rhs) : BinaryOp(location, lhs, rhs) {
    public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
        if (!LHS.TryAsConstant(out var lhs) || !RHS.TryAsConstant(out var rhs)) {
            constant = null;
            return false;
        }

        if (lhs is Number lhsNum && rhs is Number rhsNum) {
            // BYOND docs say that A %% B is equivalent to B * fract(A/B)
            var fraction = lhsNum.Value / rhsNum.Value;
            fraction -= MathF.Truncate(fraction);
            constant = new Number(Location, fraction * rhsNum.Value);
        } else {
            constant = null;
            return false;
        }

        return true;
    }

    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        LHS.EmitPushValue(dmObject, proc);
        RHS.EmitPushValue(dmObject, proc);
        proc.ModulusModulus();
    }
}

// x ** y
internal sealed class Power(Location location, DMExpression lhs, DMExpression rhs) : BinaryOp(location, lhs, rhs) {
    public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
        if (!LHS.TryAsConstant(out var lhs) || !RHS.TryAsConstant(out var rhs)) {
            constant = null;
            return false;
        }

        if (lhs is Number lhsNum && rhs is Number rhsNum) {
            constant = new Number(Location, MathF.Pow(lhsNum.Value, rhsNum.Value));
        } else {
            constant = null;
            return false;
        }

        return true;
    }

    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        LHS.EmitPushValue(dmObject, proc);
        RHS.EmitPushValue(dmObject, proc);
        proc.Power();
    }
}

// x << y
internal sealed class LeftShift(Location location, DMExpression lhs, DMExpression rhs) : BinaryOp(location, lhs, rhs) {
    public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
        if (!LHS.TryAsConstant(out var lhs) || !RHS.TryAsConstant(out var rhs)) {
            constant = null;
            return false;
        }

        if (lhs is Number lhsNum && rhs is Number rhsNum) {
            constant = new Number(Location, ((int)lhsNum.Value) << ((int)rhsNum.Value));
        } else {
            constant = null;
            return false;
        }

        return true;
    }

    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        LHS.EmitPushValue(dmObject, proc);
        RHS.EmitPushValue(dmObject, proc);
        proc.BitShiftLeft();
    }
}

// x >> y
internal sealed class RightShift(Location location, DMExpression lhs, DMExpression rhs) : BinaryOp(location, lhs, rhs) {
    public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
        if (!LHS.TryAsConstant(out var lhs) || !RHS.TryAsConstant(out var rhs)) {
            constant = null;
            return false;
        }

        if (lhs is Number lhsNum && rhs is Number rhsNum) {
            constant = new Number(Location, ((int)lhsNum.Value) >> ((int)rhsNum.Value));
        } else {
            constant = null;
            return false;
        }

        return true;
    }

    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        LHS.EmitPushValue(dmObject, proc);
        RHS.EmitPushValue(dmObject, proc);
        proc.BitShiftRight();
    }
}

// x & y
internal sealed class BinaryAnd(Location location, DMExpression lhs, DMExpression rhs) : BinaryOp(location, lhs, rhs) {
    public override bool PathIsFuzzy => true;

    public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
        if (!LHS.TryAsConstant(out var lhs) || !RHS.TryAsConstant(out var rhs)) {
            constant = null;
            return false;
        }

        if (lhs is Number lhsNum && rhs is Number rhsNum) {
            constant = new Number(Location, ((int)lhsNum.Value) & ((int)rhsNum.Value));
        } else {
            constant = null;
            return false;
        }

        return true;
    }

    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        LHS.EmitPushValue(dmObject, proc);
        RHS.EmitPushValue(dmObject, proc);
        proc.BinaryAnd();
    }
}

// x ^ y
internal sealed class BinaryXor(Location location, DMExpression lhs, DMExpression rhs) : BinaryOp(location, lhs, rhs) {
    public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
        if (!LHS.TryAsConstant(out var lhs) || !RHS.TryAsConstant(out var rhs)) {
            constant = null;
            return false;
        }

        if (lhs is Number lhsNum && rhs is Number rhsNum) {
            constant = new Number(Location, ((int)lhsNum.Value) ^ ((int)rhsNum.Value));
        } else {
            constant = null;
            return false;
        }

        return true;
    }

    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        LHS.EmitPushValue(dmObject, proc);
        RHS.EmitPushValue(dmObject, proc);
        proc.BinaryXor();
    }
}

// x | y
internal sealed class BinaryOr(Location location, DMExpression lhs, DMExpression rhs) : BinaryOp(location, lhs, rhs) {
    public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
        if (!LHS.TryAsConstant(out var lhs) || !RHS.TryAsConstant(out var rhs)) {
            constant = null;
            return false;
        }

        if (lhs is Number lhsNum && rhs is Number rhsNum) {
            constant = new Number(Location, ((int)lhsNum.Value) | ((int)rhsNum.Value));
        } else {
            constant = null;
            return false;
        }

        return true;
    }

    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        LHS.EmitPushValue(dmObject, proc);
        RHS.EmitPushValue(dmObject, proc);
        proc.BinaryOr();
    }
}

// x == y
internal sealed class Equal(Location location, DMExpression lhs, DMExpression rhs) : BinaryOp(location, lhs, rhs) {
    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        LHS.EmitPushValue(dmObject, proc);
        RHS.EmitPushValue(dmObject, proc);
        proc.Equal();
    }
}

// x != y
internal sealed class NotEqual(Location location, DMExpression lhs, DMExpression rhs) : BinaryOp(location, lhs, rhs) {
    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        LHS.EmitPushValue(dmObject, proc);
        RHS.EmitPushValue(dmObject, proc);
        proc.NotEqual();
    }
}

// x ~= y
internal sealed class Equivalent(Location location, DMExpression lhs, DMExpression rhs) : BinaryOp(location, lhs, rhs) {
    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        LHS.EmitPushValue(dmObject, proc);
        RHS.EmitPushValue(dmObject, proc);
        proc.Equivalent();
    }
}

// x ~! y
internal sealed class NotEquivalent(Location location, DMExpression lhs, DMExpression rhs) : BinaryOp(location, lhs, rhs) {
    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        LHS.EmitPushValue(dmObject, proc);
        RHS.EmitPushValue(dmObject, proc);
        proc.NotEquivalent();
    }
}

// x > y
internal sealed class GreaterThan(Location location, DMExpression lhs, DMExpression rhs) : BinaryOp(location, lhs, rhs) {
    public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
        if (!LHS.TryAsConstant(out var lhs) || !RHS.TryAsConstant(out var rhs)) {
            constant = null;
            return false;
        }

        if (lhs is Null && rhs is Number rhsNum1) {
            constant = new Number(Location, (0 > rhsNum1.Value) ? 1 : 0);
        } else if (lhs is Number lhsNum && rhs is Number rhsNum2) {
            constant = new Number(Location, (lhsNum.Value > rhsNum2.Value) ? 1 : 0);
        } else {
            constant = null;
            return false;
        }

        return true;
    }

    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        LHS.EmitPushValue(dmObject, proc);
        RHS.EmitPushValue(dmObject, proc);
        proc.GreaterThan();
    }
}

// x >= y
internal sealed class GreaterThanOrEqual(Location location, DMExpression lhs, DMExpression rhs) : BinaryOp(location, lhs, rhs) {
    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        LHS.EmitPushValue(dmObject, proc);
        RHS.EmitPushValue(dmObject, proc);
        proc.GreaterThanOrEqual();
    }

    public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
        if (!LHS.TryAsConstant(out var lhs) || !RHS.TryAsConstant(out var rhs)) {
            constant = null;
            return false;
        }

        if (lhs is Null && rhs is Number rhsNum1) {
            constant = new Number(Location, (0 >= rhsNum1.Value) ? 1 : 0);
        } else if (lhs is Number lhsNum && rhs is Number rhsNum2) {
            constant = new Number(Location, (lhsNum.Value >= rhsNum2.Value) ? 1 : 0);
        } else {
            constant = null;
            return false;
        }

        return true;
    }
}


// x < y
internal sealed class LessThan(Location location, DMExpression lhs, DMExpression rhs) : BinaryOp(location, lhs, rhs) {
    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        LHS.EmitPushValue(dmObject, proc);
        RHS.EmitPushValue(dmObject, proc);
        proc.LessThan();
    }

    public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
        if (!LHS.TryAsConstant(out var lhs) || !RHS.TryAsConstant(out var rhs)) {
            constant = null;
            return false;
        }

        if (lhs is Null && rhs is Number rhsNum1) {
            constant = new Number(Location, (0 < rhsNum1.Value) ? 1 : 0);
        } else if (lhs is Number lhsNum && rhs is Number rhsNum2) {
            constant = new Number(Location, (lhsNum.Value < rhsNum2.Value) ? 1 : 0);
        } else {
            constant = null;
            return false;
        }

        return true;
    }
}

// x <= y
internal sealed class LessThanOrEqual(Location location, DMExpression lhs, DMExpression rhs) : BinaryOp(location, lhs, rhs) {
    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        LHS.EmitPushValue(dmObject, proc);
        RHS.EmitPushValue(dmObject, proc);
        proc.LessThanOrEqual();
    }

    public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
        if (!LHS.TryAsConstant(out var lhs) || !RHS.TryAsConstant(out var rhs)) {
            constant = null;
            return false;
        }

        if (lhs is Null && rhs is Number rhsNum1) {
            constant = new Number(Location, (0 <= rhsNum1.Value) ? 1 : 0);
        } else if (lhs is Number lhsNum && rhs is Number rhsNum2) {
            constant = new Number(Location, (lhsNum.Value <= rhsNum2.Value) ? 1 : 0);
        } else {
            constant = null;
            return false;
        }

        return true;
    }
}

// x || y
internal sealed class Or(Location location, DMExpression lhs, DMExpression rhs) : BinaryOp(location, lhs, rhs) {
    public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
        if (LHS.TryAsConstant(out var lhs)) {
            if (lhs.IsTruthy()) {
                constant = lhs;
                return true;
            }

            if (RHS.TryAsConstant(out var rhs)) {
                constant = rhs;
                return true;
            }
        }

        constant = null;
        return false;
    }

    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        string endLabel = proc.NewLabelName();

        LHS.EmitPushValue(dmObject, proc);
        proc.BooleanOr(endLabel);
        RHS.EmitPushValue(dmObject, proc);
        proc.AddLabel(endLabel);
    }
}

// x && y
internal sealed class And(Location location, DMExpression lhs, DMExpression rhs) : BinaryOp(location, lhs, rhs) {
    public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
        if (LHS.TryAsConstant(out var lhs) && !lhs.IsTruthy()) {
            constant = lhs;
            return true;
        }

        if (RHS.TryAsConstant(out var rhs)) {
            constant = rhs;
            return true;
        }

        constant = null;
        return false;
    }

    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        string endLabel = proc.NewLabelName();

        LHS.EmitPushValue(dmObject, proc);
        proc.BooleanAnd(endLabel);
        RHS.EmitPushValue(dmObject, proc);
        proc.AddLabel(endLabel);
    }
}

// x in y
internal sealed class In(Location location, DMExpression expr, DMExpression container) : BinaryOp(location, expr, container) {
    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        LHS.EmitPushValue(dmObject, proc);
        RHS.EmitPushValue(dmObject, proc);
        proc.IsInList();
    }
}
#endregion

#region Compound Assignment
internal abstract class AssignmentBinaryOp(Location location, DMExpression lhs, DMExpression rhs)
    : BinaryOp(location, lhs, rhs) {
    /// <summary>
    /// Generic interface for emitting the assignment operation. Has its conditionality and reference generation already handled.
    /// </summary>
    /// <remarks>You should always make use of the reference argument, unless you totally override AssignmentBinaryOp's EmitPushValue method.</remarks>
    /// <param name="reference">A reference to the LHS emitted via <see cref="DMExpression.EmitReference(DMObject,DMProc,string,DMExpression.ShortCircuitMode)"/></param>
    protected abstract void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel);

    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        string endLabel = proc.NewLabelName();

        DMReference reference = LHS.EmitReference(dmObject, proc, endLabel);
        EmitOp(dmObject, proc, reference, endLabel);

        proc.AddLabel(endLabel);
    }
}

// x = y
internal sealed class Assignment(Location location, DMExpression lhs, DMExpression rhs) : AssignmentBinaryOp(location, lhs, rhs) {
    public override DreamPath? Path => LHS.Path;

    protected override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
        RHS.EmitPushValue(dmObject, proc);
        proc.Assign(reference);

        if (!LHS.ValType.MatchesType(RHS.ValType) && !LHS.ValType.IsUnimplemented) {
            if (DMCompiler.Settings.SkipAnythingTypecheck && RHS.ValType.IsAnything)
                return;

            DMCompiler.Emit(WarningCode.InvalidVarType, Location,
                $"Invalid var type {RHS.ValType}, expected {LHS.ValType}");
        }
    }
}

// x := y
internal sealed class AssignmentInto(Location location, DMExpression lhs, DMExpression rhs) : AssignmentBinaryOp(location, lhs, rhs) {
    public override DreamPath? Path => LHS.Path;

    protected override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
        RHS.EmitPushValue(dmObject, proc);
        proc.AssignInto(reference);
    }
}

// x += y
internal sealed class Append(Location location, DMExpression lhs, DMExpression rhs) : AssignmentBinaryOp(location, lhs, rhs) {
    protected override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
        RHS.EmitPushValue(dmObject, proc);
        proc.Append(reference);
    }
}

// x |= y
internal sealed class Combine(Location location, DMExpression lhs, DMExpression rhs) : AssignmentBinaryOp(location, lhs, rhs) {
    protected override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
        RHS.EmitPushValue(dmObject, proc);
        proc.Combine(reference);
    }
}

// x -= y
internal sealed class Remove(Location location, DMExpression lhs, DMExpression rhs) : AssignmentBinaryOp(location, lhs, rhs) {
    protected override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
        RHS.EmitPushValue(dmObject, proc);
        proc.Remove(reference);
    }
}

// x &= y
internal sealed class Mask(Location location, DMExpression lhs, DMExpression rhs) : AssignmentBinaryOp(location, lhs, rhs) {
    protected override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
        RHS.EmitPushValue(dmObject, proc);
        proc.Mask(reference);
    }
}

// x &&= y
internal sealed class LogicalAndAssign(Location location, DMExpression lhs, DMExpression rhs) : AssignmentBinaryOp(location, lhs, rhs) {
    protected override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
        proc.JumpIfFalseReference(reference, endLabel);
        RHS.EmitPushValue(dmObject, proc);
        proc.Assign(reference);
    }
}

// x ||= y
internal sealed class LogicalOrAssign(Location location, DMExpression lhs, DMExpression rhs) : AssignmentBinaryOp(location, lhs, rhs) {
    protected override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
        proc.JumpIfTrueReference(reference, endLabel);
        RHS.EmitPushValue(dmObject, proc);
        proc.Assign(reference);
    }
}

// x *= y
internal sealed class MultiplyAssign(Location location, DMExpression lhs, DMExpression rhs) : AssignmentBinaryOp(location, lhs, rhs) {
    protected override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
        RHS.EmitPushValue(dmObject, proc);
        proc.MultiplyReference(reference);
    }
}

// x /= y
internal sealed class DivideAssign(Location location, DMExpression lhs, DMExpression rhs) : AssignmentBinaryOp(location, lhs, rhs) {
    protected override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
        RHS.EmitPushValue(dmObject, proc);
        proc.DivideReference(reference);
    }
}

// x <<= y
internal sealed class LeftShiftAssign(Location location, DMExpression lhs, DMExpression rhs) : AssignmentBinaryOp(location, lhs, rhs) {
    protected override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
        RHS.EmitPushValue(dmObject, proc);
        proc.BitShiftLeftReference(reference);
    }
}

// x >>= y
internal sealed class RightShiftAssign(Location location, DMExpression lhs, DMExpression rhs) : AssignmentBinaryOp(location, lhs, rhs) {
    protected override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
        RHS.EmitPushValue(dmObject, proc);
        proc.BitShiftRightReference(reference);
    }
}

// x ^= y
internal sealed class XorAssign(Location location, DMExpression lhs, DMExpression rhs) : AssignmentBinaryOp(location, lhs, rhs) {
    protected override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
        RHS.EmitPushValue(dmObject, proc);
        proc.BinaryXorReference(reference);
    }
}

// x %= y
internal sealed class ModulusAssign(Location location, DMExpression lhs, DMExpression rhs) : AssignmentBinaryOp(location, lhs, rhs) {
    protected override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
        RHS.EmitPushValue(dmObject, proc);
        proc.ModulusReference(reference);
    }
}

// x %%= y
internal sealed class ModulusModulusAssign(Location location, DMExpression lhs, DMExpression rhs) : AssignmentBinaryOp(location, lhs, rhs) {
    protected override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
        RHS.EmitPushValue(dmObject, proc);
        proc.ModulusModulusReference(reference);
    }
}
#endregion
