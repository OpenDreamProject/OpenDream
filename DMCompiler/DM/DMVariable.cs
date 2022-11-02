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

        /// <summary>
        /// This is something necessary for <see cref="Expressions.Initial.TryAsConstant(out Expressions.Constant)"/> to work correctly. <br/>
        /// If a variable has a constant value, and is overridden with a new value, an initial() call is ambiguous, as it may refer to the old value or the new one, <br/>
        /// depending on whether the src caller is of the child type or the parent type.
        /// </summary>
        bool _wasOverriddenWithNewConstant = false;

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
            _wasOverriddenWithNewConstant = true;
            DMVariable clone = new DMVariable(Type, Name, IsGlobal, IsConst, ValType);
            clone.Value = value;
            return clone;
        }

        public bool SafeToTakeAsConstant() {
            return Value != null && (IsConst || !_wasOverriddenWithNewConstant);
        }

        public bool TryAsJsonRepresentation(out object valueJson) {
            return Value.TryAsJsonRepresentation(out valueJson);
        }
    }
}
