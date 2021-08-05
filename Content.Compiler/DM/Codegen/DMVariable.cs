using Content.Shared.Dream;

namespace Content.Compiler.DM
{
    public class DMVariable {
        public DreamPath? Type;
        public string Name;
        public bool IsGlobal;
        public DMExpression Value;

        public DMVariable(DreamPath? type, string name, bool isGlobal) {
            Type = type;
            Name = name;
            IsGlobal = isGlobal;
            Value = null;
        }
    }
}
