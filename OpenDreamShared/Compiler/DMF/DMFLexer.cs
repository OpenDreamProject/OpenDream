using System;
using System.Collections.Generic;

namespace OpenDreamShared.Compiler.DMF {
    class DMFLexer : TextLexer {
        public static Dictionary<string, TokenType> Keywords = new Dictionary<string, TokenType>() {
            { "true", TokenType.DMF_True },
            { "false", TokenType.DMF_False },
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
            { "type", TokenType.DMF_Type },
            { "pos", TokenType.DMF_Pos },
            { "size", TokenType.DMF_Size },
            { "anchor1", TokenType.DMF_Anchor1 },
            { "anchor2", TokenType.DMF_Anchor2 },
            { "is-default", TokenType.DMF_IsDefault },
            { "is-pane", TokenType.DMF_IsPane },
            { "is-vert", TokenType.DMF_IsVert },
            { "is-visible", TokenType.DMF_IsVisible },
            { "is-disabled", TokenType.DMF_IsDisabled },
            { "left", TokenType.DMF_Left },
            { "right", TokenType.DMF_Right },
            { "name", TokenType.DMF_Name },
            { "command", TokenType.DMF_Command },
            { "category", TokenType.DMF_Category },
            { "saved-params", TokenType.DMF_SavedParams },
            { "background-color", TokenType.DMF_BackgroundColor },
            { "border", TokenType.DMF_Border },
            { "button-type", TokenType.DMF_ButtonType },
            { "font-family", TokenType.DMF_FontFamily },
            { "font-size", TokenType.DMF_FontSize },
            { "zoom-mode", TokenType.DMF_ZoomMode },
            { "text-color", TokenType.DMF_TextColor },
            { "auto-format", TokenType.DMF_AutoFormat },
            { "statusbar", TokenType.DMF_StatusBar },
            { "right-click", TokenType.DMF_RightClick },
            { "icon", TokenType.DMF_Icon },
            { "text", TokenType.DMF_Text },
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
                    case 'x': Advance(); token = CreateToken(TokenType.DMF_X, c); break;
                    case ',': Advance(); token = CreateToken(TokenType.DMF_Comma, c); break;
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
                        token = CreateToken(TokenType.DMF_Color, text, Convert.ToInt32(text.Substring(1), 16));
                        break;
                    }
                    default: {
                        if (IsAlphabetic(c)) {
                            string text = c.ToString();

                            while ((IsAlphanumeric(Advance()) || GetCurrent() == '-') && !AtEndOfSource) text += GetCurrent();

                            if (Keywords.ContainsKey(text)) {
                                token = CreateToken(Keywords[text], text);
                            } else {
                                throw new Exception("Invalid keyword '" + text + "'");
                            }
                        } else if (IsNumeric(c) || c == '-') {
                            string text = c.ToString();

                            while (IsNumeric(Advance()) && !AtEndOfSource) text += GetCurrent();

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
