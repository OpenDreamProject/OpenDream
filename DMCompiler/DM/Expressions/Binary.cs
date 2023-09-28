using System.Diagnostics.CodeAnalysis;
using DMCompiler.Bytecode;
using OpenDreamShared.Compiler;
using OpenDreamShared.Dream;

namespace DMCompiler.DM.Expressions {
    abstract class BinaryOp : DMExpression {
        protected DMExpression LHS { get; }
        protected DMExpression RHS { get; }

        protected BinaryOp(Location location, DMExpression lhs, DMExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }
    }

    #region Simple
    // x + y
    sealed class Add : BinaryOp {
        public Add(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (!LHS.TryAsConstant(out var lhs) || !RHS.TryAsConstant(out var rhs)) {
                constant = null;
                return false;
            }

            constant = lhs.Add(rhs);
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.Add();
        }
    }

    // x - y
    sealed class Subtract : BinaryOp {
        public Subtract(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (!LHS.TryAsConstant(out var lhs) || !RHS.TryAsConstant(out var rhs)) {
                constant = null;
                return false;
            }

            constant = lhs.Subtract(rhs);
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.Subtract();
        }
    }

    // x * y
    sealed class Multiply : BinaryOp {
        public Multiply(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (!LHS.TryAsConstant(out var lhs) || !RHS.TryAsConstant(out var rhs)) {
                constant = null;
                return false;
            }

            constant = lhs.Multiply(rhs);
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.Multiply();
        }
    }

    // x / y
    sealed class Divide : BinaryOp {
        public Divide(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (!LHS.TryAsConstant(out var lhs) || !RHS.TryAsConstant(out var rhs)) {
                constant = null;
                return false;
            }

            constant = lhs.Divide(rhs);
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.Divide();
        }
    }

    // x % y
    sealed class Modulo : BinaryOp {
        public Modulo(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (!LHS.TryAsConstant(out var lhs) || !RHS.TryAsConstant(out var rhs)) {
                constant = null;
                return false;
            }

            constant = lhs.Modulo(rhs);
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.Modulus();
        }
    }

    // x %% y
    sealed class ModuloModulo : BinaryOp {
        public ModuloModulo(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (!LHS.TryAsConstant(out var lhs) || !RHS.TryAsConstant(out var rhs)) {
                constant = null;
                return false;
            }

            constant = lhs.ModuloModulo(rhs);
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.ModulusModulus();
        }
    }

    // x ** y
    sealed class Power : BinaryOp {
        public Power(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (!LHS.TryAsConstant(out var lhs) || !RHS.TryAsConstant(out var rhs)) {
                constant = null;
                return false;
            }

            constant = lhs.Power(rhs);
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.Power();
        }
    }

    // x << y
    sealed class LeftShift : BinaryOp {
        public LeftShift(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (!LHS.TryAsConstant(out var lhs) || !RHS.TryAsConstant(out var rhs)) {
                constant = null;
                return false;
            }

            constant = lhs.LeftShift(rhs);
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.BitShiftLeft();
        }
    }

    // x >> y
    sealed class RightShift : BinaryOp {
        public RightShift(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (!LHS.TryAsConstant(out var lhs) || !RHS.TryAsConstant(out var rhs)) {
                constant = null;
                return false;
            }

            constant = lhs.RightShift(rhs);
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.BitShiftRight();
        }
    }

    // x & y
    sealed class BinaryAnd : BinaryOp {
        public BinaryAnd(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (!LHS.TryAsConstant(out var lhs) || !RHS.TryAsConstant(out var rhs)) {
                constant = null;
                return false;
            }

            constant = lhs.BinaryAnd(rhs);
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.BinaryAnd();
        }
    }

    // x ^ y
    sealed class BinaryXor : BinaryOp {
        public BinaryXor(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (!LHS.TryAsConstant(out var lhs) || !RHS.TryAsConstant(out var rhs)) {
                constant = null;
                return false;
            }

            constant = lhs.BinaryXor(rhs);
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.BinaryXor();
        }
    }

    // x | y
    sealed class BinaryOr : BinaryOp {
        public BinaryOr(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (!LHS.TryAsConstant(out var lhs) || !RHS.TryAsConstant(out var rhs)) {
                constant = null;
                return false;
            }

            constant = lhs.BinaryOr(rhs);
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.BinaryOr();
        }
    }

    // x == y
    sealed class Equal : BinaryOp {
        public Equal(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.Equal();
        }
    }

    // x != y
    sealed class NotEqual : BinaryOp {
        public NotEqual(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.NotEqual();
        }
    }

    // x ~= y
    sealed class Equivalent : BinaryOp {
        public Equivalent(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.Equivalent();
        }
    }

    // x ~! y
    sealed class NotEquivalent : BinaryOp {
        public NotEquivalent(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.NotEquivalent();
        }
    }

    // x > y
    sealed class GreaterThan : BinaryOp {
        public GreaterThan(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }


        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (!LHS.TryAsConstant(out var lhs) || !RHS.TryAsConstant(out var rhs)) {
                constant = null;
                return false;
            }

