using OpenDreamShared.Dream;

namespace DMCompiler.DM {
    class DMVariable {
        public DreamPath? Type;
        public string Name;
        public string InternalName;
        public bool IsGlobal;
        public DMExpression Value;

        public DMVariable(DreamPath? type, string name, bool isGlobal) {
            Type = type;
            Name = name;
            InternalName = name;
            IsGlobal = isGlobal;
            Value = null;
        }
    }
}
