using System.Diagnostics.CodeAnalysis;
using DMShared.Dream;
using DMShared.Dream.Procs;

namespace DMCompiler.DM {
    sealed class DMVariable {
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

        public bool TryAsJsonRepresentation([NotNullWhen(true)] out object? valueJson) {
            return Value.TryAsJsonRepresentation(out valueJson);
        }
    }
}
