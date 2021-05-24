using OpenDreamShared.Compiler.DM;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;
using System;
using System.Collections.Generic;

namespace DMCompiler.DM.Visitors {
    abstract class Expression {
        public static Expression Create(DMObject dmObject, DMProc proc, DMASTExpression expression, DreamPath? inferredPath = null) {
            var instance = new DMVisitorExpression(dmObject, proc, inferredPath);
            expression.Visit(instance);
            return instance.Result;
        }

        // Shortcut to just directly emit code to push the result to the stack
        public static void Emit(DMObject dmObject, DMProc proc, DMASTExpression expression, DreamPath? inferredPath = null) {
            var expr = Create(dmObject, proc, expression, inferredPath);
            expr.EmitPushValue(dmObject, proc);
        }

        // Emits code that pushes the result of this expression to the proc's stack
        // May throw if this expression is unable to be pushed to the stack
        public abstract void EmitPushValue(DMObject dmObject, DMProc proc);

        // Emits code that pushes the identifier of this expression to the proc's stack
        // May throw if this expression is unable to be written
        public virtual void EmitIdentifier(DMObject dmObject, DMProc proc) {
            throw new Exception("attempt to assign to r-value");
        }

        public virtual void EmitPushProc(DMObject dmObject, DMProc proc) {
            throw new Exception("attempt to use non-proc expression as proc");
        }

        public virtual DreamPath? Path => null;
    }

    namespace Expressions {

        abstract class LValue : Expression {
            public override DreamPath? Path => _path;
            DreamPath? _path;

            public LValue(DreamPath? path) {
                _path = path;
            }

            // At the moment this generally always matches EmitPushValue for any modifiable type
            public override void EmitIdentifier(DMObject dmObject, DMProc proc) {
                EmitPushValue(dmObject, proc);
            }
        }

        abstract class UnaryOp : Expression {
            protected Expression Expr { get; }

            public UnaryOp(Expression expr) {
                Expr = expr;
            }
        }

        abstract class BinaryOp : Expression {
            protected Expression LHS { get; }
            protected Expression RHS { get; }

            public BinaryOp(Expression lhs, Expression rhs) {
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

        // null
        class Null : Expression {
            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                proc.PushNull();
            }
        }

        // 4.0, -4.0
        class Number : Expression {
            float Value { get; }

            public Number(int value) {
                Value = value;
            }

            public Number(float value) {
                Value = value;
            }

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                proc.PushFloat(Value);
            }
        }

        // "abc"
        class String : Expression {
            string Value { get; }

            public String(string value) {
                Value = value;
            }

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                proc.PushString(Value);
            }
        }

        // 'abc'
        class Resource : Expression {
            string Value { get; }

            public Resource(string value) {
                Value = value;
            }

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                proc.PushResource(Value);
            }
        }

        // /a/b/c
        class Path : Expression {
            DreamPath Value { get; }

            public Path(DreamPath value) {
                Value = value;
            }

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                proc.PushPath(Value);
            }
        }

        // "abc[d]"
        class StringFormat : Expression {
            string Value { get; }
            Expression[] Expressions { get; }

            public StringFormat(string value, Expression[] expressions) {
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
            public Negate(Expression expr)
                : base(expr)
            {}

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                Expr.EmitPushValue(dmObject, proc);
                proc.Negate();
            }
        }

        // !x
        class Not : UnaryOp {
            public Not(Expression expr)
                : base(expr)
            {}

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                Expr.EmitPushValue(dmObject, proc);
                proc.Not();
            }
        }

        // ~x
        class BinaryNot : UnaryOp {
            public BinaryNot(Expression expr)
                : base(expr)
            {}

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                Expr.EmitPushValue(dmObject, proc);
                proc.BinaryNot();
            }
        }

        // ++x
        class PreIncrement : UnaryOp {
            public PreIncrement(Expression expr)
                : base(expr)
            {}

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                // TODO: THIS IS WRONG! We have to just keep the value on the stack instead of running our LHS twice
                Expr.EmitPushValue(dmObject, proc);
                proc.PushFloat(1);
                proc.Append();
                Expr.EmitPushValue(dmObject, proc);                
            }
        }

        // x++
        class PostIncrement : UnaryOp {
            public PostIncrement(Expression expr)
                : base(expr)
            {}

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                // TODO: THIS IS WRONG! We have to just keep the value on the stack instead of running our LHS twice
                Expr.EmitPushValue(dmObject, proc);
                Expr.EmitPushValue(dmObject, proc);        
                proc.PushFloat(1);
                proc.Append(); 
            }
        }

        // --x
        class PreDecrement : UnaryOp {
            public PreDecrement(Expression expr)
                : base(expr)
            {}

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                // TODO: THIS IS WRONG! We have to just keep the value on the stack instead of running our LHS twice
                Expr.EmitPushValue(dmObject, proc);
                proc.PushFloat(1);
                proc.Remove();
                Expr.EmitPushValue(dmObject, proc);                
            }
        }

        // x--
        class PostDecrement : UnaryOp {
            public PostDecrement(Expression expr)
                : base(expr)
            {}

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                // TODO: THIS IS WRONG! We have to just keep the value on the stack instead of running our LHS twice
                Expr.EmitPushValue(dmObject, proc);
                Expr.EmitPushValue(dmObject, proc);        
                proc.PushFloat(1);
                proc.Remove(); 
            }
        }