            constant = lhs.GreaterThan(rhs);
            return true;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.GreaterThan();
        }
    }

    // x >= y
    sealed class GreaterThanOrEqual : BinaryOp {
        public GreaterThanOrEqual(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

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

            constant = lhs.GreaterThanOrEqual(rhs);
            return true;
        }
    }


    // x < y
    sealed class LessThan : BinaryOp {
        public LessThan(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

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

            constant = lhs.LessThan(rhs);
            return true;
        }
    }

    // x <= y
    sealed class LessThanOrEqual : BinaryOp {
        public LessThanOrEqual(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

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

            constant = lhs.LessThanOrEqual(rhs);
            return true;
        }
    }

    // x || y
    sealed class Or : BinaryOp {
        public Or(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (LHS.TryAsConstant(out var lhs) && lhs.IsTruthy()) {
                constant = lhs;
                return true;
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
    sealed class And : BinaryOp {
        public And(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

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
    sealed class In : BinaryOp {
        public In(Location location, DMExpression expr, DMExpression container)
            : base(location, expr, container) { }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.IsInList();
        }
    }
    #endregion

    #region Compound Assignment
    abstract class AssignmentBinaryOp : BinaryOp {
        public AssignmentBinaryOp(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        /// <summary>
        /// Generic interface for emitting the assignment operation. Has its conditionality and reference generation already handled.
        /// </summary>
        /// <remarks>You should always make use of the reference argument, unless you totally override AssignmentBinaryOp's EmitPushValue method.</remarks>
        /// <param name="reference">A reference to the LHS emitted via <see cref="DMExpression.EmitReference(DMObject, DMProc)"/></param>
        public abstract void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel);

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            string endLabel = proc.NewLabelName();

            DMReference reference = LHS.EmitReference(dmObject, proc, endLabel);
            EmitOp(dmObject, proc, reference, endLabel);

            proc.AddLabel(endLabel);
        }
    }

    // x = y
    sealed class Assignment : AssignmentBinaryOp {
        public override DreamPath? Path => LHS.Path;

        public Assignment(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
            RHS.EmitPushValue(dmObject, proc);
            proc.Assign(reference);
        }
    }
    // x := y
    class AssignmentInto : AssignmentBinaryOp {
        public override DreamPath? Path => LHS.Path;

        public AssignmentInto(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
            RHS.EmitPushValue(dmObject, proc);
            proc.AssignInto(reference);
        }
    }

    // x += y
    sealed class Append : AssignmentBinaryOp {
        public Append(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
            RHS.EmitPushValue(dmObject, proc);
            proc.Append(reference);
        }
    }

    // x |= y
    sealed class Combine : AssignmentBinaryOp {
        public Combine(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
            RHS.EmitPushValue(dmObject, proc);
            proc.Combine(reference);
        }
    }

    // x -= y
    sealed class Remove : AssignmentBinaryOp {
        public Remove(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
            RHS.EmitPushValue(dmObject, proc);
            proc.Remove(reference);
        }
    }

    // x &= y
    sealed class Mask : AssignmentBinaryOp {
        public Mask(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
            RHS.EmitPushValue(dmObject, proc);
            proc.Mask(reference);
        }
    }

    // x &&= y
    sealed class LogicalAndAssign : AssignmentBinaryOp {
        public LogicalAndAssign(Location location, DMExpression lhs, DMExpression rhs) : base(location, lhs, rhs) { }

        public override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
            proc.JumpIfFalseReference(reference, endLabel);
            RHS.EmitPushValue(dmObject, proc);
            proc.Assign(reference);
        }
    }

    // x ||= y
    sealed class LogicalOrAssign : AssignmentBinaryOp {
        public LogicalOrAssign(Location location, DMExpression lhs, DMExpression rhs) : base(location, lhs, rhs) { }

        public override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
            proc.JumpIfTrueReference(reference, endLabel);
            RHS.EmitPushValue(dmObject, proc);
            proc.Assign(reference);
        }
    }

    // x *= y
    sealed class MultiplyAssign : AssignmentBinaryOp {
        public MultiplyAssign(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
            RHS.EmitPushValue(dmObject, proc);
            proc.MultiplyReference(reference);
        }
    }

    // x /= y
    sealed class DivideAssign : AssignmentBinaryOp {
        public DivideAssign(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
            RHS.EmitPushValue(dmObject, proc);
            proc.DivideReference(reference);
        }
    }

    // x <<= y
    sealed class LeftShiftAssign : AssignmentBinaryOp {
        public LeftShiftAssign(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
            RHS.EmitPushValue(dmObject, proc);
            proc.BitShiftLeftReference(reference);
        }
    }

    // x >>= y
    sealed class RightShiftAssign : AssignmentBinaryOp {
        public RightShiftAssign(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
            RHS.EmitPushValue(dmObject, proc);
            proc.BitShiftRightReference(reference);
        }
    }

    // x ^= y
    sealed class XorAssign : AssignmentBinaryOp {
        public XorAssign(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
            RHS.EmitPushValue(dmObject, proc);
            proc.BinaryXorReference(reference);
        }
    }

    // x %= y
    sealed class ModulusAssign : AssignmentBinaryOp {
        public ModulusAssign(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
            RHS.EmitPushValue(dmObject, proc);
            proc.ModulusReference(reference);
        }
    }

    // x %%= y
    sealed class ModulusModulusAssign : AssignmentBinaryOp {
        public ModulusModulusAssign(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference, string endLabel) {
            RHS.EmitPushValue(dmObject, proc);
            proc.ModulusModulusReference(reference);
        }
    }
    #endregion
}
