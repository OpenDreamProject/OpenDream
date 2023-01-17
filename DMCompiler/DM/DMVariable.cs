using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;

namespace DMCompiler.DM {
    class DMVariable {
        public DreamPath? Type;
        public string Name;
        public bool IsGlobal;
        /// <remarks>
        /// NOTE: This DMVariable may be forced constant through opendream_compiletimereadonly. This only marks that the variable has the DM quality of /const/ness.
        /// </remarks>
        public bool IsConst;
        public DMExpression Value;
        public DMValueType ValType;

        public DMVariable(DreamPath? type, string name, bool isGlobal, bool isConst, DMValueType valType = DMValueType.Anything) {
            Type = type;
            Name = name;
            IsGlobal = isGlobal;
            IsConst = isConst;
            Value = null;
            ValType = valType;
        }

        /// <summary>
        /// This is a copy-on-write proc used to set the DMVariable to a constant value. <br/>
        /// In some contexts, doing so would clobber pre-existing constants, <br/>
        /// and so this sometimes creates a copy of <see langword="this"/>, with the new constant value.
        /// </summary>
        public DMVariable WriteToValue(Expressions.Constant value) {
            if (Value == null) {
                Value = value;
                return this;
            }
            DMVariable clone = new DMVariable(Type, Name, IsGlobal, IsConst, ValType);
            clone.Value = value;
            return clone;
        }

        /// <remarks>
        /// Most variables are not safe to take as a constant-value, even in initial(), <br/>
        /// because we may be arriving here from a member access that doesn't actually access this DMVariable. <br/>
        /// Instead, it may be accessing (through duck-typing) a member someplace here.
        /// Check the wording of the ref and read it very carefully: https://www.byond.com/docs/ref/#/operator/%2e
        /// </remarks>
        public bool SafeToTakeAsConstant() {
            //TODO: Support optional optimizations here whenever ducktyping is disabled.
            return Value != null && IsConst;
        }

        public bool TryAsJsonRepresentation(out object valueJson) {
            return Value.TryAsJsonRepresentation(out valueJson);
        }
    }
}
