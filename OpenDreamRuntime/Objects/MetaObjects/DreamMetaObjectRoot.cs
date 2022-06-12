using OpenDreamRuntime.Procs;

namespace OpenDreamRuntime.Objects.MetaObjects {
    [Virtual]
    class DreamMetaObjectRoot : IDreamMetaObject {
        public virtual bool ShouldCallNew => false;

        public virtual void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {

        }

        public virtual void OnObjectDeleted(DreamObject dreamObject) {

        }

        public virtual void OnVariableSet(DreamObject dreamObject, string variableName, DreamValue variableValue, DreamValue oldVariableValue) {

        }

        public virtual DreamValue OnVariableGet(DreamObject dreamObject, string variableName, DreamValue variableValue) {
            return variableValue;
        }

        public virtual DreamValue OperatorOutput(DreamValue a, DreamValue b) {
            throw new InvalidOperationException("Cannot output " + b + " to " + a);
        }

        public virtual DreamValue OperatorAdd(DreamValue a, DreamValue b) {
            throw new InvalidOperationException("Addition cannot be done between " + a + " and " + b);
        }

        public virtual DreamValue OperatorSubtract(DreamValue a, DreamValue b) {
            throw new InvalidOperationException("Subtraction cannot be done between " + a + " and " + b);
        }

        public virtual DreamValue OperatorMultiply(DreamValue a, DreamValue b) {
            throw new InvalidOperationException("Multiplication cannot be done between " + a + " and " + b);
        }

        public virtual DreamValue OperatorAppend(DreamValue a, DreamValue b) {
            throw new InvalidOperationException("Cannot append " + b + " to " + a);
        }

        public virtual DreamValue OperatorRemove(DreamValue a, DreamValue b) {
            throw new InvalidOperationException("Cannot remove " + b + " from " + a);
        }
        public virtual DreamValue OperatorOr(DreamValue a, DreamValue b) {
            throw new InvalidOperationException("Cannot or " + a + " and " + b);
        }

        public virtual DreamValue OperatorCombine(DreamValue a, DreamValue b) {
            throw new InvalidOperationException("Cannot combine " + a + " and " + b);
        }

        public virtual DreamValue OperatorMask(DreamValue a, DreamValue b) {
            throw new InvalidOperationException("Cannot mask " + a + " and " + b);
        }

        public virtual DreamValue OperatorIndex(DreamObject dreamObject, DreamValue index) {
            throw new InvalidOperationException("Cannot index " + dreamObject);
        }

        public virtual void OperatorIndexAssign(DreamObject dreamObject, DreamValue index, DreamValue value) {
            throw new InvalidOperationException("Cannot assign " + value + " to index " + index + " of " + dreamObject);
        }
    }
}
