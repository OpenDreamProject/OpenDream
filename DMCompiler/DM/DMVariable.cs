using OpenDreamShared.Dream;

namespace DMCompiler.DM {
    class DMVariable {
        public DreamPath? Type;
        public string Name;
        public string InternalName;
        public bool IsGlobal;
        public bool IsConst;
        public DMExpression Value;

        public DMVariable(DreamPath? type, string name, bool isGlobal, bool isConst = false) {
            Type = type;
            Name = name;
            InternalName = name;
            IsGlobal = isGlobal;
            IsConst = isConst;
            Value = null;
        }
    }
}
