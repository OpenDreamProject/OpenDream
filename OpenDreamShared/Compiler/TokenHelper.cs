using System.Collections.Generic;
using System.Text;
using static OpenDreamShared.Compiler.TokenType;

namespace OpenDreamShared.Compiler {

    public static partial class TokenStatic {
        static public string ToLongString(this IEnumerable<Token> tokens, List<Token> highlight) {
            int tokensperline = 8;
            int i = 0;
            bool eol = false;
            StringBuilder sb = new();
            foreach (var token in tokens) {
                if (i != 0 && i % tokensperline == 0) { eol = true; }
                if (eol) {
                    sb.Append('\n');
                }
                string tokentext = "";
                if (highlight.Contains(token)) {
                    if (!eol) {
                        tokentext = "\n";
                    }
                    tokentext += "%%% " + token.ToShortString() + " %%%";
                    i = tokensperline - 1;
                }
                else {
                    tokentext = token.ToShortString();
                }
                sb.Append(string.Format("{0,-20}", tokentext));
                if (tokentext.Length > 20) {
                    i = tokensperline - 1;
                }
                if (eol) { eol = false; }
                i += 1;
            }
            return sb.ToString();
        }
    }
    public partial class Token {
        public string ToShortString() {
            string inner = Text.Replace("\n", "\\n").Replace("\t", "\\t").Replace("\r", "\\r");

            if (!OperatorTokenTypes.Contains(Type)) {
                string prefix = Type.ToString();
                switch (Type) {
                    case DM_Identifier:
                    case DM_String:
                    case DM_Whitespace: prefix = ""; break;
                }
                return prefix + "(" + inner + ")";
            }
            else {
                return "'" + Text + "'";
            }
        }
        public static HashSet<TokenType> OperatorTokenTypes = new() {
            DM_And,
            DM_AndAnd,
            DM_AndEquals,
            DM_Bar,
            DM_BarBar,
            DM_BarEquals,
            DM_Colon,
            DM_Comma,
            DM_Equals,
            DM_EqualsEquals,
            DM_Exclamation,
            DM_ExclamationEquals,
            DM_GreaterThan,
            DM_GreaterThanEquals,
            DM_In,
            DM_RightShift,
            DM_RightShiftEquals,
            DM_LeftBracket,
            DM_LeftCurlyBracket,
            DM_LeftParenthesis,
            DM_LeftShift,
            DM_LeftShiftEquals,
            DM_LessThan,
            DM_LessThanEquals,
            DM_Minus,
            DM_MinusEquals,
            DM_MinusMinus,
            DM_Modulus,
            DM_ModulusEquals,
            DM_Period,
            DM_Plus,
            DM_PlusEquals,
            DM_PlusPlus,
            DM_Question,
            DM_QuestionColon,
            DM_QuestionPeriod,
            DM_RightBracket,
            DM_RightCurlyBracket,
            DM_RightParenthesis,
            DM_Semicolon,
            DM_Slash,
            DM_SlashEquals,
            DM_Star,
            DM_StarEquals,
            DM_StarStar,
            DM_Tilde,
            DM_TildeEquals,
            DM_TildeExclamation,
            DM_To,
            DM_Xor,
            DM_XorEquals
        };
    }

}
