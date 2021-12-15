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

        public bool TryAsJsonRepresentation(out object valueJson) {
            return Value.TryAsJsonRepresentation(out valueJson);
        }
    }
}
