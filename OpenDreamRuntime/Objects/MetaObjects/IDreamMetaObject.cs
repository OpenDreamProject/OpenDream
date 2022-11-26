﻿using OpenDreamRuntime.Procs;

namespace OpenDreamRuntime.Objects.MetaObjects {
    public interface IDreamMetaObject {
        public bool ShouldCallNew { get; }
        public IDreamMetaObject? ParentType { get; set; }

        public void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) =>
            ParentType?.OnObjectCreated(dreamObject, creationArguments);

        public void OnObjectDeleted(DreamObject dreamObject) =>
            ParentType?.OnObjectDeleted(dreamObject);

        public void OnVariableSet(DreamObject dreamObject, string varName, DreamValue value, DreamValue oldValue) =>
            ParentType?.OnVariableSet(dreamObject, varName, value, oldValue);

        public DreamValue OnVariableGet(DreamObject dreamObject, string varName, DreamValue value) =>
            ParentType?.OnVariableGet(dreamObject, varName, value) ?? value;

        public void OperatorOutput(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot output {b} to {a}");

            ParentType.OperatorOutput(a, b);
        }

        public DreamValue OperatorAdd(DreamValue a, DreamValue b) {
            if (ParentType == null)
                if(a.TryGetValueAsDreamObject(out DreamObject obj) && obj.TryGetProc("operator+", out DreamProc overload))
                {

                   // DreamProc.Call(overload, obj, new DreamProcArguments(new List<DreamValue>(){b}));
                }
                else
                    throw new InvalidOperationException($"Addition cannot be done between {a} and {b}");

            return ParentType.OperatorAdd(a, b);
        }

        public DreamValue OperatorSubtract(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Subtraction cannot be done between {a} and {b}");

            return ParentType.OperatorSubtract(a, b);
        }

        public DreamValue OperatorMultiply(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Multiplication cannot be done between {a} and {b}");

            return ParentType.OperatorMultiply(a, b);
        }

        public DreamValue OperatorAppend(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot append {b} to {a}");

            return ParentType.OperatorAppend(a, b);
        }

        public DreamValue OperatorRemove(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot remove {b} from {a}");

            return ParentType.OperatorRemove(a, b);
        }

        public DreamValue OperatorOr(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot or {a} and {b}");

            return ParentType.OperatorOr(a, b);
        }

        public DreamValue OperatorEquivalent(DreamValue a, DreamValue b) {
            if (ParentType == null)
                return a.Equals(b) ? new DreamValue(1f) : new DreamValue(0f);

            return ParentType.OperatorEquivalent(a, b);
        }

        public DreamValue OperatorCombine(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot combine {a} and {b}");

            return ParentType.OperatorCombine(a, b);
        }

        public DreamValue OperatorMask(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot mask {a} and {b}");

            return ParentType.OperatorMask(a, b);
        }

        public DreamValue OperatorIndex(DreamObject dreamObject, DreamValue index) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot index {dreamObject}");

            return ParentType.OperatorIndex(dreamObject, index);
        }

        public void OperatorIndexAssign(DreamObject dreamObject, DreamValue index, DreamValue value) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot assign {value} to index {index} of {dreamObject}");

            ParentType.OperatorIndexAssign(dreamObject, index, value);
        }

        public void OperatorBitAnd(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot bit-and {a} and {b}");

            ParentType.OperatorBitAnd(a, b);
        }

        public void OperatorBitNot(DreamValue a) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot bit-not {a}");

            ParentType.OperatorBitNot(a);
        }

        public void OperatorBitOr(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot bit-or {a} and {b}");

            ParentType.OperatorBitOr(a, b);
        }

        public void OperatorBitShiftLeft(DreamValue a) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot bit-shift-left {a}");

            ParentType.OperatorBitShiftLeft(a);
        }
        public void OperatorBitShiftRight(DreamValue a) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot bit-shift-right {a}");

            ParentType.OperatorBitShiftRight(a);
        }

        public void OperatorBitXor(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot bit-xor {a} and {b}");

            ParentType.OperatorBitXor(a, b);
        }
        public void OperatorDivide(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot divide {a} and {b}");

            ParentType.OperatorDivide(a, b);
        }
        public void OperatorModulus(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot modulo {a} and {b}");

            ParentType.OperatorModulus(a, b);
        }
        public void OperatorModulusModulus(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot modulo-modulo {a} and {b}");

            ParentType.OperatorModulusModulus(a, b);
        }
        public void OperatorNegate(DreamValue a) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot negate {a}");

            ParentType.OperatorNegate(a);
        }
        public void OperatorPower(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot raise {a} to the power of {b}");

            ParentType.OperatorPower(a, b);
        }


    }
}
