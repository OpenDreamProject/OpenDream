using OpenDreamShared.Dream;

namespace DMCompiler.DM {
    class DMVariable {
        public DreamPath? Type;
        public string Name;
        public bool IsGlobal;
        public bool IsConst;
        public DMExpression Value;

        public DMVariable(DreamPath? type, string name, bool isGlobal, bool isConst) {
            Type = type;
            Name = name;
            IsGlobal = isGlobal;
            IsConst = isConst;
            Value = null;
        }


        /// <summary>
        /// This is a copy-on-write proc used to set the DMVariable to a constant value. <br/>
        /// In some contexts, doing so would clobber pre-existing constants, <br/>
        /// and so this sometimes creates a copy of <see langword="this"/>, with the new constant value.
        /// </summary>
        public DMVariable WriteToValue(Expressions.Constant value)
        {
            if(Value == null)
            {
                Value = value;
                return this;
            }
            DMVariable clone = new DMVariable(Type,Name,IsGlobal,IsConst);
            clone.Value = value;
            return clone;
        }

        public bool TryAsJsonRepresentation(out object valueJson) {
            return Value.TryAsJsonRepresentation(out valueJson);
        }
    }
}
