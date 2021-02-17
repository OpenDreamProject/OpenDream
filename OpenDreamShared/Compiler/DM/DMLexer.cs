using System;
using System.Collections.Generic;
using System.Text;

namespace OpenDreamShared.Compiler.DM {
    class DMLexer : Lexer {
        public static List<string> ValidEscapeSequences = new List<string>() {
            "t", "n",
            "[", "]",
            "\\", "\"", "'",

            "icon",
            "Roman", "roman",
            "The", "the",
            "A", "a", "An", "an",
            "s",
            "him",
            "His", "his",
            "Hers", "hers",
            "ref",
            "improper", "proper",
            "red", "blue", "green", "black",
            "..."
        };

        public static Dictionary<string, TokenType> Keywords = new Dictionary<string, TokenType>() {
            { "null", TokenType.DM_Null },
            { "break", TokenType.DM_Break },
            { "continue", TokenType.DM_Continue },
            { "if", TokenType.DM_If },
            { "else", TokenType.DM_Else },
            { "for", TokenType.DM_For },
            { "switch", TokenType.DM_Switch },
            { "while", TokenType.DM_While },
            { "do", TokenType.DM_Do },
            { "var", TokenType.DM_Var },
            { "proc", TokenType.DM_Proc },
            { "new", TokenType.DM_New },
            { "del", TokenType.DM_Del },
            { "return", TokenType.DM_Return },
            { "in", TokenType.DM_In },
            { "to", TokenType.DM_To },
            { "as", TokenType.DM_As },
            { "set", TokenType.DM_Set },
            { "call", TokenType.DM_Call },
            { "spawn", TokenType.DM_Spawn },
            { "list", TokenType.DM_List },
            { "goto", TokenType.DM_Goto },
            { "step", TokenType.DM_Step }
        };

        private bool _checkingIndentation = true;
        private int bracketNesting = 0;
        private Stack<int> _indentationStack = new Stack<int>(new int[] { 0 });

        public DMLexer(string source) : base(source) { }

