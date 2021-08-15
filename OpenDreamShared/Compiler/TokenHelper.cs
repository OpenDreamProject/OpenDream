namespace OpenDreamShared.Compiler {
    public partial class Token {
        public string ShortString() {
            string prefix = "";
            string inner = Text.Replace("\n", "\\n").Replace("\t", "\\t").Replace("\r", "\\r");
            switch (Type) {
                case TokenType.DM_Whitespace:
                case TokenType.DM_Preproc_Whitespace:
                case TokenType.DM_Preproc_Identifier:
                case TokenType.DM_Identifier: break;
                default: prefix = Type.ToString(); break;
            }
            return prefix + "(" + inner + ")";
        }
    }

}
