using OpenDreamShared.Dream;

namespace DMCompiler.DM {
    class DMVariable {
        public DreamPath? Type;
        public object Value;

        public DMVariable(DreamPath? type, object value) {
            Type = type;
            Value = value;
        }
    }
}
