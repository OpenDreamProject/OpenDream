using Robust.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenDreamShared.Compiler.DMF {
    public sealed class DMFLexer : TextLexer {
        public static readonly List<string> ValidAttributes = new() {
            "align",
            "allow-html",
            "alpha",
            "anchor1",
            "anchor2",
            "angle1",
            "angle2",
            "auto-format",
            "background-color",
            "bar-color",
            "border",
            "button-type",
            "can-check",
            "can-close",
            "can-minimize",
            "can-resize",
            "can-scroll",
            "category",
            "command",
            "cell-span",
            "cells",
            "current-cell",
            "current-tab",
            "dir",
            "drop-zone",
            "enable-http-images",
            "flash",
            "focus",
            "font-family",
            "font-size",
            "font-style",
            "group",
            "highlight-color",
            "icon",
            "icon-size",
            "id",
            "image",
            "image-mode",
            "inner-size",
            "index",
            "is-checked",
            "is-default",
            "is-disabled",
            "is-flat",
            "is-list",
            "is-minimized",
            "is-maximized",
            "is-pane",
            "is-password",
            "is-slider",
            "is-transparent",
            "is-vert",
            "is-visible",
            "keep-aspect",
            "legacy-size",
            "letterbox",
            "line-color",
            "link-color",
            "lock",
            "map-to",
            "max-lines",
            "multi-line",
            "name",
            "no-command",
            "on-close",
            "on-change",
            "on-hide",
            "on-show",
            "on-status",
            "on-size",
            "on-tab",
            "outer-size",
            "pos",
            "prefix-color",
            "right-click",
            "saved-params",
            "size",
            "show-history",
            "show-lines",
            "show-names",
            "show-splitter",
            "show-url",
            "small-icons",
            "splitter",
            "suffix-color",
            "statusbar",
            "style",
            "tab-background-color",
            "tab-font-family",
            "tab-font-size",
            "tab-font-style",
            "tab-text-color",
            "tabs",
            "text",
            "text-color",
            "text-mode",
            "text-wrap",
            "title",
            "titlebar",
            "transparent-color",
            "type",
            "use-title",
            "value",
            "view-size",
            "visited-color",
            "width",
            "zoom",
            "zoom-mode",
            "zoom-mode"
        };

        private static readonly Dictionary<string, TokenType> _keywords = new() {
            { "bottom", TokenType.DMF_Bottom },
            { "bottom-left", TokenType.DMF_BottomLeft },
            { "bottom-right", TokenType.DMF_BottomRight },
            { "BROWSER", TokenType.DMF_Browser },
            { "BUTTON", TokenType.DMF_Button },
            { "CHILD", TokenType.DMF_Child },
            { "center", TokenType.DMF_Center },
            { "distort", TokenType.DMF_Distort },
            { "elem", TokenType.DMF_Elem },
            { "INFO", TokenType.DMF_Info },
            { "INPUT", TokenType.DMF_Input },
            { "LABEL", TokenType.DMF_Label },
            { "left", TokenType.DMF_Left },
            { "macro", TokenType.DMF_Macro },
            { "MAIN", TokenType.DMF_Main },
            { "MAP", TokenType.DMF_Map },
            { "menu", TokenType.DMF_Menu },
            { "none", TokenType.DMF_None },
            { "line", TokenType.DMF_Line },
            { "OUTPUT", TokenType.DMF_Output },
            { "pushbox", TokenType.DMF_PushBox },
            { "pushbutton", TokenType.DMF_PushButton },
            { "right", TokenType.DMF_Right },
            { "stretch", TokenType.DMF_Stretch },
            { "sunken", TokenType.DMF_Sunken },
            { "top", TokenType.DMF_Top },
            { "top-left", TokenType.DMF_TopLeft },
            { "top-right", TokenType.DMF_TopRight },
            { "vertical", TokenType.DMF_Vertical },
            { "window", TokenType.DMF_Window }
        };

        public DMFLexer(string sourceName, string source) : base(sourceName, source) { }

        protected override Token ParseNextToken() {
            Token token = base.ParseNextToken();

            if (token == null) {
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

                        while (Advance() != c && !AtEndOfSource) {
                            if (GetCurrent() == '\\') {
                                Advance();

                                switch (GetCurrent()) {
                                    case '"':
                                    case '\\': text += GetCurrent(); break;
                                    case 't': text += '\t'; break;
                                    default: throw new Exception("Invalid escape sequence '\\" + GetCurrent() + "'");
                                }
                            } else {
                                text += GetCurrent();
                            }
                        }
                        if (GetCurrent() != c) throw new Exception("Expected '" + c + "'");
                        text += c;
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
                        token = CreateToken(TokenType.DMF_Color, text, Color.FromHex(text, Color.White));
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
                                token = CreateToken(TokenType.Error, text, "Invalid keyword '" + text + "'");
                            }

                            break;
                        }

                        string number = Number();
                        if (number != null) {
                            if (GetCurrent() == 'x') {
                                Advance();

                                string number2 = Number();
                                if (number2 == null) token = CreateToken(TokenType.Error, "Expected another number");
                                else token = CreateToken(TokenType.DMF_Dimension, number + "x" + number2, new Vector2i(int.Parse(number), int.Parse(number2)));
                            } else if (GetCurrent() == ',') {
                                Advance();

                                string number2 = Number();
                                if (number2 == null) token = CreateToken(TokenType.Error, "Expected another number");
                                else token = CreateToken(TokenType.DMF_Position, number + "," + number2, new Vector2i(int.Parse(number), int.Parse(number2)));
                            } else {
                                token = CreateToken(TokenType.DMF_Integer, number, int.Parse(number));
                            }

                            break;
                        }

                        token = CreateToken(TokenType.Error, $"Unknown character: {c.ToString()}");
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
