using System;
using System.Collections.Generic;

namespace DMCompiler {
    class DMLexer : Lexer {
        private Stack<int> _indentationStack = new Stack<int>(new int[] { 0 });
        private Dictionary<string, TokenType> _keywords = new Dictionary<string, TokenType>() {
            { "null", TokenType.DM_Null },
            { "break", TokenType.DM_Break },
            { "if", TokenType.DM_If },
            { "for", TokenType.DM_For },
            { "switch", TokenType.DM_Switch },
            { "while", TokenType.DM_While },
            { "var", TokenType.DM_Var },
            { "proc", TokenType.DM_Proc },
            { "new", TokenType.DM_New },
            { "del", TokenType.DM_Del },
            { "return", TokenType.DM_Return },
            { "in", TokenType.DM_In }
        };

        public DMLexer(string source) : base(source) {

        }

        protected override Token ParseNextToken() {
            Token token = base.ParseNextToken();

            if (token.Type == TokenType.Unknown) {
                char c = GetCurrent();

                switch (c) {
                    case ' ': this.Advance(); token = CreateToken(TokenType.Skip, c); break;
                    case '(': this.Advance(); token = CreateToken(TokenType.DM_LeftParenthesis, c); break;
                    case ')': this.Advance(); token = CreateToken(TokenType.DM_RightParenthesis, c); break;
                    case ',': this.Advance(); token = CreateToken(TokenType.DM_Comma, c); break;
                    case '.': {
                        c = this.Advance();

                        if (c == '.') {
                            this.Advance();

                            token = CreateToken(TokenType.DM_SuperProc, "..");
                        } else {
                            token = CreateToken(TokenType.DM_Period, '.');
                        }

                        break;
                    }
                    case '/': {
                        c = this.Advance();

                        if (c == '=') {
                            this.Advance();

                            token = CreateToken(TokenType.DM_SlashEquals, "/=");
                        } else {
                            token = CreateToken(TokenType.DM_Slash, '/');
                        }

                        break;
                    }
                    case '=': {
                        c = this.Advance();

                        if (c == '=') {
                            this.Advance();

                            token = CreateToken(TokenType.DM_EqualsEquals, "==");
                        } else {
                            token = CreateToken(TokenType.DM_Equals, '=');
                        }

                        break;
                    }
                    case '!': {
                        c = this.Advance();

                        if (c == '=') {
                            this.Advance();

                            token = CreateToken(TokenType.DM_ExclamationEquals, "!=");
                        } else {
                            token = CreateToken(TokenType.DM_Exclamation, '!');
                        }

                        break;
                    }
                    case '&': {
                        c = this.Advance();

                        if (c == '&') {
                            this.Advance();

                            token = CreateToken(TokenType.DM_AndAnd, "&&");
                        } else if (c == '=') {
                            this.Advance();

                            token = CreateToken(TokenType.DM_AndEquals, "&=");
                        } else {
                            token = CreateToken(TokenType.DM_And, '&');
                        }

                        break;
                    }
                    case '+': {
                        c = this.Advance();

                        if (c == '=') {
                            this.Advance();

                            token = CreateToken(TokenType.DM_PlusEquals, "+=");
                        } else {
                            token = CreateToken(TokenType.DM_Plus, '+');
                        }

                        break;
                    }
                    case '-': {
                        c = this.Advance();

                        if (c == '=') {
                            this.Advance();

                            token = CreateToken(TokenType.DM_MinusEquals, "-=");
                        } else {
                            token = CreateToken(TokenType.DM_Minus, '-');
                        }

                        break;
                    }
                    case '"': {
                        string text = Convert.ToString(c);
                        string stringValue = String.Empty;

                        do {
                            c = Advance();

                            text += c;
                            if (c == '\\') {
                                c = Advance();

                                text += c;
                                if (c == '"') {
                                    stringValue += '"';
                                } else if (c == 'n') {
                                    stringValue += '\n';
                                } else {
                                    throw new Exception("Invalid escape sequence '\\" + c + "'");
                                }
                            } else if (c != '"' && c != '\n') {
                                stringValue += c;
                            } else {
                                break;
                            }
                        } while (!IsAtEndOfFile());
                        if (c != '"') throw new Exception("Expected '\"' to end string");

                        this.Advance();
                        token = CreateToken(TokenType.DM_String, text, stringValue);
                        break;
                    }
                    default: {
                        if (IsAlphabetic(c)) {
                            string text = Convert.ToString(c);

                            do {
                                c = Advance();

                                if (IsAlphanumeric(c) || c == '_') {
                                    text += c;
                                } else {
                                    break;
                                }
                            } while (!IsAtEndOfFile());

                            if (_keywords.TryGetValue(text, out TokenType keywordType)) {
                                token = CreateToken(keywordType, text);
                            } else {
                                token = CreateToken(TokenType.DM_Identifier, text);
                            }
                        } else if (IsNumeric(c)) {
                            string text = Convert.ToString(c);

                            do {
                                c = Advance();

                                if (IsAlphanumeric(c) || c == '.') {
                                    if (c == '.' && text.Contains('.')) throw new Exception("Multiple decimals in number");

                                    text += c;
                                } else {
                                    break;
                                }
                            } while (!IsAtEndOfFile());

                            if (text.Contains('.')) {
                                token = CreateToken(TokenType.DM_Float, text, Convert.ToSingle(text));
                            } else {
                                token = CreateToken(TokenType.DM_Integer, text, Convert.ToInt32(text));
                            }
                        } else { //Not a known token, advance over it
                            Advance();
                        }

                        break;
                    }
                }
            }

            return token;
        }

        protected override char Advance() {
            base.Advance();

            if (_currentColumn == 1) { //Beginning a new line
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
    }
}