#region Binary Ops
        // x + y
        class Add : BinaryOp {
            public Add(Expression lhs, Expression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                LHS.EmitPushValue(dmObject, proc);
                RHS.EmitPushValue(dmObject, proc);
                proc.Add();
            }
        }

        // x - y
        class Subtract : BinaryOp {
            public Subtract(Expression lhs, Expression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                LHS.EmitPushValue(dmObject, proc);
                RHS.EmitPushValue(dmObject, proc);
                proc.Subtract();
            }
        }

        // x * y
        class Multiply : BinaryOp {
            public Multiply(Expression lhs, Expression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                LHS.EmitPushValue(dmObject, proc);
                RHS.EmitPushValue(dmObject, proc);
                proc.Multiply();
            }
        }

        // x / y
        class Divide : BinaryOp {
            public Divide(Expression lhs, Expression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                LHS.EmitPushValue(dmObject, proc);
                RHS.EmitPushValue(dmObject, proc);
                proc.Divide();
            }
        }

        // x % y
        class Modulo : BinaryOp {
            public Modulo(Expression lhs, Expression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                LHS.EmitPushValue(dmObject, proc);
                RHS.EmitPushValue(dmObject, proc);
                proc.Modulus();
            }
        }

        // x ^ y
        class Power : BinaryOp {
            public Power(Expression lhs, Expression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                LHS.EmitPushValue(dmObject, proc);
                RHS.EmitPushValue(dmObject, proc);
                proc.Power();
            }
        }

        // x << y
        class LeftShift : BinaryOp {
            public LeftShift(Expression lhs, Expression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                LHS.EmitPushValue(dmObject, proc);
                RHS.EmitPushValue(dmObject, proc);
                proc.BitShiftLeft();
            }
        }

        // x >> y
        class RightShift : BinaryOp {
            public RightShift(Expression lhs, Expression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                LHS.EmitPushValue(dmObject, proc);
                RHS.EmitPushValue(dmObject, proc);
                proc.BitShiftRight();
            }
        }

        // x & y
        class BinaryAnd : BinaryOp {
            public BinaryAnd(Expression lhs, Expression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                LHS.EmitPushValue(dmObject, proc);
                RHS.EmitPushValue(dmObject, proc);
                proc.BinaryAnd();
            }
        }

        // x ^ y
        class BinaryXor : BinaryOp {
            public BinaryXor(Expression lhs, Expression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                LHS.EmitPushValue(dmObject, proc);
                RHS.EmitPushValue(dmObject, proc);
                proc.BinaryXor();
            }
        }

        // x | y
        class BinaryOr : BinaryOp {
            public BinaryOr(Expression lhs, Expression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                LHS.EmitPushValue(dmObject, proc);
                RHS.EmitPushValue(dmObject, proc);
                proc.BinaryOr();
            }
        }

        // x == y
        class Equal : BinaryOp {
            public Equal(Expression lhs, Expression rhs)
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
            public NotEqual(Expression lhs, Expression rhs)
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
            public GreaterThan(Expression lhs, Expression rhs)
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
            public GreaterThanOrEqual(Expression lhs, Expression rhs)
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
            public LessThan(Expression lhs, Expression rhs)
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
            public LessThanOrEqual(Expression lhs, Expression rhs)
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
            public Or(Expression lhs, Expression rhs)
                : base(lhs, rhs)
            {}

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
            public And(Expression lhs, Expression rhs)
                : base(lhs, rhs)
            {}

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
        // x += y
        class Append : BinaryOp {
            public Append(Expression lhs, Expression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                LHS.EmitIdentifier(dmObject, proc);
                RHS.EmitPushValue(dmObject, proc);
                proc.Append();
            }
        }

        // x |= y
        class Combine : BinaryOp {
            public Combine(Expression lhs, Expression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                LHS.EmitIdentifier(dmObject, proc);
                RHS.EmitPushValue(dmObject, proc);
                proc.Combine();
            }
        }

        // x -= y
        class Remove : BinaryOp {
            public Remove(Expression lhs, Expression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                LHS.EmitIdentifier(dmObject, proc);
                RHS.EmitPushValue(dmObject, proc);
                proc.Remove();
            }
        }

        // x &= y
        class Mask : BinaryOp {
            public Mask(Expression lhs, Expression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                LHS.EmitIdentifier(dmObject, proc);
                RHS.EmitPushValue(dmObject, proc);
                proc.Mask();
            }
        }

        // x *= y
        class MultiplyAssign : BinaryOp {
            public MultiplyAssign(Expression lhs, Expression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                LHS.EmitPushValue(dmObject, proc);
                LHS.EmitIdentifier(dmObject, proc);
                RHS.EmitPushValue(dmObject, proc);
                proc.Multiply();
                proc.Assign();
            }
        }

        // x /= y
        class DivideAssign : BinaryOp {
            public DivideAssign(Expression lhs, Expression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                LHS.EmitPushValue(dmObject, proc);
                LHS.EmitIdentifier(dmObject, proc);
                RHS.EmitPushValue(dmObject, proc);
                proc.Divide();
                proc.Assign();
            }
        }

        // x <<= y
        class LeftShiftAssign : BinaryOp {
            public LeftShiftAssign(Expression lhs, Expression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                LHS.EmitPushValue(dmObject, proc);
                LHS.EmitIdentifier(dmObject, proc);
                RHS.EmitPushValue(dmObject, proc);
                proc.BitShiftLeft();
                proc.Assign();
            }
        }

        // x >>= y
        class RightShiftAssign : BinaryOp {
            public RightShiftAssign(Expression lhs, Expression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                LHS.EmitPushValue(dmObject, proc);
                LHS.EmitIdentifier(dmObject, proc);
                RHS.EmitPushValue(dmObject, proc);
                proc.BitShiftRight();
                proc.Assign();
            }
        }

        // x ^= y
        class XorAssign : BinaryOp {
            public XorAssign(Expression lhs, Expression rhs)
                : base(lhs, rhs)
            {}

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                LHS.EmitPushValue(dmObject, proc);
                LHS.EmitIdentifier(dmObject, proc);
                RHS.EmitPushValue(dmObject, proc);
                proc.BinaryXor();
                proc.Assign();
            }
        }
