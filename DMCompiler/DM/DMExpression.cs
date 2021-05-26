using DMCompiler.DM.Visitors;
using OpenDreamShared.Compiler.DM;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;
using System;
using System.Collections.Generic;

namespace DMCompiler.DM {
    abstract class DMExpression {
        public enum ProcPushResult {
            // The emitted code has pushed the proc onto the stack
            Unconditional,

            // The emitted code has pushed either null or the proc onto the stack
            // If null was pushed, any calls to this proc should silently evaluate to null
            Conditional,
        }

        public enum IdentifierPushResult {
            // The emitted code has pushed the identifier onto the stack
            Unconditional,

            // The emitted code has pushed either null or the identifier onto the stack
            // If null was pushed, any assignments to this identifier should silently evaluate to null
            Conditional,
        }

        public static DMExpression Create(DMObject dmObject, DMProc proc, DMASTExpression expression, DreamPath? inferredPath = null) {
            var instance = new DMVisitorExpression(dmObject, proc, inferredPath);
            expression.Visit(instance);
            return instance.Result;
        }

        public static void Emit(DMObject dmObject, DMProc proc, DMASTExpression expression, DreamPath? inferredPath = null) {
            var expr = Create(dmObject, proc, expression, inferredPath);
            expr.EmitPushValue(dmObject, proc);
        }

        public static Expressions.Constant Constant(DMObject dmObject, DMProc proc, DMASTExpression expression) {
            var expr = Create(dmObject, proc, expression, null);
            return expr.ToConstant();
        }

        // Attempt to convert this expression into a Constant expression
        public virtual Expressions.Constant ToConstant() {
            throw new Exception($"expression {this} can not be const-evaluated");
        }

        // Emits code that pushes the result of this expression to the proc's stack
        // May throw if this expression is unable to be pushed to the stack
        public abstract void EmitPushValue(DMObject dmObject, DMProc proc);

        // Emits code that pushes the identifier of this expression to the proc's stack
        // May throw if this expression is unable to be written
        public virtual IdentifierPushResult EmitIdentifier(DMObject dmObject, DMProc proc) {
            throw new Exception("attempt to assign to r-value");
        }

        public virtual ProcPushResult EmitPushProc(DMObject dmObject, DMProc proc) {
            throw new Exception("attempt to use non-proc expression as proc");
        }

        public virtual DreamPath? Path => null;
    }

    // (a, b, c, ...)
    // This isn't an expression, it's just a helper class for working with argument lists
    class ArgumentList {
        (string Name, DMExpression Expr)[] Expressions;
        public int Length => Expressions.Length;

        public ArgumentList(DMObject dmObject, DMProc proc, DMASTCallParameter[] arguments, DreamPath? inferredPath = null) {
            if (arguments == null) {
                Expressions = new (string, DMExpression)[0];
                return;
            }

            Expressions = new (string, DMExpression)[arguments.Length];

            int idx = 0;
            foreach(var arg in arguments) {
                var expr = DMExpression.Create(dmObject, proc, arg.Value, inferredPath);
                Expressions[idx++] = (arg.Name, expr);
            }
        }

        public void EmitPushArguments(DMObject dmObject, DMProc proc) {
            if (Expressions.Length == 0) {
                proc.PushArguments(0);
                return;
            }

            if (Expressions[0].Name == null && Expressions[0].Expr is Expressions.Arglist arglist) {
                if (Expressions.Length != 1) {
                    throw new Exception("`arglist` expression should be the only argument");
                }

                arglist.EmitPushArglist(dmObject, proc);
                return;
            }

            List<DreamProcOpcodeParameterType> parameterTypes = new List<DreamProcOpcodeParameterType>();
            List<string> parameterNames = new List<string>();

            foreach ((string name, DMExpression expr) in Expressions) {
                expr.EmitPushValue(dmObject, proc);

                if (name != null) {
                    parameterTypes.Add(DreamProcOpcodeParameterType.Named);
                    parameterNames.Add(name);
                } else {
                    parameterTypes.Add(DreamProcOpcodeParameterType.Unnamed);
                }
            }

            proc.PushArguments(Expressions.Length, parameterTypes.ToArray(), parameterNames.ToArray());
        }
    }

    namespace Expressions {
        abstract class LValue : DMExpression {
            public override DreamPath? Path => _path;
            DreamPath? _path;

            public LValue(DreamPath? path) {
                _path = path;
            }

