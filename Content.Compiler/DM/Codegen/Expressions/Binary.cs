namespace Content.Compiler.DM.Expressions {
    abstract class BinaryOp : DMExpression {
        protected DMExpression LHS { get; }
        protected DMExpression RHS { get; }

        public BinaryOp(DMExpression lhs, DMExpression rhs) {
            LHS = lhs;
            RHS = rhs;
        }
    }

#region Simple
    // x + y
    class Add : BinaryOp {
        public Add(DMExpression lhs, DMExpression rhs)
            : base(lhs, rhs)
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
        public Subtract(DMExpression lhs, DMExpression rhs)
            : base(lhs, rhs)
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
        public Multiply(DMExpression lhs, DMExpression rhs)
            : base(lhs, rhs)
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
        public Divide(DMExpression lhs, DMExpression rhs)
            : base(lhs, rhs)
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
        public Modulo(DMExpression lhs, DMExpression rhs)
            : base(lhs, rhs)
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
        public Power(DMExpression lhs, DMExpression rhs)
            : base(lhs, rhs)
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
        public LeftShift(DMExpression lhs, DMExpression rhs)
            : base(lhs, rhs)
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
        public RightShift(DMExpression lhs, DMExpression rhs)
            : base(lhs, rhs)
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
        public BinaryAnd(DMExpression lhs, DMExpression rhs)
            : base(lhs, rhs)
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
        public BinaryXor(DMExpression lhs, DMExpression rhs)
            : base(lhs, rhs)
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
        public BinaryOr(DMExpression lhs, DMExpression rhs)
            : base(lhs, rhs)
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
        public Equal(DMExpression lhs, DMExpression rhs)
            : base(lhs, rhs)
        {}

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.Equal();
        }
    }

    // x != y
    class NotEqual : BinaryOp {
        public NotEqual(DMExpression lhs, DMExpression rhs)
            : base(lhs, rhs)
        {}

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.NotEqual();
        }
    }

    // x > y
    class GreaterThan : BinaryOp {
        public GreaterThan(DMExpression lhs, DMExpression rhs)
            : base(lhs, rhs)
        {}

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.GreaterThan();
        }
    }

    // x >= y
    class GreaterThanOrEqual : BinaryOp {
        public GreaterThanOrEqual(DMExpression lhs, DMExpression rhs)
            : base(lhs, rhs)
        {}

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.GreaterThanOrEqual();
        }
    }


    // x < y
    class LessThan : BinaryOp {
        public LessThan(DMExpression lhs, DMExpression rhs)
            : base(lhs, rhs)
        {}

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.LessThan();
        }
    }

    // x <= y
    class LessThanOrEqual : BinaryOp {
        public LessThanOrEqual(DMExpression lhs, DMExpression rhs)
            : base(lhs, rhs)
        {}

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            LHS.EmitPushValue(dmObject, proc);
            RHS.EmitPushValue(dmObject, proc);
            proc.LessThanOrEqual();
        }
    }

    // x || y
    class Or : BinaryOp {
        public Or(DMExpression lhs, DMExpression rhs)
            : base(lhs, rhs)
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
        public And(DMExpression lhs, DMExpression rhs)
            : base(lhs, rhs)
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
        public In(DMExpression expr, DMExpression container)
            : base(expr, container)
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
            public AssignmentBinaryOp(DMExpression lhs, DMExpression rhs)
                : base(lhs, rhs)
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
            public DoubleAssignmentBinaryOp(DMExpression lhs, DMExpression rhs)
                : base(lhs, rhs)
            {}

            public abstract void EmitOps(DMObject dmObject, DMProc proc);

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                var identifierPushResult = LHS.EmitIdentifier(dmObject, proc);
                proc.PushCopy();

                switch (identifierPushResult) {
                    case IdentifierPushResult.Unconditional:
                        RHS.EmitPushValue(dmObject, proc);
                        EmitOps(dmObject, proc);
                        break;

                    case IdentifierPushResult.Conditional:
                        var skipLabel = proc.NewLabelName();
                        var endLabel = proc.NewLabelName();
                        proc.JumpIfNullIdentifier(skipLabel);
                        RHS.EmitPushValue(dmObject, proc);
                        EmitOps(dmObject, proc);
                        proc.Jump(endLabel);
                        proc.AddLabel(skipLabel);
                        proc.Pop();
                        proc.Pop();
                        proc.PushNull();
                        proc.AddLabel(endLabel);
                        break;
                }

            }
        }

        // x = y
        class Assignment : AssignmentBinaryOp {
            public Assignment(DMExpression lhs, DMExpression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitOp(DMObject dmObject, DMProc proc)
            {
                proc.Assign();
            }
        }

        // x += y
        class Append : AssignmentBinaryOp {
            public Append(DMExpression lhs, DMExpression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitOp(DMObject dmObject, DMProc proc) {
                proc.Append();
            }
        }

        // x |= y
        class Combine : AssignmentBinaryOp {
            public Combine(DMExpression lhs, DMExpression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitOp(DMObject dmObject, DMProc proc) {
                proc.Combine();
            }
        }

        // x -= y
        class Remove : AssignmentBinaryOp {
            public Remove(DMExpression lhs, DMExpression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitOp(DMObject dmObject, DMProc proc) {
                proc.Remove();
            }
        }

        // x &= y
        class Mask : AssignmentBinaryOp {
            public Mask(DMExpression lhs, DMExpression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitOp(DMObject dmObject, DMProc proc) {
                proc.Mask();
            }
        }

        // x *= y
        class MultiplyAssign : DoubleAssignmentBinaryOp {
            public MultiplyAssign(DMExpression lhs, DMExpression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitOps(DMObject dmObject, DMProc proc) {
                proc.Multiply();
                proc.Assign();
            }
        }

        // x /= y
        class DivideAssign : DoubleAssignmentBinaryOp {
            public DivideAssign(DMExpression lhs, DMExpression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitOps(DMObject dmObject, DMProc proc) {
                proc.Divide();
                proc.Assign();
            }
        }

        // x <<= y
        class LeftShiftAssign : DoubleAssignmentBinaryOp {
            public LeftShiftAssign(DMExpression lhs, DMExpression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitOps(DMObject dmObject, DMProc proc) {
                proc.BitShiftLeft();
                proc.Assign();
            }
        }

        // x >>= y
        class RightShiftAssign : DoubleAssignmentBinaryOp {
            public RightShiftAssign(DMExpression lhs, DMExpression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitOps(DMObject dmObject, DMProc proc) {
                proc.BitShiftRight();
                proc.Assign();
            }
        }

        // x ^= y
        class XorAssign : DoubleAssignmentBinaryOp {
            public XorAssign(DMExpression lhs, DMExpression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitOps(DMObject dmObject, DMProc proc) {
                proc.BinaryXor();
                proc.Assign();
            }
        }

        // x %= y
        class ModulusAssign : DoubleAssignmentBinaryOp {
            public ModulusAssign(DMExpression lhs, DMExpression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitOps(DMObject dmObject, DMProc proc) {
                proc.Modulus();
                proc.Assign();
            }
        }
#endregion
}
