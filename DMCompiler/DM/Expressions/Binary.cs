using OpenDreamShared.Compiler;
using OpenDreamShared.Dream.Procs;

namespace DMCompiler.DM.Expressions {
    abstract class BinaryOp : DMExpression {
        protected DMExpression LHS { get; }
        protected DMExpression RHS { get; }

        public BinaryOp(Location location, DMExpression lhs, DMExpression rhs) : base(location) {
            LHS = lhs;
            RHS = rhs;
        }
    }

    #region Simple
    // x + y
    class Add : BinaryOp {
        public Add(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override bool TryAsConstant(out Constant constant) {
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
    class Subtract : BinaryOp {
        public Subtract(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override bool TryAsConstant(out Constant constant) {
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
    class Multiply : BinaryOp {
        public Multiply(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override bool TryAsConstant(out Constant constant) {
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
    class Divide : BinaryOp {
        public Divide(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override bool TryAsConstant(out Constant constant) {
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
    class Modulo : BinaryOp {
        public Modulo(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override bool TryAsConstant(out Constant constant) {
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

    // x ** y
    class Power : BinaryOp {
        public Power(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override bool TryAsConstant(out Constant constant) {
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
    class LeftShift : BinaryOp {
        public LeftShift(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override bool TryAsConstant(out Constant constant) {
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
    class RightShift : BinaryOp {
        public RightShift(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override bool TryAsConstant(out Constant constant) {
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
    class BinaryAnd : BinaryOp {
        public BinaryAnd(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override bool TryAsConstant(out Constant constant) {
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
    class BinaryXor : BinaryOp {
        public BinaryXor(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override bool TryAsConstant(out Constant constant) {
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
    class BinaryOr : BinaryOp {
        public BinaryOr(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override bool TryAsConstant(out Constant constant) {
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
    class Equal : BinaryOp {
        public Equal(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.Equal();
        }
    }

    // x != y
    class NotEqual : BinaryOp {
        public NotEqual(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.NotEqual();
        }
    }

    // x ~= y
    class Equivalent : BinaryOp {
        public Equivalent(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.Equivalent();
        }
    }

    // x ~! y
    class NotEquivalent : BinaryOp {
        public NotEquivalent(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.NotEquivalent();
        }
    }

    // x > y
    class GreaterThan : BinaryOp {
        public GreaterThan(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.GreaterThan();
        }
    }

    // x >= y
    class GreaterThanOrEqual : BinaryOp {
        public GreaterThanOrEqual(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.GreaterThanOrEqual();
        }
    }


    // x < y
    class LessThan : BinaryOp {
        public LessThan(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.LessThan();
        }
    }

    // x <= y
    class LessThanOrEqual : BinaryOp {
        public LessThanOrEqual(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.LessThanOrEqual();
        }
    }

    // x || y
    class Or : BinaryOp {
        public Or(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override bool TryAsConstant(out Constant constant) {
            if (!LHS.TryAsConstant(out var lhs) || !RHS.TryAsConstant(out var rhs)) {
                constant = null;
                return false;
            }

            constant = lhs.Or(rhs);
            return true;
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
    class And : BinaryOp {
        public And(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override bool TryAsConstant(out Constant constant) {
            if (!LHS.TryAsConstant(out var lhs) || !RHS.TryAsConstant(out var rhs)) {
                constant = null;
                return false;
            }

            constant = lhs.And(rhs);
            return true;
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
    class In : BinaryOp {
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

        public abstract void EmitOp(DMObject dmObject, DMProc proc, DMReference reference);

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            (DMReference reference, bool conditional) = LHS.EmitReference(dmObject, proc);

            if (conditional) {
                string nullSkipLabel = proc.NewLabelName();

                proc.JumpIfNullDereference(reference, nullSkipLabel);
                EmitOp(dmObject, proc, reference);
                proc.AddLabel(nullSkipLabel);
            } else {
                EmitOp(dmObject, proc, reference);
            }
        }
    }

    // x = y
    class Assignment : AssignmentBinaryOp {
        public Assignment(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference) {
            RHS.EmitPushValue(dmObject, proc);
            proc.Assign(reference);
        }
    }

    // x += y
    class Append : AssignmentBinaryOp {
        public Append(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference) {
            RHS.EmitPushValue(dmObject, proc);
            proc.Append(reference);
        }
    }

    // x |= y
    class Combine : AssignmentBinaryOp {
        public Combine(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference) {
            RHS.EmitPushValue(dmObject, proc);
            proc.Combine(reference);
        }
    }

    // x -= y
    class Remove : AssignmentBinaryOp {
        public Remove(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference) {
            RHS.EmitPushValue(dmObject, proc);
            proc.Remove(reference);
        }
    }

    // x &= y
    class Mask : AssignmentBinaryOp {
        public Mask(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference) {
            RHS.EmitPushValue(dmObject, proc);
            proc.Mask(reference);
        }
    }

    // x &&= y
    class LogicalAndAssign : AssignmentBinaryOp {
        public LogicalAndAssign(Location location, DMExpression lhs, DMExpression rhs) : base(location, lhs, rhs) { }

        public override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference) {
            string skipLabel = proc.NewLabelName();
            string endLabel = proc.NewLabelName();

            proc.PushReferenceValue(reference);
            proc.JumpIfFalse(skipLabel);

            RHS.EmitPushValue(dmObject, proc);
            proc.Assign(reference);
            proc.Jump(endLabel);

            proc.AddLabel(skipLabel);
            proc.PushReferenceValue(reference);
            proc.AddLabel(endLabel);
        }
    }

    // x ||= y
    class LogicalOrAssign : AssignmentBinaryOp {
        public LogicalOrAssign(Location location, DMExpression lhs, DMExpression rhs) : base(location, lhs, rhs) { }

        public override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference) {
            string skipLabel = proc.NewLabelName();
            string endLabel = proc.NewLabelName();

            proc.PushReferenceValue(reference);
            proc.JumpIfTrue(skipLabel);

            RHS.EmitPushValue(dmObject, proc);
            proc.Assign(reference);
            proc.Jump(endLabel);

            proc.AddLabel(skipLabel);
            proc.PushReferenceValue(reference);
            proc.AddLabel(endLabel);
        }
    }

    // x *= y
    class MultiplyAssign : AssignmentBinaryOp {
        public MultiplyAssign(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference) {
            RHS.EmitPushValue(dmObject, proc);
            proc.MultiplyReference(reference);
        }
    }

    // x /= y
    class DivideAssign : AssignmentBinaryOp {
        public DivideAssign(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference) {
            RHS.EmitPushValue(dmObject, proc);
            proc.DivideReference(reference);
        }
    }

    // x <<= y
    class LeftShiftAssign : AssignmentBinaryOp {
        public LeftShiftAssign(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference) {
            proc.PushReferenceValue(reference);
            RHS.EmitPushValue(dmObject, proc);
            proc.BitShiftLeft();
            proc.Assign(reference);
        }
    }

    // x >>= y
    class RightShiftAssign : AssignmentBinaryOp {
        public RightShiftAssign(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference) {
            proc.PushReferenceValue(reference);
            RHS.EmitPushValue(dmObject, proc);
            proc.BitShiftRight();
            proc.Assign(reference);
        }
    }

    // x ^= y
    class XorAssign : AssignmentBinaryOp {
        public XorAssign(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference) {
            RHS.EmitPushValue(dmObject, proc);
            proc.BinaryXorReference(reference);
        }
    }

    // x %= y
    class ModulusAssign : AssignmentBinaryOp {
        public ModulusAssign(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs) { }

        public override void EmitOp(DMObject dmObject, DMProc proc, DMReference reference) {
            RHS.EmitPushValue(dmObject, proc);
            proc.ModulusReference(reference);
        }
    }
    #endregion
}