            // At the moment this generally always matches EmitPushValue for any modifiable type
            public override IdentifierPushResult EmitIdentifier(DMObject dmObject, DMProc proc) {
                EmitPushValue(dmObject, proc);
                return IdentifierPushResult.Unconditional;
            }
        }

        abstract class UnaryOp : DMExpression {
            protected DMExpression Expr { get; }

            public UnaryOp(DMExpression expr) {
                Expr = expr;
            }
        }

        abstract class BinaryOp : DMExpression {
            protected DMExpression LHS { get; }
            protected DMExpression RHS { get; }

            public BinaryOp(DMExpression lhs, DMExpression rhs) {
                LHS = lhs;
                RHS = rhs;
            }
        }

        // src
        class Src : LValue {
            public Src(DreamPath? path)
                : base(path)
            {}

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                proc.PushSrc();
            }
        }

        // usr
        class Usr : LValue {
            public Usr()
                : base(DreamPath.Mob)
            {}

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                proc.PushUsr();
            }
        }

        // args
        class Args : LValue {
            public Args()
                : base(DreamPath.List)
            {}

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                proc.GetIdentifier("args");
            }
        }

        // Identifier of local variable
        class Local : LValue {
            string Name { get; }

            public Local(DreamPath? path, string name)
                : base(path) {
                Name = name;
            }

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                proc.PushLocalVariable(Name);
            }
        }

        // Identifier of field (potentially a global variable)
        class Field : LValue {
            string Name { get; }

            public Field(DreamPath? path, string name)
                : base(path) {
                Name = name;
            }

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                proc.GetIdentifier(Name);
            }

            public void EmitPushInitial(DMProc proc) {
                proc.PushSrc();
                proc.Initial(Name);
            }
        }


        // "abc[d]"
        class StringFormat : DMExpression {
            string Value { get; }
            DMExpression[] Expressions { get; }

            public StringFormat(string value, DMExpression[] expressions) {
                Value = value;
                Expressions = expressions;
            }

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                for (int i = Expressions.Length - 1; i >= 0; i--) {
                    Expressions[i].EmitPushValue(dmObject, proc);
                }

                proc.FormatString(Value);
            }
        }

        // -x
        class Negate : UnaryOp {
            public Negate(DMExpression expr)
                : base(expr)
            {}

            public override Constant ToConstant()
            {
                return Expr.ToConstant().Negate();
            }

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                Expr.EmitPushValue(dmObject, proc);
                proc.Negate();
            }
        }

        // !x
        class Not : UnaryOp {
            public Not(DMExpression expr)
                : base(expr)
            {}

            public override Constant ToConstant()
            {
                return Expr.ToConstant().Not();
            }

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                Expr.EmitPushValue(dmObject, proc);
                proc.Not();
            }
        }

        // ~x
        class BinaryNot : UnaryOp {
            public BinaryNot(DMExpression expr)
                : base(expr)
            {}

            public override Constant ToConstant()
            {
                return Expr.ToConstant().BinaryNot();
            }

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                Expr.EmitPushValue(dmObject, proc);
                proc.BinaryNot();
            }
        }

        // ++x
        class PreIncrement : UnaryOp {
            public PreIncrement(DMExpression expr)
                : base(expr)
            {}

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                Expr.EmitPushValue(dmObject, proc);
                proc.PushFloat(1);
                proc.Append();
            }
        }

        // x++
        class PostIncrement : UnaryOp {
            public PostIncrement(DMExpression expr)
                : base(expr)
            {}

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                // TODO: THIS IS WRONG! We have to just keep the value on the stack instead of running our LHS twice
                Expr.EmitPushValue(dmObject, proc);
                Expr.EmitPushValue(dmObject, proc);
                proc.PushFloat(1);
                proc.Append();
                proc.Pop();
            }
        }

        // --x
        class PreDecrement : UnaryOp {
            public PreDecrement(DMExpression expr)
                : base(expr)
            {}

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                Expr.EmitPushValue(dmObject, proc);
                proc.PushFloat(1);
                proc.Remove();
            }
        }

        // x--
        class PostDecrement : UnaryOp {
            public PostDecrement(DMExpression expr)
                : base(expr)
            {}

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                // TODO: THIS IS WRONG! We have to just keep the value on the stack instead of running our LHS twice
                Expr.EmitPushValue(dmObject, proc);
                Expr.EmitPushValue(dmObject, proc);
                proc.PushFloat(1);
                proc.Remove();
                proc.Pop();
            }
        }

