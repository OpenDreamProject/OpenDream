using System;
using System.Collections.Generic;

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
            { "goto", TokenType.DM_Goto }
        };

        private bool _checkingIndentation = true;
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
                    case '(': Advance(); token = CreateToken(TokenType.DM_LeftParenthesis, c); break;
                    case ')': Advance(); token = CreateToken(TokenType.DM_RightParenthesis, c); break;
                    case '[': Advance(); token = CreateToken(TokenType.DM_LeftBracket, c); break;
                    case ']': Advance(); token = CreateToken(TokenType.DM_RightBracket, c); break;
                    case ',': Advance(); token = CreateToken(TokenType.DM_Comma, c); break;
                    case ';': Advance(); token = CreateToken(TokenType.DM_Semicolon, c); break;
                    case ':': Advance(); token = CreateToken(TokenType.DM_Colon, c); break;
                    case '?': Advance(); token = CreateToken(TokenType.DM_Question, c); break;
                    case '{': {
                        c = Advance();
                        
                        if (c == '"') {
                            token = LexString(true);
                            if (GetCurrent() != '}') throw new Exception("Expected '}' to end long string");
                            Advance();
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
                        string text = Convert.ToString(c);
                        string resourcePath = String.Empty;

                        do {
                            c = Advance();

                            text += c;
                            if (c != '\'' && c != '\n') {
                                resourcePath += c;
                            } else {
                                break;
                            }
                        } while (!IsAtEndOfFile());
                        if (c != '\'') throw new Exception("Expected \"\'\" to end resource path");

                        Advance();
                        token = CreateToken(TokenType.DM_Resource, text, resourcePath);
                        break;
                    }
                    case '"': {
                        token = LexString(false);

                        break;
                    }
                    default: {
                        if (IsAlphabetic(c) || c == '_') {
                            string text = Convert.ToString(c);

                            do {
                                c = Advance();

                                if (IsAlphanumeric(c) || c == '_') {
                                    text += c;
                                } else {
                                    break;
                                }
                            } while (!IsAtEndOfFile());

                            if (Keywords.TryGetValue(text, out TokenType keywordType)) {
                                token = CreateToken(keywordType, text);
                            } else {
                                token = CreateToken(TokenType.DM_Identifier, text);
                            }
                        } else if (IsNumeric(c)) {
                            string text = Convert.ToString(c);

                            do {
                                c = Advance();

                                if (IsNumeric(c) || c == '.' || c == 'E' || c == 'e') {
                                    if (c == '.' && text.Contains('.')) throw new Exception("Multiple decimals in number");
                                    if (c == 'E' || c == 'e') {
                                        text += c;
                                        c = Advance();

                                        if (!(IsNumeric(c) || c == '-' || c == '+')) throw new Exception("Invalid scientific notation");
                                    }

                                    text += c;
                                } else {
                                    break;
                                }
                            } while (!IsAtEndOfFile());

                            if (text.Contains('.') || text.Contains("e")) {
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

            while (c == '\t') {
                indentationLevel++;

                c = Advance();
            }

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
            char c = GetCurrent();
            string text = Convert.ToString(c);
            string stringValue = String.Empty;
            int bracketNesting = 0;

            if (isLong) _checkingIndentation = false;
            do {
                c = Advance();

                text += c;

                if (c == '[') bracketNesting++;
                else if (c == ']') bracketNesting--;

                if (bracketNesting == 0) {
                    if (c == '\\') {
                        string escapeSequence = String.Empty;

                        while (Advance() != ' ') {
                            c = GetCurrent();

                            text += c;
                            escapeSequence += c;
                            if (escapeSequence == "\"" || escapeSequence == "\\" || escapeSequence == "'") {
                                stringValue += escapeSequence;
                                break;
                            } else if (escapeSequence == "n") {
                                stringValue += '\n';
                                break;
                            } else if (escapeSequence == "t") {
                                stringValue += '\t';
                                break;
                            } else if (ValidEscapeSequences.Contains(escapeSequence)) { //Handled in the parser
                                stringValue += '\\' + escapeSequence;
                                break;
                            }
                        }

                        if (!ValidEscapeSequences.Contains(escapeSequence)) {
                            throw new Exception("Invalid escape sequence \"\\" + escapeSequence + "\"");
                        }
                    } else if (c != '"' && !(!isLong && c == '\n')) {
                        stringValue += c;
                    } else {
                        break;
                    }
                } else {
                    stringValue += c;
                }
        } while (!IsAtEndOfFile());
            if (c != '"') throw new Exception("Expected '\"' to end string");

            Advance();
            if (isLong) _checkingIndentation = true;
            return CreateToken(TokenType.DM_String, text, stringValue);
        }
    }
}
