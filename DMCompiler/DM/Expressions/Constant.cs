using OpenDreamShared.Compiler;
using OpenDreamShared.Dream;
using System;

namespace DMCompiler.DM.Expressions {
    abstract class Constant : DMExpression {
        public sealed override Constant ToConstant() {
            return this;
        }

        public abstract bool IsTruthy();

#region Unary Operations
        public Constant Not() {
            return new Number(IsTruthy() ? 0 : 1);
        }

        public virtual Constant Negate() {
            throw new CompileErrorException($"const operation `-{this}` is invalid");
        }

        public virtual Constant BinaryNot() {
            throw new CompileErrorException($"const operation `~{this}` is invalid");
        }
#endregion

#region Binary Operations
        public Constant And(Constant rhs) {
            var truthy = IsTruthy() && rhs.IsTruthy();
            return new Number(truthy ? 1 : 0);
        }

        public Constant Or(Constant rhs) {
            var truthy = IsTruthy() || rhs.IsTruthy();
            return new Number(truthy ? 1 : 0);
        }

        public virtual Constant Add(Constant rhs) {
            throw new CompileErrorException($"const operation `{this} + {rhs}` is invalid");
        }

        public virtual Constant Subtract(Constant rhs) {
            throw new CompileErrorException($"const operation `{this} - {rhs}` is invalid");
        }

        public virtual Constant Multiply(Constant rhs) {
            throw new CompileErrorException($"const operation `{this} * {rhs}` is invalid");
        }

        public virtual Constant Divide(Constant rhs) {
            throw new CompileErrorException($"const operation `{this} / {rhs}` is invalid");
        }

        public virtual Constant Modulo(Constant rhs) {
            throw new CompileErrorException($"const operation `{this} % {rhs}` is invalid");
        }

        public virtual Constant Power(Constant rhs) {
            throw new CompileErrorException($"const operation `{this} ** {rhs}` is invalid");
        }

        public virtual Constant LeftShift(Constant rhs) {
            throw new CompileErrorException($"const operation `{this} << {rhs}` is invalid");
        }

        public virtual Constant RightShift(Constant rhs) {
            throw new CompileErrorException($"const operation `{this} >> {rhs}` is invalid");
        }

        public virtual Constant BinaryAnd(Constant rhs) {
            throw new CompileErrorException($"const operation `{this} & {rhs}` is invalid");
        }

        public virtual Constant BinaryXor(Constant rhs) {
            throw new CompileErrorException($"const operation `{this} ^ {rhs}` is invalid");
        }

        public virtual Constant BinaryOr(Constant rhs) {
            throw new CompileErrorException($"const operation `{this} | {rhs}` is invalid");
        }
#endregion
    }

    // null
    class Null : Constant {
        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            proc.PushNull();
        }

        public override bool IsTruthy() => false;
    }

    // 4.0, -4.0
    class Number : Constant {
        public float Value { get; }

        public Number(int value) {
            Value = value;
        }

        public Number(float value) {
            Value = value;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            proc.PushFloat(Value);
        }

        public override bool IsTruthy() => Value != 0;

        public override Constant Negate() {
            return new Number(-Value);
        }

        public override Constant BinaryNot() {
            return new Number(~(int) Value);
        }

        public override Constant Add(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.Add(rhs);
            }

            return new Number(Value + rhsNum.Value);
        }

        public override Constant Subtract(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.Add(rhs);
            }

            return new Number(Value - rhsNum.Value);
        }

        public override Constant Multiply(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.Add(rhs);
            }

            return new Number(Value * rhsNum.Value);
        }

        public override Constant Divide(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.Add(rhs);
            }

            return new Number(Value / rhsNum.Value);
        }

        public override Constant Modulo(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.Add(rhs);
            }

            return new Number(Value % rhsNum.Value);
        }

        public override Constant Power(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.Add(rhs);
            }

            return new Number(MathF.Pow(Value, rhsNum.Value));
        }

        public override Constant LeftShift(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.Add(rhs);
            }

            return new Number(((int) Value) << ((int) rhsNum.Value));
        }

        public override Constant RightShift(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.Add(rhs);
            }

            return new Number(((int) Value) >> ((int) rhsNum.Value));
        }


        public override Constant BinaryAnd(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.Add(rhs);
            }

            return new Number(((int) Value) & ((int) rhsNum.Value));
        }


        public override Constant BinaryXor(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.Add(rhs);
            }

            return new Number(((int) Value) ^ ((int) rhsNum.Value));
        }


        public override Constant BinaryOr(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.Add(rhs);
            }

            return new Number(((int) Value) | ((int) rhsNum.Value));
        }
    }

    // "abc"
    class String : Constant {
        public string Value { get; }

        public String(string value) {
            Value = value;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            proc.PushString(Value);
        }

        public override bool IsTruthy() => Value.Length != 0;

        public override Constant Add(Constant rhs) {
            if (rhs is not String rhsString) {
                return base.Add(rhs);
            }

            return new String(Value + rhsString.Value);
        }
    }

    // 'abc'
    class Resource : Constant {
        public string Value { get; }

        public Resource(string value) {
            Value = value;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            proc.PushResource(Value);
        }

        public override bool IsTruthy() => true;
    }

    // /a/b/c
    class Path : Constant {
        public DreamPath Value { get; }

        public Path(DreamPath value) {
            Value = value;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            proc.PushPath(Value);
        }

        public override bool IsTruthy() => true;
    }
}
