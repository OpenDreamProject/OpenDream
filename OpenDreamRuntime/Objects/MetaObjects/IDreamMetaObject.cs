using OpenDreamRuntime.Procs;

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

        public void OperatorOutput(DreamObject a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot output {b} to {a}");

            ParentType.OperatorOutput(a, b);
        }

        public DreamValue OperatorAdd(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Addition cannot be done between {a} and {b}");

            return ParentType.OperatorAdd(a, b);
        }
        //AKA AddRef
        public DreamValue OperatorAppend(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot append {b} to {a}");

            return ParentType.OperatorAppend(a, b);
        }

        public DreamValue OperatorSubtract(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Subtraction cannot be done between {a} and {b}");

            return ParentType.OperatorSubtract(a, b);
        }

        //AKA SubtractRef
        public DreamValue OperatorRemove(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot remove {b} from {a}");

            return ParentType.OperatorRemove(a, b);
        }

        public DreamValue OperatorIncrement(DreamValue a) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot increment {a}");

            return ParentType.OperatorIncrement(a);
        }

        public DreamValue OperatorDecrement(DreamValue a) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot decrement {a}");

            return ParentType.OperatorDecrement(a);
        }

        public DreamValue OperatorMultiply(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Multiplication cannot be done between {a} and {b}");

            return ParentType.OperatorMultiply(a, b);
        }

        public DreamValue OperatorMultiplyRef(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Multiplication cannot be done between {a} and {b}");

            return ParentType.OperatorMultiplyRef(a, b);
        }

        public DreamValue OperatorIndex(DreamObject a, DreamValue index) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot index {a}");

            return ParentType.OperatorIndex(a, index);
        }

        public void OperatorIndexAssign(DreamObject a, DreamValue index, DreamValue value) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot assign {value} to index {index} of {a}");

            ParentType.OperatorIndexAssign(a, index, value);
        }

        public DreamValue OperatorBitAnd(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot bit-and {a} and {b}");

            return ParentType.OperatorBitAnd(a, b);
        }
        //AKA AndRef
        public DreamValue OperatorMask(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot mask {a} and {b}");

            return ParentType.OperatorMask(a, b);
        }

        public DreamValue OperatorBitNot(DreamValue a) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot bit-not {a}");

            return ParentType.OperatorBitNot(a);
        }

        public DreamValue OperatorBitOr(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot bit-or {a} and {b}");

            return ParentType.OperatorBitOr(a, b);
        }

        //AKA OrRef
        public DreamValue OperatorCombine(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot combine {a} and {b}");

            return ParentType.OperatorCombine(a, b);
        }

        public DreamValue OperatorBitShiftLeft(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot bit-shift-left {a} by {b}");

            return ParentType.OperatorBitShiftLeft(a, b);
        }

        public DreamValue OperatorBitShiftLeftRef(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot bit-shift-left {a} by {b}");

            return ParentType.OperatorBitShiftLeftRef(a, b);
        }
        public DreamValue OperatorBitShiftRight(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot bit-shift-right {a} by {b}");

            return ParentType.OperatorBitShiftRight(a, b);
        }

        public DreamValue OperatorBitShiftRightRef(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot bit-shift-right {a} by {b}");

            return ParentType.OperatorBitShiftRightRef(a, b);
        }

        public DreamValue OperatorBitXor(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot bit-xor {a} and {b}");

            return ParentType.OperatorBitXor(a, b);
        }

        public DreamValue OperatorBitXorRef(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot bit-xor {a} and {b}");

            return ParentType.OperatorBitXorRef(a, b);
        }

        public DreamValue OperatorDivide(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot divide {a} by {b}");

            return ParentType.OperatorDivide(a, b);
        }

        public DreamValue OperatorDivideRef(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot divide {a} by {b}");

            return ParentType.OperatorDivideRef(a, b);
        }

        public DreamValue OperatorModulus(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot modulo {a} by {b}");

            return ParentType.OperatorModulus(a, b);
        }

        public DreamValue OperatorModulusRef(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot modulo {a} by {b}");

            return ParentType.OperatorModulusRef(a, b);
        }

        public DreamValue OperatorModulusModulus(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot modulo-modulo {a} by {b}");

            return ParentType.OperatorModulusModulus(a, b);
        }

        public DreamValue OperatorModulusModulusRef(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot modulo-modulo {a} by {b}");

            return ParentType.OperatorModulusModulusRef(a, b);
        }
        public DreamValue OperatorNegate(DreamValue a) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot negate {a}");

            return ParentType.OperatorNegate(a);
        }
        public DreamValue OperatorPower(DreamValue a, DreamValue b) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot raise {a} to the power of {b}");

            return ParentType.OperatorPower(a, b);
        }


        //comparators
        public DreamValue OperatorEquivalent(DreamValue a, DreamValue b) {
            if (ParentType == null){
                return (a.Equals(b) ? DreamValue.True : DreamValue.False);
            }

            return ParentType.OperatorEquivalent(a, b);
        }

        public DreamValue OperatorNotEquivalent(DreamValue a, DreamValue b) { //TODO : find a way to check if OperatorEquivalent is implemented and use the negation of that
            if (ParentType == null){
                return (a.Equals(b) ? DreamValue.False : DreamValue.True);
            }

            return ParentType.OperatorEquivalent(a, b);
        }

        public DreamValue OperatorLessThan(DreamValue a, DreamValue b) {
            if (ParentType == null){
                return a; //yes this is actually the byond behaviour. I know.
            }

            return ParentType.OperatorLessThan(a, b);
        }
        public DreamValue OperatorLessThanOrEquals(DreamValue a, DreamValue b) {
            if (ParentType == null){
                return a; //yes this is actually the byond behaviour. I know.
            }

            return ParentType.OperatorLessThanOrEquals(a, b);
        }

        public DreamValue OperatorGreaterThan(DreamValue a, DreamValue b) {
            if (ParentType == null){
                return a; //yes this is actually the byond behaviour. I know.
            }

            return ParentType.OperatorGreaterThan(a, b);
        }
        public DreamValue OperatorGreaterThanOrEquals(DreamValue a, DreamValue b) {
            if (ParentType == null){
                return a; //yes this is actually the byond behaviour. I know.
            }

            return ParentType.OperatorGreaterThanOrEquals(a, b);
        }

    }
}