#endregion

        // x ? y : z
        class Ternary : Expression {
            Expression _a, _b, _c;

            public Ternary(Expression a, Expression b, Expression c) {
                _a = a;
                _b = b;
                _c = c;
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
        class Proc : Expression {
            string _identifier;

            public Proc(string identifier) {
                _identifier = identifier;
            }

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                throw new Exception("attempt to use proc as value");
            }

            public override void EmitPushProc(DMObject dmObject, DMProc proc) {
                if (!dmObject.HasProc(_identifier)) {
                    throw new Exception($"Type + {dmObject.Path} does not have a proc named `{_identifier}`");
                }

                proc.GetProc(_identifier);
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

            public override void EmitPushProc(DMObject dmObject, DMProc proc) {
                proc.PushSelf();
            }
        }

        // ..
        class ProcSuper : Expression {
            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                throw new Exception("attempt to use proc as value");
            }

            public override void EmitPushProc(DMObject dmObject, DMProc proc) {
                proc.PushSuperProc();
            }
        }

        // x(y, z, ...)
        class ProcCall : Expression {
            Expression _target;
            ArgumentList _arguments;

            public ProcCall(Expression target, ArgumentList arguments) {
                _target = target;
                _arguments = arguments;
            }

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                _target.EmitPushProc(dmObject, proc);
                _arguments.EmitPushArguments(dmObject, proc);
                proc.Call();
            }
        }

        // x[y]
        class ListIndex : LValue {
            Expression _expr;
            Expression _index;

            public ListIndex(Expression expr, Expression index)
                : base(null)
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
            Expression _expr;
            List<string> _fields = new();

            public override DreamPath? Path => _path;
            DreamPath? _path;

            public Dereference(Expression expr, DMASTDereference astNode, bool includingLast)
                : base(null) // This gets filled in later
            {
                _expr = expr;

                var current_path = _expr.Path;
                DMASTDereference.Dereference[] dereferences = astNode.Dereferences;
                for (int i = 0; i < (includingLast ? dereferences.Length : dereferences.Length - 1); i++) {
                    DMASTDereference.Dereference deref = dereferences[i];

                    if (deref.Type == DMASTDereference.DereferenceType.Direct) {
                        if (current_path == null) {
                            throw new Exception("Cannot dereference property \"" + deref.Property + "\" because a type specifier is missing");
                        }

                        DMObject dmObject = DMObjectTree.GetDMObject(current_path.Value, false);

                        var current = dmObject.GetVariable(deref.Property);
                        if (current == null) current = dmObject.GetGlobalVariable(deref.Property);
                        if (current == null) throw new Exception("Invalid property \"" + deref.Property + "\" on type " + dmObject.Path);

                        current_path = current.Type;
                        _fields.Add(deref.Property);
                    } else if (deref.Type == DMASTDereference.DereferenceType.Search) { //No compile-time checks
                        var current = new DMVariable(null, deref.Property, false);
                        current_path = current.Type;
                        _fields.Add(deref.Property);
                    }
                }

                _path = current_path;
            }

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                _expr.EmitPushValue(dmObject, proc);

                foreach (var field in _fields) {
                    proc.Dereference(field);
                }
            }

            public void EmitPushInitial(DMObject dmObject, DMProc proc) {
                _expr.EmitPushValue(dmObject, proc);

                for (int idx = 0; idx < _fields.Count - 1; idx++)
                {
                    proc.Dereference(_fields[idx]);
                }

                proc.Initial(_fields[^1]);
            }
        }

        // x.y.z()
        class DereferenceProc : Expression {
            // Kind of a lazy port
            Dereference _parent;
            string _field;

            public DereferenceProc(Expression expr, DMASTDereferenceProc astNode) {
                _parent = new Dereference(expr, astNode, false);

                DMASTDereference.Dereference deref = astNode.Dereferences[^1];
                if (deref.Type == DMASTDereference.DereferenceType.Direct) {
                    if (_parent.Path == null) {
                        throw new Exception("Cannot dereference property \"" + deref.Property + "\" because a type specifier is missing");
                    }

                    DreamPath type = _parent.Path.Value;
                    DMObject dmObject = DMObjectTree.GetDMObject(type, false);

                    if (!dmObject.HasProc(deref.Property)) throw new Exception("Type + " + type + " does not have a proc named \"" + deref.Property + "\"");
                    _field = deref.Property;
                } else if (deref.Type == DMASTDereference.DereferenceType.Search) { //No compile-time checks
                    _field = deref.Property;
                }
            }

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                throw new Exception("attempt to use proc as value");
            }

            public override void EmitPushProc(DMObject dmObject, DMProc proc) {
                _parent.EmitPushValue(dmObject, proc);
                proc.DereferenceProc(_field);
            }
        }

        // x = y
        class Assignment : Expression {
            Expression LHS;
            Expression RHS;

            public Assignment(Expression lhs, Expression rhs) {
                LHS = lhs;
                RHS = rhs;
            }

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                LHS.EmitIdentifier(dmObject, proc);
                RHS.EmitPushValue(dmObject, proc);
                proc.Assign();
            }
        }

        // arglist(...)
        class Arglist : Expression {
            Expression _expr;

            public Arglist(Expression expr) {
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
        class New : Expression {
            Expression Expr;
            ArgumentList Arguments;

            public New(Expression expr, ArgumentList arguments) {
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
        class NewPath : Expression {
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

        // locate() | locate(x)
        class Locate : Expression {
            Expression _path;
            Expression _container;

            public Locate(Expression path, Expression container) {
                _path = path;
                _container = container;
            }

            public override void EmitPushValue(DMObject dmObject, DMProc proc) {
                if (_path != null) {
                    _path.EmitPushValue(dmObject, proc);
                } else {
                    throw new Exception("implicit locate() not implemented");
                }

                if (_container != null) {
                    _container.EmitPushProc(dmObject, proc);
                } else {
                    proc.GetIdentifier("world");
                }

                proc.Locate();
            }
        }

        // locate(x, y, z)
        class LocateCoordinates : Expression {
            Expression _x, _y, _z;

            public LocateCoordinates(Expression x, Expression y, Expression z) {
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
        class IsType : Expression {
            Expression _expr;
            Expression _path;

            public IsType(Expression expr, Expression path) {
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
        class IsTypeInferred : Expression {
            Expression _expr;
            DreamPath _path;

            public IsTypeInferred(Expression expr, DreamPath path) {
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
        class List : Expression {
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
                            Expression.Create(dmObject, proc, associatedAssign.Value).EmitPushValue(dmObject, proc);

                            if (associatedAssign.Expression is DMASTIdentifier) {
                                proc.PushString(value.Name);
                                proc.ListAppendAssociated();
                            } else {
                                Expression.Create(dmObject, proc, associatedAssign.Expression).EmitPushValue(dmObject, proc);
                                proc.ListAppendAssociated();
                            }
                        } else {
                            Expression.Create(dmObject, proc, value.Value).EmitPushValue(dmObject, proc);

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
        class Input : Expression {
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
                        Expression.Create(dmObject, proc, parameter.Value).EmitPushValue(dmObject, proc);
                    } else {
                        proc.PushNull();
                    }
                }

                proc.Prompt(_astNode.Types);
            }
        }

        // initial(x)
        class Initial : Expression {
            Expression _expr;

            public Initial(Expression expr) {
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
    }

    // (a, b, c, ...)
    // This isn't an expression, it's just a helper class for working with argument lists
    class ArgumentList {
        (string Name, Expression Expr)[] Expressions;
        public int Length => Expressions.Length;

        public ArgumentList(DMObject dmObject, DMProc proc, DMASTCallParameter[] arguments) {
            if (arguments == null) {
                Expressions = new (string, Expression)[0];
                return;
            }

            Expressions = new (string, Expression)[arguments.Length];

            int idx = 0;
            foreach(var arg in arguments) {
                var expr = Expression.Create(dmObject, proc, arg.Value);
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

            foreach ((string name, Expression expr) in Expressions) {
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

    class DMVisitorExpression : DMASTVisitor {
        DMObject _dmObject { get; }
        DMProc _proc { get; }
        DreamPath? _inferredPath { get; }
        internal Expression Result { get; private set; }

        internal DMVisitorExpression(DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
            _dmObject = dmObject;
            _proc = proc;
            _inferredPath = inferredPath;
        }

        public void VisitProcStatementExpression(DMASTProcStatementExpression statement) {
            statement.Expression.Visit(this);
        }


        public void VisitConstantNull(DMASTConstantNull constant) {
            Result = new Expressions.Null();
        }

        public void VisitConstantInteger(DMASTConstantInteger constant) {
            Result = new Expressions.Number(constant.Value);
        }

        public void VisitConstantFloat(DMASTConstantInteger constant) {
            Result = new Expressions.Number(constant.Value);
        }

        public void VisitConstantString(DMASTConstantString constant) {
            Result = new Expressions.String(constant.Value);
        }

        public void VisitConstantResource(DMASTConstantResource constant) {
            Result = new Expressions.Resource(constant.Path);
        }

        public void VisitConstantPath(DMASTConstantPath constant) {
            Result = new Expressions.Path(constant.Value.Path);
        }

        public void VisitStringFormat(DMASTStringFormat stringFormat) {
            var expressions = new Expression[stringFormat.InterpolatedValues.Length];

            for (int i = 0; i < stringFormat.InterpolatedValues.Length; i++) {
                expressions[i] = Expression.Create(_dmObject, _proc, stringFormat.InterpolatedValues[i]);
            }
            
            Result = new Expressions.StringFormat(stringFormat.Value, expressions);
        }


        public void VisitIdentifier(DMASTIdentifier identifier) {
            var name = identifier.Identifier;

            if (name == "src") {
                Result = new Expressions.Src(_dmObject.Path);
            } else if (name == "usr") {
                Result = new Expressions.Usr();
            } else if (name == "args") {
                Result = new Expressions.Args();
            } else {
                DMProc.DMLocalVariable localVar = _proc.GetLocalVariable(name);

                if (localVar != null) {
                    Result = new Expressions.Local(localVar.Type, name);
                    return;
                }

                var field = _dmObject.GetVariable(name);

                if (field == null) {
                    field = _dmObject.GetGlobalVariable(name);
                }

                if (field == null) {
                    throw new Exception($"unknown identifier {name}");
                }

                Result = new Expressions.Field(field.Type, name);
            }            
        }


        public void VisitCallableSelf(DMASTCallableSelf self) {
            Result = new Expressions.ProcSelf();
        }

        public void VisitCallableSuper(DMASTCallableSuper super) {
            Result = new Expressions.ProcSuper();
        }

        public void VisitCallableProcIdentifier(DMASTCallableProcIdentifier procIdentifier) {
            Result = new Expressions.Proc(procIdentifier.Identifier);
        }

        public void VisitProcCall(DMASTProcCall procCall) {
            // arglist hack
            if (procCall.Callable is DMASTCallableProcIdentifier ident) {
                if (ident.Identifier == "arglist") {
                    if (procCall.Parameters.Length != 1) throw new Exception("arglist must be the only argument");
                    if (procCall.Parameters.Length != 1) throw new Exception("arglist must have 1 argument");

                    var expr = Expression.Create(_dmObject, _proc, procCall.Parameters[0].Value);
                    Result = new Expressions.Arglist(expr);
                    return;
                }
            }

            var target = Expression.Create(_dmObject, _proc, procCall.Callable);
            var args = new ArgumentList(_dmObject, _proc, procCall.Parameters);
            Result = new Expressions.ProcCall(target, args);
        }

        public void VisitAssign(DMASTAssign assign) {
            var lhs = Expression.Create(_dmObject, _proc, assign.Expression);
            var rhs = Expression.Create(_dmObject, _proc, assign.Value, lhs.Path);
            Result = new Expressions.Assignment(lhs, rhs);
        }

        public void VisitNegate(DMASTNegate negate) {
            var expr = Expression.Create(_dmObject, _proc, negate.Expression);
            Result = new Expressions.Negate(expr);
        }

        public void VisitNot(DMASTNot not) {
            var expr = Expression.Create(_dmObject, _proc, not.Expression);
            Result = new Expressions.Not(expr);
        }

        public void VisitBinaryNot(DMASTBinaryNot binaryNot) {
            var expr = Expression.Create(_dmObject, _proc, binaryNot.Value);
            Result = new Expressions.BinaryNot(expr);
        }

        public void VisitAdd(DMASTAdd add) {
            var lhs = Expression.Create(_dmObject, _proc, add.A);
            var rhs = Expression.Create(_dmObject, _proc, add.B);
            Result = new Expressions.Add(lhs, rhs);
        }

        public void VisitSubtract(DMASTSubtract subtract) {
            var lhs = Expression.Create(_dmObject, _proc, subtract.A);
            var rhs = Expression.Create(_dmObject, _proc, subtract.B);
            Result = new Expressions.Add(lhs, rhs);
        }
        
        public void VisitMultiply(DMASTMultiply multiply) {
            var lhs = Expression.Create(_dmObject, _proc, multiply.A);
            var rhs = Expression.Create(_dmObject, _proc, multiply.B);
            Result = new Expressions.Multiply(lhs, rhs);
        }

        public void VisitDivide(DMASTDivide divide) {
            var lhs = Expression.Create(_dmObject, _proc, divide.A);
            var rhs = Expression.Create(_dmObject, _proc, divide.B);
            Result = new Expressions.Divide(lhs, rhs);
        }

        public void VisitModulus(DMASTModulus modulus) {
            var lhs = Expression.Create(_dmObject, _proc, modulus.A);
            var rhs = Expression.Create(_dmObject, _proc, modulus.B);
            Result = new Expressions.Modulo(lhs, rhs);
        }

        public void VisitPower(DMASTPower power) {
            var lhs = Expression.Create(_dmObject, _proc, power.A);
            var rhs = Expression.Create(_dmObject, _proc, power.B);
            Result = new Expressions.Power(lhs, rhs);
        }

        public void VisitAppend(DMASTAppend append) {
            var lhs = Expression.Create(_dmObject, _proc, append.A);
            var rhs = Expression.Create(_dmObject, _proc, append.B);
            Result = new Expressions.Append(lhs, rhs);
        }

        public void VisitCombine(DMASTCombine combine) {
            var lhs = Expression.Create(_dmObject, _proc, combine.A);
            var rhs = Expression.Create(_dmObject, _proc, combine.B);
            Result = new Expressions.Combine(lhs, rhs);
        }

        public void VisitRemove(DMASTRemove remove) {
            var lhs = Expression.Create(_dmObject, _proc, remove.A);
            var rhs = Expression.Create(_dmObject, _proc, remove.B);
            Result = new Expressions.Remove(lhs, rhs);
        }

        public void VisitMask(DMASTMask mask) {
            var lhs = Expression.Create(_dmObject, _proc, mask.A);
            var rhs = Expression.Create(_dmObject, _proc, mask.B);
            Result = new Expressions.Mask(lhs, rhs);
        }

        public void VisitMultiplyAssign(DMASTMultiplyAssign multiplyAssign) {
            var lhs = Expression.Create(_dmObject, _proc, multiplyAssign.A);
            var rhs = Expression.Create(_dmObject, _proc, multiplyAssign.B);
            Result = new Expressions.MultiplyAssign(lhs, rhs);
        }

        public void VisitDivideAssign(DMASTDivideAssign divideAssign) {
            var lhs = Expression.Create(_dmObject, _proc, divideAssign.A);
            var rhs = Expression.Create(_dmObject, _proc, divideAssign.B);
            Result = new Expressions.DivideAssign(lhs, rhs);
        }

        public void VisitLeftShiftAssign(DMASTLeftShiftAssign leftShiftAssign) {
            var lhs = Expression.Create(_dmObject, _proc, leftShiftAssign.A);
            var rhs = Expression.Create(_dmObject, _proc, leftShiftAssign.B);
            Result = new Expressions.LeftShiftAssign(lhs, rhs);
        }

        public void VisitRightShiftAssign(DMASTRightShiftAssign rightShiftAssign) {
            var lhs = Expression.Create(_dmObject, _proc, rightShiftAssign.A);
            var rhs = Expression.Create(_dmObject, _proc, rightShiftAssign.B);
            Result = new Expressions.RightShiftAssign(lhs, rhs);
        }

        public void VisitXorAssign(DMASTXorAssign xorAssign) {
            var lhs = Expression.Create(_dmObject, _proc, xorAssign.A);
            var rhs = Expression.Create(_dmObject, _proc, xorAssign.B);
            Result = new Expressions.XorAssign(lhs, rhs);
        }

        public void VisitLeftShift(DMASTLeftShift leftShift) {
            var lhs = Expression.Create(_dmObject, _proc, leftShift.A);
            var rhs = Expression.Create(_dmObject, _proc, leftShift.B);
            Result = new Expressions.LeftShift(lhs, rhs);
        }

        public void VisitRightShift(DMASTRightShift rightShift) {
            var lhs = Expression.Create(_dmObject, _proc, rightShift.A);
            var rhs = Expression.Create(_dmObject, _proc, rightShift.B);
            Result = new Expressions.RightShift(lhs, rhs);
        }

        public void VisitBinaryAnd(DMASTBinaryAnd binaryAnd) {
            var lhs = Expression.Create(_dmObject, _proc, binaryAnd.A);
            var rhs = Expression.Create(_dmObject, _proc, binaryAnd.B);
            Result = new Expressions.BinaryAnd(lhs, rhs);
        }

        public void VisitBinaryXor(DMASTBinaryXor binaryXor) {
            var lhs = Expression.Create(_dmObject, _proc, binaryXor.A);
            var rhs = Expression.Create(_dmObject, _proc, binaryXor.B);
            Result = new Expressions.BinaryXor(lhs, rhs);
        }

        public void VisitBinaryOr(DMASTBinaryOr binaryOr) {
            var lhs = Expression.Create(_dmObject, _proc, binaryOr.A);
            var rhs = Expression.Create(_dmObject, _proc, binaryOr.B);
            Result = new Expressions.BinaryOr(lhs, rhs);
        }

        public void VisitEqual(DMASTEqual equal) {
            var lhs = Expression.Create(_dmObject, _proc, equal.A);
            var rhs = Expression.Create(_dmObject, _proc, equal.B);
            Result = new Expressions.Equal(lhs, rhs);
        }

        public void VisitNotEqual(DMASTNotEqual notEqual) {
            var lhs = Expression.Create(_dmObject, _proc, notEqual.A);
            var rhs = Expression.Create(_dmObject, _proc, notEqual.B);
            Result = new Expressions.NotEqual(lhs, rhs);
        }
        
        public void VisitGreaterThan(DMASTGreaterThan greaterThan) {
            var lhs = Expression.Create(_dmObject, _proc, greaterThan.A);
            var rhs = Expression.Create(_dmObject, _proc, greaterThan.B);
            Result = new Expressions.GreaterThan(lhs, rhs);
        }

        public void VisitGreaterThanOrEqual(DMASTGreaterThanOrEqual greaterThanOrEqual) {
            var lhs = Expression.Create(_dmObject, _proc, greaterThanOrEqual.A);
            var rhs = Expression.Create(_dmObject, _proc, greaterThanOrEqual.B);
            Result = new Expressions.GreaterThanOrEqual(lhs, rhs);
        }

        public void VisitLessThan(DMASTLessThan lessThan) {
            var lhs = Expression.Create(_dmObject, _proc, lessThan.A);
            var rhs = Expression.Create(_dmObject, _proc, lessThan.B);
            Result = new Expressions.LessThan(lhs, rhs);
        }

        public void VisitLessThanOrEqual(DMASTLessThanOrEqual lessThanOrEqual) {
            var lhs = Expression.Create(_dmObject, _proc, lessThanOrEqual.A);
            var rhs = Expression.Create(_dmObject, _proc, lessThanOrEqual.B);
            Result = new Expressions.LessThanOrEqual(lhs, rhs);
        }

        public void VisitOr(DMASTOr or) {
            var lhs = Expression.Create(_dmObject, _proc, or.A);
            var rhs = Expression.Create(_dmObject, _proc, or.B);
            Result = new Expressions.Or(lhs, rhs);
        }

        public void VisitAnd(DMASTAnd and) {
            var lhs = Expression.Create(_dmObject, _proc, and.A);
            var rhs = Expression.Create(_dmObject, _proc, and.B);
            Result = new Expressions.And(lhs, rhs);
        }

        public void VisitTernary(DMASTTernary ternary) {
            var a = Expression.Create(_dmObject, _proc, ternary.A);
            var b = Expression.Create(_dmObject, _proc, ternary.B);
            var c = Expression.Create(_dmObject, _proc, ternary.C);
            Result = new Expressions.Ternary(a, b, c);
        }

        public void VisitListIndex(DMASTListIndex listIndex) {
            var expr = Expression.Create(_dmObject, _proc, listIndex.Expression);
            var index = Expression.Create(_dmObject, _proc, listIndex.Index);
            Result = new Expressions.ListIndex(expr, index);
        }

        public void VisitDereference(DMASTDereference dereference) {
            var expr = Expression.Create(_dmObject, _proc, dereference.Expression);
            Result = new Expressions.Dereference(expr, dereference, true);
        }

        public void VisitDereferenceProc(DMASTDereferenceProc dereferenceProc) {
            var expr = Expression.Create(_dmObject, _proc, dereferenceProc.Expression);
            Result = new Expressions.DereferenceProc(expr, dereferenceProc);
        }

        public void VisitNewPath(DMASTNewPath newPath) {
            var args = new ArgumentList(_dmObject, _proc, newPath.Parameters);
            Result = new Expressions.NewPath(newPath.Path.Path, args);
        }

        public void VisitNewInferred(DMASTNewInferred newInferred) {
            if (_inferredPath is null) {
                throw new Exception("An inferred new requires a type!");
            }

            var args = new ArgumentList(_dmObject, _proc, newInferred.Parameters);
            Result = new Expressions.NewPath(_inferredPath.Value, args);
        }

        public void VisitNewIdentifier(DMASTNewIdentifier newIdentifier) {
            var expr = Expression.Create(_dmObject, _proc, newIdentifier.Identifier);
            var args = new ArgumentList(_dmObject, _proc, newIdentifier.Parameters);
            Result = new Expressions.New(expr, args);
        }

        public void VisitNewDereference(DMASTNewDereference newDereference) {
            var expr = Expression.Create(_dmObject, _proc, newDereference.Dereference);
            var args = new ArgumentList(_dmObject, _proc, newDereference.Parameters);
            Result = new Expressions.New(expr, args);
        }

        public void VisitPreIncrement(DMASTPreIncrement preIncrement) {
            var expr = Expression.Create(_dmObject, _proc, preIncrement.Expression);
            Result = new Expressions.PreIncrement(expr);
        }

        public void VisitPostIncrement(DMASTPostIncrement postIncrement) {
            var expr = Expression.Create(_dmObject, _proc, postIncrement.Expression);
            Result = new Expressions.PostIncrement(expr);
        }

        public void VisitPreDecrement(DMASTPreDecrement preDecrement) {
            var expr = Expression.Create(_dmObject, _proc, preDecrement.Expression);
            Result = new Expressions.PreDecrement(expr);
        }

        public void VisitPostDecrement(DMASTPostDecrement postDecrement) {
            var expr = Expression.Create(_dmObject, _proc, postDecrement.Expression);
            Result = new Expressions.PostDecrement(expr);
        }

        public void VisitLocate(DMASTLocate locate) {
            var path = locate.Expression != null ? Expression.Create(_dmObject, _proc, locate.Expression) : null;
            var container = locate.Container != null ? Expression.Create(_dmObject, _proc, locate.Container) : null;
            Result = new Expressions.Locate(path, container);
        }

        public void VisitLocateCoordinates(DMASTLocateCoordinates locateCoordinates) {
            var _x = Expression.Create(_dmObject, _proc, locateCoordinates.X);
            var _y = Expression.Create(_dmObject, _proc, locateCoordinates.Y);
            var _z = Expression.Create(_dmObject, _proc, locateCoordinates.Z);
            Result = new Expressions.LocateCoordinates(_x, _y, _z);
        }

        public void VisitIsType(DMASTIsType isType) {
            var expr = Expression.Create(_dmObject, _proc, isType.Value);
            var path = Expression.Create(_dmObject, _proc, isType.Type);
            Result = new Expressions.IsType(expr, path);
        }

        public void VisitImplicitIsType(DMASTImplicitIsType isType) {
            var expr = Expression.Create(_dmObject, _proc, isType.Value);

            if (expr.Path is null) {
                throw new Exception("An inferred istype requires a type!");
            }

            Result = new Expressions.IsTypeInferred(expr, expr.Path.Value);
        }
        
        public void VisitList(DMASTList list) {
            Result = new Expressions.List(list);
        }

        public void VisitInput(DMASTInput input) {
            Result = new Expressions.Input(input);
        }

        public void VisitInitial(DMASTInitial initial) {
            var expr = Expression.Create(_dmObject, _proc, initial.Expression);
            Result = new Expressions.Initial(expr);
        }
    }
}