        protected override Token ParseNextToken() {
            Token token = base.ParseNextToken();

            if (token.Type == TokenType.Unknown) {
                char c = GetCurrent();

                switch (c) {
                    case ' ':
                    case '\t': {
                        Advance();
                        while (GetCurrent() == ' ' || GetCurrent() == '\t') Advance();

                        token = CreateToken(TokenType.DM_Whitespace, ' ');
                        break;
                    }
                    case '(': Advance(); token = CreateToken(TokenType.DM_LeftParenthesis, c); bracketNesting++; break;
                    case ')': Advance(); token = CreateToken(TokenType.DM_RightParenthesis, c); bracketNesting--; break;
                    case '[': Advance(); token = CreateToken(TokenType.DM_LeftBracket, c); bracketNesting++; break;
                    case ']': Advance(); token = CreateToken(TokenType.DM_RightBracket, c); bracketNesting--; break;
                    case ',': Advance(); token = CreateToken(TokenType.DM_Comma, c); break;
                    case ';': Advance(); token = CreateToken(TokenType.DM_Semicolon, c); break;
                    case ':': Advance(); token = CreateToken(TokenType.DM_Colon, c); break;
                    case '?': Advance(); token = CreateToken(TokenType.DM_Question, c); break;
                    case '{': {
                        c = Advance();

                        if (c == '"') {
                            token = LexString(true);
                            token.Text = "{" + token.Text + "}";
                        } else {
                            token = CreateToken(TokenType.DM_LeftCurlyBracket, '{');
                        }

                        break;
                    }
                    case '}': {
                        Advance();

                        _pendingTokenQueue.Enqueue(CreateToken(TokenType.DM_RightCurlyBracket, c));
                        token = CreateToken(TokenType.Newline, '\n');
                        break;
                    }
                    case '.': {
                        c = Advance();

                        if (c == '.') {
                            Advance();

                            token = CreateToken(TokenType.DM_SuperProc, "..");
                        } else {
                            token = CreateToken(TokenType.DM_Period, '.');
                        }

                        break;
                    }
                    case '/': {
                        c = Advance();

                        if (c == '=') {
                            Advance();

                            token = CreateToken(TokenType.DM_SlashEquals, "/=");
                        } else {
                            token = CreateToken(TokenType.DM_Slash, '/');
                        }

                        break;
                    }
                    case '=': {
                        c = Advance();

                        if (c == '=') {
                            Advance();

                            token = CreateToken(TokenType.DM_EqualsEquals, "==");
                        } else {
                            token = CreateToken(TokenType.DM_Equals, '=');
                        }

                        break;
                    }
                    case '!': {
                        c = Advance();

                        if (c == '=') {
                            Advance();

                            token = CreateToken(TokenType.DM_ExclamationEquals, "!=");
                        } else {
                            token = CreateToken(TokenType.DM_Exclamation, '!');
                        }

                        break;
                    }
                    case '&': {
                        c = Advance();

                        if (c == '&') {
                            Advance();

                            token = CreateToken(TokenType.DM_AndAnd, "&&");
                        } else if (c == '=') {
                            Advance();

                            token = CreateToken(TokenType.DM_AndEquals, "&=");
                        } else {
                            token = CreateToken(TokenType.DM_And, '&');
                        }

                        break;
                    }
                    case '+': {
                        c = Advance();

                        if (c == '+') {
                            Advance();

                            token = CreateToken(TokenType.DM_PlusPlus, "++");
                        } else if (c == '=') {
                            Advance();

                            token = CreateToken(TokenType.DM_PlusEquals, "+=");
                        } else {
                            token = CreateToken(TokenType.DM_Plus, '+');
                        }

                        break;
                    }
                    case '-': {
                        c = Advance();

                        if (c == '-') {
                            Advance();

                            token = CreateToken(TokenType.DM_MinusMinus, "--");
                        } else if (c == '=') {
                            Advance();

                            token = CreateToken(TokenType.DM_MinusEquals, "-=");
                        } else {
                            token = CreateToken(TokenType.DM_Minus, '-');
                        }

                        break;
                    }
                    case '*': {
                        c = Advance();

                        if (c == '=') {
                            Advance();

                            token = CreateToken(TokenType.DM_StarEquals, "*=");
                        } else if (c == '*') {
                            Advance();

                            token = CreateToken(TokenType.DM_StarStar, "**");
                        } else {
                            token = CreateToken(TokenType.DM_Star, '*');
                        }

                        break;
                    }
                    case '^': {
                        c = Advance();

                        if (c == '=') {
                            Advance();

                            token = CreateToken(TokenType.DM_XorEquals, "^=");
                        } else {
                            token = CreateToken(TokenType.DM_Xor, '^');
                        }

                        break;
                    }
                    case '%': {
                        c = Advance();

                        if (c == '=') {
                            Advance();

                            token = CreateToken(TokenType.DM_ModulusEquals, "%=");
                        } else {
                            token = CreateToken(TokenType.DM_Modulus, '%');
                        }

                        break;
                    }
                    case '~': {
                        c = Advance();

                        if (c == '=') {
                            Advance();

                            token = CreateToken(TokenType.DM_TildeEquals, "~=");
                        } else {
                            token = CreateToken(TokenType.DM_Tilde, '~');
                        }

                        break;
                    }
                    case '<': {
                        c = Advance();

                        if (c == '<') {
                            c = Advance();

                            if (c == '=') {
                                Advance();

                                token = CreateToken(TokenType.DM_LeftShiftEquals, "<<=");
                            } else {
                                token = CreateToken(TokenType.DM_LeftShift, "<<");
                            }
                        } else if (c == '=') {
                            Advance();

                            token = CreateToken(TokenType.DM_LessThanEquals, "<=");
                        } else {
                            token = CreateToken(TokenType.DM_LessThan, '<');
                        }

                        break;
                    }
                    case '>': {
                        c = Advance();

                        if (c == '>') {
                            c = Advance();

                            if (c == '=') {
                                Advance();

                                token = CreateToken(TokenType.DM_RightShiftEquals, ">>=");
                            } else {
                                token = CreateToken(TokenType.DM_RightShift, ">>");
                            }
                        } else if (c == '=') {
                            Advance();

                            token = CreateToken(TokenType.DM_GreaterThanEquals, ">=");
                        } else {
                            token = CreateToken(TokenType.DM_GreaterThan, '>');
                        }

                        break;
                    }
                    case '|': {
                        c = Advance();

                        if (c == '|') {
                            Advance();

                            token = CreateToken(TokenType.DM_BarBar, "||");
                        } else if (c == '=') {
                            Advance();

                            token = CreateToken(TokenType.DM_BarEquals, "|=");
                        } else {
                            token = CreateToken(TokenType.DM_Bar, '|');
                        }

                        break;
                    }
                    case '\'': {
                        StringBuilder resourcePathBuilder = new StringBuilder(Convert.ToString(c));

                        do {
                            c = Advance();

                            if (c != '\'' && c != '\n') {
                                resourcePathBuilder.Append(c);
                            } else {
                                break;
                            }
                        } while (!IsAtEndOfFile());
                        if (c != '\'') throw new Exception("Expected \"\'\" to end resource path");
                        resourcePathBuilder.Append('\'');

                        Advance();

                        string text = resourcePathBuilder.ToString();
                        token = CreateToken(TokenType.DM_Resource, text, text.Substring(1, text.Length - 2));
                        break;
                    }
                    case '"': {
                        token = LexString(false);

                        break;
                    }
                    default: {
                        if (IsAlphabetic(c) || c == '_') {
                            StringBuilder textBuilder = new StringBuilder(Convert.ToString(c));

                            do {
                                c = Advance();

                                if (IsAlphanumeric(c) || c == '_') {
                                    textBuilder.Append(c);
                                } else {
                                    break;
                                }
                            } while (!IsAtEndOfFile());

                            string text = textBuilder.ToString();
                            if (Keywords.TryGetValue(text, out TokenType keywordType)) {
                                token = CreateToken(keywordType, text);
                            } else {
                                token = CreateToken(TokenType.DM_Identifier, text);
                            }
                        } else if (IsNumeric(c)) {
                            StringBuilder textBuilder = new StringBuilder(Convert.ToString(c));
                            bool containsDecimal = false;

                            do {
                                c = Advance();

                                if (IsNumeric(c) || c == '.' || c == 'E' || c == 'e') {
                                    if (c == '.') {
                                        if (containsDecimal) throw new Exception("Multiple decimals in number");

                                        containsDecimal = true;
                                    }

                                    if (c == 'E' || c == 'e') {
                                        textBuilder.Append(c);
                                        c = Advance();

                                        if (!(IsNumeric(c) || c == '-' || c == '+')) throw new Exception("Invalid scientific notation");
                                    }

                                    textBuilder.Append(c);
                                } else {
                                    break;
                                }
                            } while (!IsAtEndOfFile());

                            string text = textBuilder.ToString();
                            if (containsDecimal || text.Contains("e")) {
                                token = CreateToken(TokenType.DM_Float, text, Convert.ToSingle(text));
                            } else if (Int32.TryParse(text, out int value)) {
                                token = CreateToken(TokenType.DM_Integer, text, value);
                            } else {
                                throw new Exception("Invalid number '" + text + "'");
                            }
                        } else { //Not a known token, advance over it
                            Advance();
                        }

                        break;
                    }
                }
            } else if (token.Type == TokenType.Newline && bracketNesting != 0) { //Don't emit newlines within brackets
                token = CreateToken(TokenType.Skip, '\n');
            } else if (token.Type == TokenType.EndOfFile) {
                while (_indentationStack.Peek() > 0) {
                    _indentationStack.Pop();
                    _pendingTokenQueue.Enqueue(CreateToken(TokenType.DM_Dedent, '\r'));
                }
            }

            return token;
        }

