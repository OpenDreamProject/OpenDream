using OpenDreamShared.Dream;

namespace DMCompiler.DM {
    class DMVariable {
        public DreamPath? Type;
        public string Name;
        public string InternalName;
        public bool IsGlobal;
        public bool IsConst;
        public Expressions.Constant JsonValue;
        public DMExpression InitialExpression;

        public DMVariable(DreamPath? type, string name, bool isGlobal, bool isConst = false) {
            Type = type;
            Name = name;
            InternalName = name;
            IsGlobal = isGlobal;
            IsConst = isConst;
        }

        public void Initialize(DMExpression expression) {
            InitialExpression = expression;
            InitialExpression.ConstValue = DMObjectTree.TryConstConvert(expression);
            if (InitialExpression.ConstValue != null) {
                JsonValue = InitialExpression.ConstValue;
            }
        }
    }
}
