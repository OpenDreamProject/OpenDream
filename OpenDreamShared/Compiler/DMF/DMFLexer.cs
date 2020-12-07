using System;
using System.Collections.Generic;
using System.Text;

namespace OpenDreamShared.Compiler.DMF {
    class DMFLexer : Lexer {
        public static Dictionary<string, TokenType> Keywords = new Dictionary<string, TokenType>() {
            { "true", TokenType.DMF_True },
            { "false", TokenType.DMF_False },
            { "window", TokenType.DMF_Window },
            { "elem", TokenType.DMF_Elem },
            { "MAIN", TokenType.DMF_Main },
            { "CHILD", TokenType.DMF_Child },
            { "MAP", TokenType.DMF_Map },
            { "OUTPUT", TokenType.DMF_Output },
            { "INFO", TokenType.DMF_Info },
            { "type", TokenType.DMF_Type },
            { "pos", TokenType.DMF_Pos },
            { "size", TokenType.DMF_Size },
            { "anchor1", TokenType.DMF_Anchor1 },
            { "anchor2", TokenType.DMF_Anchor2 },
            { "is-default", TokenType.DMF_IsDefault },
            { "is-pane", TokenType.DMF_IsPane },
            { "is-vert", TokenType.DMF_IsVert },
            { "left", TokenType.DMF_Left },
            { "right", TokenType.DMF_Right }
        };

        public DMFLexer(string source) : base(source) { }

        protected override Token ParseNextToken() {
            Token token = base.ParseNextToken();

            if (token.Type == TokenType.Unknown) {
                char c = GetCurrent();

                switch (c) {
                    case ' ':
                    case '\t': {
                        Advance();
                        token = CreateToken(TokenType.Skip, c);
                        break;
                    }
                    case '=': Advance(); token = CreateToken(TokenType.DMF_Equals, c); break;
                    case 'x': Advance(); token = CreateToken(TokenType.DMF_X, c); break;
                    case ',': Advance(); token = CreateToken(TokenType.DMF_Comma, c); break;
                    case '"': {
                        string text = c.ToString();

                        do {
                            text += Advance();
                        } while (GetCurrent() != '"' && !IsAtEndOfFile());
                        if (GetCurrent() != '"') throw new Exception("Expected '\"'");
                        Advance();

                        token = CreateToken(TokenType.DMF_String, text, text.Substring(1, text.Length - 2));
                        break;
                    }
                    default: {
                        if (IsAlphabetic(c)) {
                            string text = c.ToString();

                            while ((IsAlphanumeric(Advance()) || GetCurrent() == '-') && !IsAtEndOfFile()) text += GetCurrent();

                            if (Keywords.ContainsKey(text)) {
                                token = CreateToken(Keywords[text], text);
                            } else {
                                throw new Exception("Invalid keyword '" + text + "'");
                            }
                        } else if (IsNumeric(c)) {
                            string text = c.ToString();

                            while (IsNumeric(Advance()) && !IsAtEndOfFile()) text += GetCurrent();

                            token = CreateToken(TokenType.DMF_Integer, text, int.Parse(text));
                        } else {
                            Advance();
                        }

                        break;
                    }
                }
            }

            return token;
        }
    }
}
