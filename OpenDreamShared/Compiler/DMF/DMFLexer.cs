using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace OpenDreamShared.Compiler.DMF {
    public class DMFLexer : TextLexer {
        public static readonly List<string> ValidAttributes = new() {
            "type",
            "pos",
            "size",
            "anchor1",
            "anchor2",
            "is-default",
            "is-pane",
            "is-vert",
            "is-visible",
            "is-disabled",
            "left",
            "right",
            "name",
            "command",
            "category",
            "saved-params",
            "background-color",
            "border",
            "button-type",
            "font-family",
            "font-size",
            "zoom-mode",
            "text-color",
            "auto-format",
            "statusbar",
            "right-click",
            "icon",
            "text"
        };

        private static Dictionary<string, TokenType> _keywords = new() {
            { "macro", TokenType.DMF_Macro },
            { "menu", TokenType.DMF_Menu },
            { "window", TokenType.DMF_Window },
            { "elem", TokenType.DMF_Elem },
            { "MAIN", TokenType.DMF_Main },
            { "CHILD", TokenType.DMF_Child },
            { "MAP", TokenType.DMF_Map },
            { "OUTPUT", TokenType.DMF_Output },
            { "INFO", TokenType.DMF_Info },
            { "INPUT", TokenType.DMF_Input },
            { "BUTTON", TokenType.DMF_Button },
            { "BROWSER", TokenType.DMF_Browser },
            { "sunken", TokenType.DMF_Sunken },
            { "pushbox", TokenType.DMF_PushBox },
            { "distort", TokenType.DMF_Distort },
            { "none", TokenType.DMF_None }
        };

        public DMFLexer(string sourceName, string source) : base(sourceName, source) { }

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
                    case '\'':
                    case '"': {
                        string text = c.ToString();

                        do {
                            if (GetCurrent() == '\\') {
                                Advance();

                                if (GetCurrent() == '"' || GetCurrent() == '\\') {
                                    Advance();

                                    text += GetCurrent();
                                } else if (GetCurrent() == 't') {
                                    Advance();

                                    text += '\t';
                                } else {
                                    throw new Exception("Invalid escape sequence '\\" + GetCurrent() + "'");
                                }
                            } else {
                                text += Advance();
                            }
                        } while (GetCurrent() != c && !AtEndOfSource);
                        if (GetCurrent() != c) throw new Exception("Expected '" + c + "'");
                        Advance();

                        if (c == '"') token = CreateToken(TokenType.DMF_String, text, text.Substring(1, text.Length - 2));
                        else if (c == '\'') token = CreateToken(TokenType.DMF_Resource, text, text.Substring(1, text.Length - 2));
                        break;
                    }
                    case '#': {
                        string text = c.ToString();

                        for (int i = 0; i < 6; i++) {
                            if (!IsHex(Advance())) throw new Exception("Expected 6 hexadecimal digits");

                            text += GetCurrent();
                        }

                        Advance();
                        token = CreateToken(TokenType.DMF_Color, text, Color.FromArgb(Convert.ToInt32(text.Substring(1), 16)));
                        break;
                    }
                    default: {
                        if (IsAlphabetic(c)) {
                            string text = c.ToString();

                            while ((IsAlphanumeric(Advance()) || GetCurrent() == '-') && !AtEndOfSource) text += GetCurrent();

                            if (_keywords.TryGetValue(text, out TokenType keyword)) {
                                token = CreateToken(keyword, text);
                            } else if (text == "true") {
                                token = CreateToken(TokenType.DMF_Boolean, text, true);
                            } else if (text == "false") {
                                token = CreateToken(TokenType.DMF_Boolean, text, false);
                            } else if (ValidAttributes.Contains(text)) {
                                token = CreateToken(TokenType.DMF_Attribute, text);
                            } else {
                                throw new Exception("Invalid keyword '" + text + "'");
                            }

                            break;
                        }

                        string number = Number();
                        if (number != null) {
                            if (GetCurrent() == 'x') {
                                Advance();

                                string number2 = Number();
                                if (number2 == null) token = CreateToken(TokenType.Error, "Expected another number");
                                else token = CreateToken(TokenType.DMF_Dimension, number + "x" + number2, new Size(int.Parse(number), int.Parse(number2)));
                            } else if (GetCurrent() == ',') {
                                Advance();

                                string number2 = Number();
                                if (number2 == null) token = CreateToken(TokenType.Error, "Expected another number");
                                else token = CreateToken(TokenType.DMF_Position, number + "," + number2, new Point(int.Parse(number), int.Parse(number2)));
                            } else {
                                token = CreateToken(TokenType.DMF_Integer, number, int.Parse(number));
                            }

                            break;
                        }

                        Advance();
                        break;
                    }
                }
            }

            return token;
        }

        private string Number() {
            char c = GetCurrent();

            if (IsNumeric(c) || c == '-') {
                StringBuilder text = new StringBuilder(c.ToString());

                while (IsNumeric(Advance()) && !AtEndOfSource) text.Append(GetCurrent());

                return text.ToString();
            }

            return null;
        }
    }
}