#region Binary Ops
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
#endregion

#region Binary Ops (with l-value mutation)
        abstract class MutatingBinaryOp : BinaryOp {
            public MutatingBinaryOp(DMExpression lhs, DMExpression rhs)
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

        abstract class DoubleMutatingBinaryOp : BinaryOp {
            public DoubleMutatingBinaryOp(DMExpression lhs, DMExpression rhs)
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
        class Assignment : MutatingBinaryOp {
            public Assignment(DMExpression lhs, DMExpression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitOp(DMObject dmObject, DMProc proc)
            {
                proc.Assign();
            }
        }

        // x += y
        class Append : MutatingBinaryOp {
            public Append(DMExpression lhs, DMExpression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitOp(DMObject dmObject, DMProc proc) {
                proc.Append();
            }
        }

        // x |= y
        class Combine : MutatingBinaryOp {
            public Combine(DMExpression lhs, DMExpression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitOp(DMObject dmObject, DMProc proc) {
                proc.Combine();
            }
        }

        // x -= y
        class Remove : MutatingBinaryOp {
            public Remove(DMExpression lhs, DMExpression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitOp(DMObject dmObject, DMProc proc) {
                proc.Remove();
            }
        }

        // x &= y
        class Mask : MutatingBinaryOp {
            public Mask(DMExpression lhs, DMExpression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitOp(DMObject dmObject, DMProc proc) {
                proc.Mask();
            }
        }

        // x *= y
        class MultiplyAssign : DoubleMutatingBinaryOp {
            public MultiplyAssign(DMExpression lhs, DMExpression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitOps(DMObject dmObject, DMProc proc) {
                proc.Multiply();
                proc.Assign();
            }
        }

        // x /= y
        class DivideAssign : DoubleMutatingBinaryOp {
            public DivideAssign(DMExpression lhs, DMExpression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitOps(DMObject dmObject, DMProc proc) {
                proc.Divide();
                proc.Assign();
            }
        }

        // x <<= y
        class LeftShiftAssign : DoubleMutatingBinaryOp {
            public LeftShiftAssign(DMExpression lhs, DMExpression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitOps(DMObject dmObject, DMProc proc) {
                proc.BitShiftLeft();
                proc.Assign();
            }
        }

        // x >>= y
        class RightShiftAssign : DoubleMutatingBinaryOp {
            public RightShiftAssign(DMExpression lhs, DMExpression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitOps(DMObject dmObject, DMProc proc) {
                proc.BitShiftRight();
                proc.Assign();
            }
        }

        // x ^= y
        class XorAssign : DoubleMutatingBinaryOp {
            public XorAssign(DMExpression lhs, DMExpression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitOps(DMObject dmObject, DMProc proc) {
                proc.BinaryXor();
                proc.Assign();
            }
        }
#endregion

        // x ? y : z
        class Ternary : DMExpression {
            DMExpression _a, _b, _c;

            public Ternary(DMExpression a, DMExpression b, DMExpression c) {
                _a = a;
                _b = b;
                _c = c;
            }

            public override Constant ToConstant()
            {
                var a = _a.ToConstant();

                if (a.IsTruthy()) {
                    return _b.ToConstant();
                }

                return _c.ToConstant();
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

        // x() (only the identifier)
        class Proc : DMExpression {
            string _identifier;

            public Proc(string identifier) {
                _identifier = identifier;
            }

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                throw new Exception("attempt to use proc as value");
            }

            public override ProcPushResult EmitPushProc(DMObject dmObject, DMProc proc) {
                if (!dmObject.HasProc(_identifier)) {
                    throw new Exception($"Type + {dmObject.Path} does not have a proc named `{_identifier}`");
                }

                proc.GetProc(_identifier);
                return ProcPushResult.Unconditional;
            }
        }

        // .
        class ProcSelf : LValue {
            public ProcSelf()
                : base(null)
            {}

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                proc.PushSelf();
            }

            public override ProcPushResult EmitPushProc(DMObject dmObject, DMProc proc) {
                proc.PushSelf();
                return ProcPushResult.Unconditional;
            }
        }

        // ..
        class ProcSuper : DMExpression {
            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                throw new Exception("attempt to use proc as value");
            }

            public override ProcPushResult EmitPushProc(DMObject dmObject, DMProc proc) {
                proc.PushSuperProc();
                return ProcPushResult.Unconditional;
            }
        }

        // x(y, z, ...)
        class ProcCall : DMExpression {
            DMExpression _target;
            ArgumentList _arguments;

            public ProcCall(DMExpression target, ArgumentList arguments) {
                _target = target;
                _arguments = arguments;
            }

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {

                var _procResult = _target.EmitPushProc(dmObject, proc);

                switch (_procResult) {
                    case ProcPushResult.Unconditional:
                        if (_arguments.Length == 0 && _target is ProcSuper) {
                            proc.PushProcArguments();
                        } else {
                            _arguments.EmitPushArguments(dmObject, proc);
                        }
                        proc.Call();
                        break;

                    case ProcPushResult.Conditional: {
                        var skipLabel = proc.NewLabelName();
                        var endLabel = proc.NewLabelName();
                        proc.JumpIfNullIdentifier(skipLabel);
                        if (_arguments.Length == 0 && _target is ProcSuper) {
                            proc.PushProcArguments();
                        } else {
                            _arguments.EmitPushArguments(dmObject, proc);
                        }
                        proc.Call();
                        proc.Jump(endLabel);
                        proc.AddLabel(skipLabel);
                        proc.Pop();
                        proc.PushNull();
                        proc.AddLabel(endLabel);
                        break;
                    }
                }
            }
        }

        // call(...)(...)
        class CallStatement : DMExpression {
            DMExpression _a; // Procref, Object, LibName
            DMExpression _b; // ProcName, FuncName
            ArgumentList _procArgs;

            public CallStatement(DMExpression a, ArgumentList procArgs) {
                _a = a;
                _procArgs = procArgs;
            }

            public CallStatement(DMExpression a, DMExpression b, ArgumentList procArgs) {
                _a = a;
                _b = b;
                _procArgs = procArgs;
            }

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                if (_b != null) {
                    _b.EmitPushValue(dmObject, proc);
                }

                _a.EmitPushValue(dmObject, proc);
                _procArgs.EmitPushArguments(dmObject, proc);
                proc.CallStatement();
            }
        }

        // x[y]
        class ListIndex : LValue {
            DMExpression _expr;
            DMExpression _index;

            public ListIndex(DMExpression expr, DMExpression index, DreamPath? path)
                : base(path)
            {
                _expr = expr;
                _index = index;
            }

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                _expr.EmitPushValue(dmObject, proc);
                _index.EmitPushValue(dmObject, proc);
                proc.IndexList();
            }
        }

        // x.y.z
        class Dereference : LValue {
            // Kind of a lazy port
            DMExpression _expr;
            List<(bool conditional, string field)> _fields = new();

            public override DreamPath? Path => _path;
            DreamPath? _path;

            public Dereference(DMExpression expr, DMASTDereference astNode, bool includingLast)
                : base(null) // This gets filled in later
            {
                _expr = expr;

                var current_path = _expr.Path;
                DMASTDereference.Dereference[] dereferences = astNode.Dereferences;
                for (int i = 0; i < (includingLast ? dereferences.Length : dereferences.Length - 1); i++) {
                    DMASTDereference.Dereference deref = dereferences[i];

                    switch (deref.Type) {
                        case DMASTDereference.DereferenceType.Direct: {
                            if (current_path == null) {
                                throw new Exception("Cannot dereference property \"" + deref.Property + "\" because a type specifier is missing");
                            }

                            DMObject dmObject = DMObjectTree.GetDMObject(current_path.Value, false);

                            var current = dmObject.GetVariable(deref.Property);
                            if (current == null) current = dmObject.GetGlobalVariable(deref.Property);
                            if (current == null) throw new Exception("Invalid property \"" + deref.Property + "\" on type " + dmObject.Path);

                            current_path = current.Type;
                            _fields.Add((deref.Conditional, deref.Property));
                            break;
                        }

                        case DMASTDereference.DereferenceType.Search: {
                            var current = new DMVariable(null, deref.Property, false);
                            current_path = current.Type;
                            _fields.Add((deref.Conditional, deref.Property));
                            break;
                        }

                        default:
                            throw new InvalidOperationException();
                    }
                }

                _path = current_path;
            }

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                _expr.EmitPushValue(dmObject, proc);

                foreach (var (conditional, field) in _fields) {
                    if (conditional) {
                        proc.DereferenceConditional(field);
                    } else {
                        proc.Dereference(field);
                    }
                }
            }

            public override IdentifierPushResult EmitIdentifier(DMObject dmObject, DMProc proc) {
                _expr.EmitPushValue(dmObject, proc);

                bool lastConditional = false;
                foreach (var (conditional, field) in _fields) {
                    if (conditional) {
                        proc.DereferenceConditional(field);
                    } else {
                        proc.Dereference(field);
                    }

                    lastConditional = conditional;
                }

                if (lastConditional) {
                    return IdentifierPushResult.Conditional;
                } else {
                    return IdentifierPushResult.Unconditional;
                }
            }

            public void EmitPushInitial(DMObject dmObject, DMProc proc) {
                _expr.EmitPushValue(dmObject, proc);

                for (int idx = 0; idx < _fields.Count - 1; idx++)
                {
                    if (_fields[idx].conditional) {
                        proc.DereferenceConditional(_fields[idx].field);
                    } else {
                        proc.Dereference(_fields[idx].field);
                    }
                }

                // TODO: Handle conditional
                proc.Initial(_fields[^1].field);
            }
        }

        // x.y.z()
        class DereferenceProc : DMExpression {
            // Kind of a lazy port
            Dereference _parent;
            bool _conditional;
            string _field;

            public DereferenceProc(DMExpression expr, DMASTDereferenceProc astNode) {
                _parent = new Dereference(expr, astNode, false);

                DMASTDereference.Dereference deref = astNode.Dereferences[^1];
                switch (deref.Type) {
                    case DMASTDereference.DereferenceType.Direct: {
                        if (_parent.Path == null) {
                            throw new Exception("Cannot dereference property \"" + deref.Property + "\" because a type specifier is missing");
                        }

                        DreamPath type = _parent.Path.Value;
                        DMObject dmObject = DMObjectTree.GetDMObject(type, false);

                        if (!dmObject.HasProc(deref.Property)) throw new Exception("Type + " + type + " does not have a proc named \"" + deref.Property + "\"");
                        _conditional = deref.Conditional;
                        _field = deref.Property;
                        break;
                    }

                    case DMASTDereference.DereferenceType.Search: {
                        _conditional = deref.Conditional;
                        _field = deref.Property;
                        break;
                    }

                    default:
                        throw new InvalidOperationException();
                }
            }

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                throw new Exception("attempt to use proc as value");
            }

            public override ProcPushResult EmitPushProc(DMObject dmObject, DMProc proc) {
                _parent.EmitPushValue(dmObject, proc);

                if (_conditional) {
                    proc.DereferenceProcConditional(_field);
                    return ProcPushResult.Conditional;
                } else {
                    proc.DereferenceProc(_field);
                    return ProcPushResult.Unconditional;
                }
            }
        }

        // arglist(...)
        class Arglist : DMExpression {
            DMExpression _expr;

            public Arglist(DMExpression expr) {
                _expr = expr;
            }

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                throw new Exception("invalid use of `arglist`");
            }

            public void EmitPushArglist(DMObject dmObject, DMProc proc) {
                _expr.EmitPushValue(dmObject, proc);
                proc.PushArgumentList();
            }
        }

        // new x (...)
        class New : DMExpression {
            DMExpression Expr;
            ArgumentList Arguments;

            public New(DMExpression expr, ArgumentList arguments) {
                Expr = expr;
                Arguments = arguments;
            }

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                Expr.EmitPushValue(dmObject, proc);
                Arguments.EmitPushArguments(dmObject, proc);
                proc.CreateObject();
            }
        }

        // new /x/y/z (...)
        class NewPath : DMExpression {
            DreamPath TargetPath;
            ArgumentList Arguments;

            public NewPath(DreamPath targetPath, ArgumentList arguments) {
                TargetPath = targetPath;
                Arguments = arguments;
            }

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                proc.PushPath(TargetPath);
                Arguments.EmitPushArguments(dmObject, proc);
                proc.CreateObject();
            }
        }

        // locate()
        class LocateInferred : DMExpression {
            DreamPath _path;
            DMExpression _container;

            public LocateInferred(DreamPath path, DMExpression container) {
                _path = path;
                _container = container;
            }

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                proc.PushPath(_path);

                if (_container != null) {
                    _container.EmitPushValue(dmObject, proc);
                } else {
                    proc.GetIdentifier("world");
                }

                proc.Locate();
            }
        }

        // locate(x)
        class Locate : DMExpression {
            DMExpression _path;
            DMExpression _container;

            public Locate(DMExpression path, DMExpression container) {
                _path = path;
                _container = container;
            }

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                _path.EmitPushValue(dmObject, proc);

                if (_container != null) {
                    _container.EmitPushValue(dmObject, proc);
                } else {
                    proc.GetIdentifier("world");
                }

                proc.Locate();
            }
        }

        // locate(x, y, z)
        class LocateCoordinates : DMExpression {
            DMExpression _x, _y, _z;

            public LocateCoordinates(DMExpression x, DMExpression y, DMExpression z) {
                _x = x;
                _y = y;
                _z = z;
            }

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                _x.EmitPushValue(dmObject, proc);
                _y.EmitPushValue(dmObject, proc);
                _z.EmitPushValue(dmObject, proc);
                proc.LocateCoordinates();
            }
        }

        // istype(x, y)
        class IsType : DMExpression {
            DMExpression _expr;
            DMExpression _path;

            public IsType(DMExpression expr, DMExpression path) {
                _expr = expr;
                _path = path;
            }

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                _expr.EmitPushValue(dmObject, proc);
                _path.EmitPushValue(dmObject, proc);
                proc.IsType();
            }
        }

        // istype(x)
        class IsTypeInferred : DMExpression {
            DMExpression _expr;
            DreamPath _path;

            public IsTypeInferred(DMExpression expr, DreamPath path) {
                _expr = expr;
                _path = path;
            }

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                _expr.EmitPushValue(dmObject, proc);
                proc.PushPath(_path);
                proc.IsType();
            }
        }

        // list(...)
        class List : DMExpression {
            // Lazy
            DMASTList _astNode;

            public List(DMASTList astNode) {
                _astNode = astNode;
            }

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                proc.CreateList();

                if (_astNode.Values != null) {
                    foreach (DMASTCallParameter value in _astNode.Values) {
                        DMASTAssign associatedAssign = value.Value as DMASTAssign;

                        if (associatedAssign != null) {
                            DMExpression.Create(dmObject, proc, associatedAssign.Value).EmitPushValue(dmObject, proc);

                            if (associatedAssign.Expression is DMASTIdentifier) {
                                proc.PushString(value.Name);
                                proc.ListAppendAssociated();
                            } else {
                                DMExpression.Create(dmObject, proc, associatedAssign.Expression).EmitPushValue(dmObject, proc);
                                proc.ListAppendAssociated();
                            }
                        } else {
                            DMExpression.Create(dmObject, proc, value.Value).EmitPushValue(dmObject, proc);

                            if (value.Name != null) {
                                proc.PushString(value.Name);
                                proc.ListAppendAssociated();
                            } else {
                                proc.ListAppend();
                            }
                        }
                    }
                }
            }
        }

        // input(...)
        class Input : DMExpression {
            // Lazy
            DMASTInput _astNode;

            public Input(DMASTInput astNode) {
                _astNode = astNode;
            }

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                if (_astNode.Parameters.Length == 0 || _astNode.Parameters.Length > 4) throw new Exception("Invalid input() parameter count");

                //Push input's four arguments, pushing null for the missing ones
                for (int i = 3; i >= 0; i--) {
                    if (i < _astNode.Parameters.Length) {
                        DMASTCallParameter parameter = _astNode.Parameters[i];

                        if (parameter.Name != null) throw new Exception("input() does not take named arguments");
                        DMExpression.Create(dmObject, proc, parameter.Value).EmitPushValue(dmObject, proc);
                    } else {
                        proc.PushNull();
                    }
                }

                proc.Prompt(_astNode.Types);
            }
        }

        // initial(x)
        class Initial : DMExpression {
            DMExpression _expr;

            public Initial(DMExpression expr) {
                _expr = expr;
            }

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                // TODO: Add EmitPushInitial to Base?

                if (_expr is Field field) {
                    field.EmitPushInitial(proc);
                    return;
                }

                if (_expr is Dereference dereference) {
                    dereference.EmitPushInitial(dmObject, proc);
                    return;
                }

                throw new Exception($"can't get initial value of {_expr}");
            }
        }

        // x in y
        class In : DMExpression {
            DMExpression _expr;
            DMExpression _container;

            public In(DMExpression expr, DMExpression container) {
                _expr = expr;
                _container = container;
            }

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                _expr.EmitPushValue(dmObject, proc);
                _container.EmitPushValue(dmObject, proc);
                proc.IsInList();
            }
        }
    }
}