        protected override char Advance() {
            base.Advance();

            if (_currentColumn == 1 && _checkingIndentation) { //Beginning a new line
                CheckIndentation();
            }

            return GetCurrent();
        }

        private void CheckIndentation() {
            char c = GetCurrent();
            int indentationLevel = 0;

            while (c == '\t' || c == ' ') {
                indentationLevel++;

                c = Advance();
            }

            if (bracketNesting != 0) return; //Don't emit identation when inside brackets
            if (Lines[_currentLine - 1].Trim() == "") //Don't emit indentation tokens on empty lines
                return;

            int currentIndentationLevel = _indentationStack.Peek();
            if (indentationLevel > currentIndentationLevel) {
                _indentationStack.Push(indentationLevel);
                _pendingTokenQueue.Enqueue(CreateToken(TokenType.DM_Indent, '\t'));
            } else if (indentationLevel < currentIndentationLevel) {
                do {
                    _indentationStack.Pop();
                    _pendingTokenQueue.Enqueue(CreateToken(TokenType.DM_Dedent, '\r'));
                } while (indentationLevel < _indentationStack.Peek());

                if (indentationLevel != _indentationStack.Peek()) {
                    throw new Exception("Invalid indentation at line " + _currentLine + ":" + _currentColumn);
                }
            }
        }

        private Token LexString(bool isLong) {
            StringBuilder textBuilder = new StringBuilder(Convert.ToString(GetCurrent()));
            int stringBracketNesting = 0;
            
            if (isLong) _checkingIndentation = false;

            char c;
            Advance();
            do {
                c = GetCurrent();

                textBuilder.Append(c);

                if (c == '\\') { // So \" doesn't end the string
                    textBuilder.Append(Advance());
                    c = Advance();
                    textBuilder.Append(c);
                }

                if (c == '[') stringBracketNesting++;
                else if (c == ']') stringBracketNesting--;

                if (stringBracketNesting == 0) {
                    if (c == '"' || (!isLong && c == '\n')) {
                        if (isLong) {
                            c = Advance();

                            if (c == '}') break;
                        } else {
                            break;
                        }
                    } else {
                        Advance();
                    }
                } else {
                    Advance();
                }
            } while (!IsAtEndOfFile());

            if (isLong && c != '}') throw new Exception("Expected \"\"}\" to end long string");
            else if (!isLong && c != '"') throw new Exception("Expected '\"' to end string");

            Advance();
            if (isLong) _checkingIndentation = true;

            string text = textBuilder.ToString();
            return CreateToken(TokenType.DM_String, text, text.Substring(1, text.Length - 2));
        }
    }
}
