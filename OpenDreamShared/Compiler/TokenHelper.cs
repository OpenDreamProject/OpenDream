using System.Collections.Generic;
using System.Text;
using OpenDreamShared.Compiler;

namespace OpenDreamShared.Compiler {
    public static partial class TokenHelper {
        static public string ToLongString(this IEnumerable<Token> tokens, List<Token> highlight = null) {
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
                if (highlight != null && highlight.Contains(token)) {
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
                    case TokenType.DM_Identifier:
                    case TokenType.DM_String:
                    case TokenType.DM_Whitespace: prefix = ""; break;
                }
                return prefix + "(" + inner + ")";
            }
            else {
                return "'" + Text + "'";
            }
        }
        public static HashSet<TokenType> OperatorTokenTypes = new() {
            TokenType.DM_And,
            TokenType.DM_AndAnd,
            TokenType.DM_AndEquals,
            TokenType.DM_Bar,
            TokenType.DM_BarBar,
            TokenType.DM_BarEquals,
            TokenType.DM_Colon,
            TokenType.DM_Comma,
            TokenType.DM_Equals,
            TokenType.DM_EqualsEquals,
            TokenType.DM_Exclamation,
            TokenType.DM_ExclamationEquals,
            TokenType.DM_GreaterThan,
            TokenType.DM_GreaterThanEquals,
            TokenType.DM_In,
            TokenType.DM_RightShift,
            TokenType.DM_RightShiftEquals,
            TokenType.DM_LeftBracket,
            TokenType.DM_LeftCurlyBracket,
            TokenType.DM_LeftParenthesis,
            TokenType.DM_LeftShift,
            TokenType.DM_LeftShiftEquals,
            TokenType.DM_LessThan,
            TokenType.DM_LessThanEquals,
            TokenType.DM_Minus,
            TokenType.DM_MinusEquals,
            TokenType.DM_MinusMinus,
            TokenType.DM_Modulus,
            TokenType.DM_ModulusEquals,
            TokenType.DM_Period,
            TokenType.DM_Plus,
            TokenType.DM_PlusEquals,
            TokenType.DM_PlusPlus,
            TokenType.DM_Question,
            TokenType.DM_QuestionColon,
            TokenType.DM_QuestionPeriod,
            TokenType.DM_RightBracket,
            TokenType.DM_RightCurlyBracket,
            TokenType.DM_RightParenthesis,
            TokenType.DM_Semicolon,
            TokenType.DM_Slash,
            TokenType.DM_SlashEquals,
            TokenType.DM_Star,
            TokenType.DM_StarEquals,
            TokenType.DM_StarStar,
            TokenType.DM_Tilde,
            TokenType.DM_TildeEquals,
            TokenType.DM_To,
            TokenType.DM_Xor,
            TokenType.DM_XorEquals
        };
    }
}
