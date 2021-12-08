using OpenDreamShared.Compiler;

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
            : base(location, lhs, rhs)
        {}

        public override Constant ToConstant()
        {
            var lhs = LHS.ToConstant();
            var rhs = RHS.ToConstant();
            return lhs.Add(rhs);
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
            : base(location, lhs, rhs)
        {}

        public override Constant ToConstant()
        {
            var lhs = LHS.ToConstant();
            var rhs = RHS.ToConstant();
            return lhs.Subtract(rhs);
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
            : base(location, lhs, rhs)
        {}

        public override Constant ToConstant()
        {
            var lhs = LHS.ToConstant();
            var rhs = RHS.ToConstant();
            return lhs.Multiply(rhs);
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
            : base(location, lhs, rhs)
        {}

        public override Constant ToConstant()
        {
            var lhs = LHS.ToConstant();
            var rhs = RHS.ToConstant();
            return lhs.Divide(rhs);
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
            : base(location, lhs, rhs)
        {}

        public override Constant ToConstant()
        {
            var lhs = LHS.ToConstant();
            var rhs = RHS.ToConstant();
            return lhs.Modulo(rhs);
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
            : base(location, lhs, rhs)
        {}

        public override Constant ToConstant()
        {
            var lhs = LHS.ToConstant();
            var rhs = RHS.ToConstant();
            return lhs.Power(rhs);
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
            : base(location, lhs, rhs)
        {}

        public override Constant ToConstant()
        {
            var lhs = LHS.ToConstant();
            var rhs = RHS.ToConstant();
            return lhs.LeftShift(rhs);
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
            : base(location, lhs, rhs)
        {}

        public override Constant ToConstant()
        {
            var lhs = LHS.ToConstant();
            var rhs = RHS.ToConstant();
            return lhs.RightShift(rhs);
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
            : base(location, lhs, rhs)
        {}

        public override Constant ToConstant()
        {
            var lhs = LHS.ToConstant();
            var rhs = RHS.ToConstant();
            return lhs.BinaryAnd(rhs);
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
            : base(location, lhs, rhs)
        {}

        public override Constant ToConstant()
        {
            var lhs = LHS.ToConstant();
            var rhs = RHS.ToConstant();
            return lhs.BinaryXor(rhs);
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
            : base(location, lhs, rhs)
        {}

        public override Constant ToConstant()
        {
            var lhs = LHS.ToConstant();
            var rhs = RHS.ToConstant();
            return lhs.BinaryOr(rhs);
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
            : base(location, lhs, rhs)
        {}

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.Equal();
        }
    }

    // x != y
    class NotEqual : BinaryOp {
        public NotEqual(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs)
        {}

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.NotEqual();
        }
    }

    // x ~= y
    class Equivalent : BinaryOp {
        public Equivalent(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs)
        {}

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.Equivalent();
        }
    }

    // x ~! y
    class NotEquivalent : BinaryOp {
        public NotEquivalent(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs)
        {}

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.NotEquivalent();
        }
    }

    // x > y
    class GreaterThan : BinaryOp {
        public GreaterThan(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs)
        {}

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.GreaterThan();
        }
    }

    // x >= y
    class GreaterThanOrEqual : BinaryOp {
        public GreaterThanOrEqual(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs)
        {}

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.GreaterThanOrEqual();
        }
    }


    // x < y
    class LessThan : BinaryOp {
        public LessThan(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs)
        {}

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.LessThan();
        }
    }

    // x <= y
    class LessThanOrEqual : BinaryOp {
        public LessThanOrEqual(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs)
        {}

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.LessThanOrEqual();
        }
    }

    // x || y
    class Or : BinaryOp {
        public Or(Location location, DMExpression lhs, DMExpression rhs)
            : base(location, lhs, rhs)
        {}

        public override Constant ToConstant()
        {
            var lhs = LHS.ToConstant();
            var rhs = RHS.ToConstant();
            return lhs.Or(rhs);
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
            : base(location, lhs, rhs)
        {}

        public override Constant ToConstant()
        {
            var lhs = LHS.ToConstant();
            var rhs = RHS.ToConstant();
            return lhs.And(rhs);
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
            : base(location, expr, container)
        {}

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
                : base(location, lhs, rhs)
            {}

            public abstract void EmitOp(DMObject dmObject, DMProc proc);

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                switch (LHS.EmitIdentifier(dmObject, proc)) {
                    case IdentifierPushResult.Unconditional:
                        RHS.EmitPushValue(dmObject, proc);
                        EmitOp(dmObject, proc);
                        break;

                    case IdentifierPushResult.Conditional:
                        var skipLabel = proc.NewLabelName();
                        var endLabel = proc.NewLabelName();
                        proc.JumpIfNullIdentifier(skipLabel);
                        RHS.EmitPushValue(dmObject, proc);
                        EmitOp(dmObject, proc);
                        proc.Jump(endLabel);
                        proc.AddLabel(skipLabel);
                        proc.Pop();
                        proc.PushNull();
                        proc.AddLabel(endLabel);
                        break;
                }

            }
        }

        // Same as AssignmentBinaryOp except the lhs identifier is pushed to the stack twice
        abstract class DoubleAssignmentBinaryOp : BinaryOp {
            public DoubleAssignmentBinaryOp(Location location, DMExpression lhs, DMExpression rhs)
                : base(location, lhs, rhs)
            {}

            public abstract void EmitOps(DMObject dmObject, DMProc proc);

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                var identifierPushResult = LHS.EmitIdentifier(dmObject, proc);
                
                switch (identifierPushResult) {
                    case IdentifierPushResult.Unconditional:
                        proc.PushCopy();
                        RHS.EmitPushValue(dmObject, proc);
                        EmitOps(dmObject, proc);
                        break;

                    case IdentifierPushResult.Conditional:
                        var skipLabel = proc.NewLabelName();
                        var endLabel = proc.NewLabelName();
                        proc.JumpIfNullIdentifier(skipLabel);
                        proc.PushCopy();
                        RHS.EmitPushValue(dmObject, proc);
                        EmitOps(dmObject, proc);
                        proc.Jump(endLabel);
                        proc.AddLabel(skipLabel);
                        proc.Pop();
                        proc.PushNull();
                        proc.AddLabel(endLabel);
                        break;
                }

            }
        }

        // x = y
        class Assignment : AssignmentBinaryOp {
            public Assignment(Location location, DMExpression lhs, DMExpression rhs)
                : base(location, lhs, rhs)
            {}

            public override void EmitOp(DMObject dmObject, DMProc proc)
            {
                proc.Assign();
            }
        }

        // x += y
        class Append : AssignmentBinaryOp {
            public Append(Location location, DMExpression lhs, DMExpression rhs)
                : base(location, lhs, rhs)
            {}

            public override void EmitOp(DMObject dmObject, DMProc proc) {
                proc.Append();
            }
        }

        // x |= y
        class Combine : AssignmentBinaryOp {
            public Combine(Location location, DMExpression lhs, DMExpression rhs)
                : base(location, lhs, rhs)
            {}

            public override void EmitOp(DMObject dmObject, DMProc proc) {
                proc.Combine();
            }
        }

        // x -= y
        class Remove : AssignmentBinaryOp {
            public Remove(Location location, DMExpression lhs, DMExpression rhs)
                : base(location, lhs, rhs)
            {}

            public override void EmitOp(DMObject dmObject, DMProc proc) {
                proc.Remove();
            }
        }

        // x &= y
        class Mask : AssignmentBinaryOp {
            public Mask(Location location, DMExpression lhs, DMExpression rhs)
                : base(location, lhs, rhs)
            {}

            public override void EmitOp(DMObject dmObject, DMProc proc) {
                proc.Mask();
            }
        }

        // x &&= y
        class LogicalAndAssign : BinaryOp {
            public LogicalAndAssign(Location location, DMExpression lhs, DMExpression rhs) : base(location, lhs, rhs) { }
            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                var skipRHSLabel = proc.NewLabelName();
                switch (LHS.EmitIdentifier(dmObject, proc)) {
                    case IdentifierPushResult.Unconditional: {
                            proc.PushCopy();
                            proc.JumpIfFalse(skipRHSLabel);
                            RHS.EmitPushValue(dmObject, proc);
                            proc.Assign();
                            proc.AddLabel(skipRHSLabel);
                            break;
                        }
                    case IdentifierPushResult.Conditional: {
                            var skipLabel = proc.NewLabelName();
                            var endLabel = proc.NewLabelName();
                            proc.JumpIfNullIdentifier(skipLabel);
                            proc.PushCopy();
                            proc.JumpIfFalse(skipRHSLabel);
                            RHS.EmitPushValue(dmObject, proc);
                            proc.Assign();
                            proc.Jump(endLabel);
                            proc.AddLabel(skipLabel);
                            proc.Pop();
                            proc.PushNull();
                            proc.AddLabel(endLabel);
                            proc.AddLabel(skipRHSLabel);
                            break;
                        }
                }
            }
        }

        // x ||= y
        class LogicalOrAssign : BinaryOp {
            public LogicalOrAssign(Location location, DMExpression lhs, DMExpression rhs) : base(location, lhs, rhs) { }
            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                var skipRHSLabel = proc.NewLabelName();
                switch (LHS.EmitIdentifier(dmObject, proc)) {
                    case IdentifierPushResult.Unconditional: {
                        proc.PushCopy();
                        proc.JumpIfTrue(skipRHSLabel);
                        RHS.EmitPushValue(dmObject, proc);
                        proc.Assign();
                        proc.AddLabel(skipRHSLabel);
                        break;
                    }
                    case IdentifierPushResult.Conditional: {
                        var skipLabel = proc.NewLabelName();
                        var endLabel = proc.NewLabelName();
                        proc.JumpIfNullIdentifier(skipLabel);
                        proc.PushCopy();
                        proc.JumpIfTrue(skipRHSLabel);
                        RHS.EmitPushValue(dmObject, proc);
                        proc.Assign();
                        proc.Jump(endLabel);
                        proc.AddLabel(skipLabel);
                        proc.Pop();
                        proc.PushNull();
                        proc.AddLabel(endLabel);
                        proc.AddLabel(skipRHSLabel);
                        break;
                    }
                }
            }
        }

        // x *= y
        class MultiplyAssign : DoubleAssignmentBinaryOp {
            public MultiplyAssign(Location location, DMExpression lhs, DMExpression rhs)
                : base(location, lhs, rhs)
            {}

            public override void EmitOps(DMObject dmObject, DMProc proc) {
                proc.Multiply();
                proc.Assign();
            }
        }

        // x /= y
        class DivideAssign : DoubleAssignmentBinaryOp {
            public DivideAssign(Location location, DMExpression lhs, DMExpression rhs)
                : base(location, lhs, rhs)
            {}

            public override void EmitOps(DMObject dmObject, DMProc proc) {
                proc.Divide();
                proc.Assign();
            }
        }

        // x <<= y
        class LeftShiftAssign : DoubleAssignmentBinaryOp {
            public LeftShiftAssign(Location location, DMExpression lhs, DMExpression rhs)
                : base(location, lhs, rhs)
            {}

            public override void EmitOps(DMObject dmObject, DMProc proc) {
                proc.BitShiftLeft();
                proc.Assign();
            }
        }

        // x >>= y
        class RightShiftAssign : DoubleAssignmentBinaryOp {
            public RightShiftAssign(Location location, DMExpression lhs, DMExpression rhs)
                : base(location, lhs, rhs)
            {}

            public override void EmitOps(DMObject dmObject, DMProc proc) {
                proc.BitShiftRight();
                proc.Assign();
            }
        }

        // x ^= y
        class XorAssign : DoubleAssignmentBinaryOp {
            public XorAssign(Location location, DMExpression lhs, DMExpression rhs)
                : base(location, lhs, rhs)
            {}

            public override void EmitOps(DMObject dmObject, DMProc proc) {
                proc.BinaryXor();
                proc.Assign();
            }
        }

        // x %= y
        class ModulusAssign : DoubleAssignmentBinaryOp {
            public ModulusAssign(Location location, DMExpression lhs, DMExpression rhs)
                : base(location, lhs, rhs)
            {}

            public override void EmitOps(DMObject dmObject, DMProc proc) {
                proc.Modulus();
                proc.Assign();
            }
        }
#endregion
}
