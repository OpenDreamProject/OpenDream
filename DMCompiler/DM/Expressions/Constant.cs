using OpenDreamShared.Compiler;
using OpenDreamShared.Dream;
using OpenDreamShared.Json;
using System;
using System.Collections.Generic;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace DMCompiler.DM.Expressions {
    abstract class Constant : DMExpression {
        public Constant(Location location) : base(location) { }

        public sealed override bool TryAsConstant(out Constant constant) {
            constant = this;
            return true;
        }

        public abstract bool IsTruthy();

        #region Unary Operations
        public Constant Not() {
            return new Number(Location, IsTruthy() ? 0 : 1);
        }

        public virtual Constant Negate() {
            throw new CompileErrorException(Location, $"const operation \"-{this}\" is invalid");
        }

        public virtual Constant BinaryNot() {
            throw new CompileErrorException(Location, $"const operation \"~{this}\" is invalid");
        }
        #endregion

        #region Binary Operations
        public Constant And(Constant rhs) {
            var truthy = IsTruthy() && rhs.IsTruthy();
            return new Number(Location, truthy ? 1 : 0);
        }

        public Constant Or(Constant rhs) {
            var truthy = IsTruthy() || rhs.IsTruthy();
            return new Number(Location, truthy ? 1 : 0);
        }

        public virtual Constant Add(Constant rhs) {
            throw new CompileErrorException(Location, $"const operation \"{this} + {rhs}\" is invalid");
        }

        public virtual Constant Subtract(Constant rhs) {
            throw new CompileErrorException(Location, $"const operation \"{this} - {rhs}\" is invalid");
        }

        public virtual Constant Multiply(Constant rhs) {
            throw new CompileErrorException(Location, $"const operation \"{this} * {rhs}\" is invalid");
        }

        public virtual Constant Divide(Constant rhs) {
            throw new CompileErrorException(Location, $"const operation \"{this} / {rhs}\" is invalid");
        }

        public virtual Constant Modulo(Constant rhs) {
            throw new CompileErrorException(Location, $"const operation \"{this} % {rhs}\" is invalid");
        }

        public virtual Constant Power(Constant rhs) {
            throw new CompileErrorException(Location, $"const operation \"{this} ** {rhs}\" is invalid");
        }

        public virtual Constant LeftShift(Constant rhs) {
            throw new CompileErrorException(Location, $"const operation \"{this} << {rhs}\" is invalid");
        }

        public virtual Constant RightShift(Constant rhs) {
            throw new CompileErrorException(Location, $"const operation \"{this} >> {rhs}\" is invalid");
        }

        public virtual Constant BinaryAnd(Constant rhs) {
            throw new CompileErrorException(Location, $"const operation \"{this} & {rhs}\" is invalid");
        }

        public virtual Constant BinaryXor(Constant rhs) {
            throw new CompileErrorException(Location, $"const operation \"{this} ^ {rhs}\" is invalid");
        }

        public virtual Constant BinaryOr(Constant rhs) {
            throw new CompileErrorException(Location, $"const operation \"{this} | {rhs}\" is invalid");
        }

        public virtual Constant Equals(Constant rhs) {
            throw new CompileErrorException(Location, $"const operation \"{this} == {rhs}\" is invalid");
        }

        public virtual Constant NotEquals(Constant rhs) {
            throw new CompileErrorException(Location, $"const operation \"{this} != {rhs}\" is invalid");
        }
        #endregion
    }

    // null
    class Null : Constant {
        public Null(Location location) : base(location) { }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            proc.PushNull();
        }

        public override bool IsTruthy() => false;

        public override bool TryAsJsonRepresentation(out object json) {
            json = null;
            return true;
        }

        public override Constant Equals(Constant rhs) {
            if (rhs is not Null) {
                return new Number(Location, 0); // false
            }

            return new Number(Location, 1); // true
        }

        public override Constant NotEquals(Constant rhs) {
            if (rhs is not Null) {
                return new Number(Location, 1); // true
            }

            return new Number(Location, 0); // false
        }
    }

    // 4.0, -4.0
    class Number : Constant {
        public float Value { get; }

        public Number(Location location, int value) : base(location) {
            Value = value;
        }

        public Number(Location location, float value) : base(location) {
            Value = value;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            proc.PushFloat(Value);
        }

        public override bool IsTruthy() => Value != 0;

        public override bool TryAsJsonRepresentation(out object json) {
            json = Value;
            return true;
        }

        public override Constant Negate() {
            return new Number(Location, -Value);
        }

        public override Constant BinaryNot() {
            return new Number(Location, ~(int)Value);
        }

        public override Constant Add(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.Add(rhs);
            }

            return new Number(Location, Value + rhsNum.Value);
        }

        public override Constant Subtract(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.Add(rhs);
            }

            return new Number(Location, Value - rhsNum.Value);
        }

        public override Constant Multiply(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.Add(rhs);
            }

            return new Number(Location, Value * rhsNum.Value);
        }

        public override Constant Divide(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.Add(rhs);
            }

            return new Number(Location, Value / rhsNum.Value);
        }

        public override Constant Modulo(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.Add(rhs);
            }

            return new Number(Location, Value % rhsNum.Value);
        }

        public override Constant Power(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.Add(rhs);
            }

            return new Number(Location, MathF.Pow(Value, rhsNum.Value));
        }

        public override Constant LeftShift(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.Add(rhs);
            }

            return new Number(Location, ((int)Value) << ((int)rhsNum.Value));
        }

        public override Constant RightShift(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.Add(rhs);
            }

            return new Number(Location, ((int)Value) >> ((int)rhsNum.Value));
        }


        public override Constant BinaryAnd(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.Add(rhs);
            }

            return new Number(Location, ((int)Value) & ((int)rhsNum.Value));
        }


        public override Constant BinaryXor(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.Add(rhs);
            }

            return new Number(Location, ((int)Value) ^ ((int)rhsNum.Value));
        }


        public override Constant BinaryOr(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.Add(rhs);
            }

            return new Number(Location, ((int)Value) | ((int)rhsNum.Value));
        }

        public override Constant Equals(Constant rhs) {
            if (rhs is not Number rhsNum || MathHelper.CloseTo(rhsNum.Value, Value)) {
                return new Number(Location, 0); // false
            }

            return new Number(Location, 1); // true
        }

        public override Constant NotEquals(Constant rhs) {
            if (rhs is not Number rhsNum || !MathHelper.CloseTo(rhsNum.Value, Value)) {
                return new Number(Location, 1); // true
            }

            return new Number(Location, 0); // false
        }
    }

    // "abc"
    class String : Constant {
        public string Value { get; }

        public String(Location location, string value) : base(location) {
            Value = value;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            proc.PushString(Value);
        }

        public override bool IsTruthy() => Value.Length != 0;

        public override bool TryAsJsonRepresentation(out object json) {
            json = Value;
            return true;
        }

        public override Constant Add(Constant rhs) {
            if (rhs is not String rhsString) {
                return base.Add(rhs);
            }

            return new String(Location, Value + rhsString.Value);
        }

        public override Constant Equals(Constant rhs) {
            if (rhs is not String rhsString || rhsString.Value != Value) {
                return new Number(Location, 0); // false
            }

            return new Number(Location, 1); // true
        }

        public override Constant NotEquals(Constant rhs) {
            if (rhs is not String rhsString || rhsString.Value != Value) {
                return new Number(Location, 1); // true
            }

            return new Number(Location, 0); // false
        }
    }

    // 'abc'
    class Resource : Constant {
        string Value { get; }

        public Resource(Location location, string value) : base(location) {
            Value = value;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            proc.PushResource(Value);
        }

        public override bool IsTruthy() => true;

        public override bool TryAsJsonRepresentation(out object json) {
            json = new Dictionary<string, object>() {
                { "type", JsonVariableType.Resource },
                { "resourcePath", Value }
            };

            return true;
        }

        public override Constant Equals(Constant rhs) {
            if (rhs is not Resource rhsResource || rhsResource.Value != Value) {
                return new Number(Location, 0); // false
            }

            return new Number(Location, 1); // true
        }

        public override Constant NotEquals(Constant rhs) {
            if (rhs is not Resource rhsResource || rhsResource.Value != Value) {
                return new Number(Location, 1); // true
            }

            return new Number(Location, 0); // false
        }
    }

    // /a/b/c
    class Path : Constant {
        public DreamPath Value { get; }

        public Path(Location location, DreamPath value) : base(location) {
            Value = value;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            proc.PushPath(Value);
        }

        public override bool IsTruthy() => true;

        public override bool TryAsJsonRepresentation(out object json) {
            object value;

            if (DMObjectTree.TryGetTypeId(Value, out int typeId)) {
                value = typeId;
            } else {
                value = Value.PathString;
            }

            json = new Dictionary<string, object>() {
                { "type", JsonVariableType.Path },
                { "value", value }
            };

            return true;
        }

        public override Constant Equals(Constant rhs) {
            if (rhs is not Path rhsPath || rhsPath.Value != Value) {
                return new Number(Location, 0); // false
            }

            return new Number(Location, 1); // true
        }

        public override Constant NotEquals(Constant rhs) {
            if (rhs is not Path rhsPath || rhsPath.Value != Value) {
                return new Number(Location, 1); // true
            }

            return new Number(Location, 0); // false
        }
    }
}
