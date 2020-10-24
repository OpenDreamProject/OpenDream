using System;
using System.Collections.Generic;

namespace DMCompiler.DM {
    class DMLexer : Lexer {
        private Stack<int> _indentationStack = new Stack<int>(new int[] { 0 });
        private Dictionary<string, TokenType> _keywords = new Dictionary<string, TokenType>() {
            { "null", TokenType.DM_Null },
            { "break", TokenType.DM_Break },
            { "if", TokenType.DM_If },
            { "else", TokenType.DM_Else },
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
                    case ' ': Advance(); token = CreateToken(TokenType.Skip, c); break;
                    case '\t': Advance(); token = CreateToken(TokenType.Skip, c); break;
                    case '(': Advance(); token = CreateToken(TokenType.DM_LeftParenthesis, c); break;
                    case ')': Advance(); token = CreateToken(TokenType.DM_RightParenthesis, c); break;
                    case '[': Advance(); token = CreateToken(TokenType.DM_LeftBracket, c); break;
                    case ']': Advance(); token = CreateToken(TokenType.DM_RightBracket, c); break;
                    case '{': Advance(); token = CreateToken(TokenType.DM_LeftCurlyBracket, c); break;
                    case '}': Advance(); token = CreateToken(TokenType.DM_RightCurlyBracket, c); break;
                    case ',': Advance(); token = CreateToken(TokenType.DM_Comma, c); break;
                    case ';': Advance(); token = CreateToken(TokenType.DM_Semicolon, c); break;
                    case ':': Advance(); token = CreateToken(TokenType.DM_Colon, c); break;
                    case '?': Advance(); token = CreateToken(TokenType.DM_Question, c); break;
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

                        if (c == '=') {
                            Advance();

                            token = CreateToken(TokenType.DM_PlusEquals, "+=");
                        } else {
                            token = CreateToken(TokenType.DM_Plus, '+');
                        }

                        break;
                    }
                    case '-': {
                        c = Advance();

                        if (c == '=') {
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
                        } else {
                            token = CreateToken(TokenType.DM_Star, '*');
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
                            Advance();

                            token = CreateToken(TokenType.DM_Output, "<<");
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
                            Advance();

                            token = CreateToken(TokenType.DM_Input, ">>");
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

                            token = CreateToken(TokenType.DM_Bar, "||");
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
                        string text = Convert.ToString(c);
                        string stringValue = String.Empty;

                        do {
                            c = Advance();

                            text += c;
                            if (c == '\\') {
                                string escapeSequence = String.Empty;
                                bool validEscapeSequence = false;

                                while (Advance() != ' ') {
                                    c = GetCurrent();

                                    text += c;
                                    escapeSequence += c;
                                    if (escapeSequence == "\"" || escapeSequence == "n" || escapeSequence == "\\" || escapeSequence == "[" || escapeSequence == "]") {
                                        stringValue += escapeSequence;
                                        validEscapeSequence = true;
                                        break;
                                    } else if (escapeSequence == "Roman" || escapeSequence == "roman") {
                                        //TODO: Roman escape sequence
                                        validEscapeSequence = true;
                                        break;
                                    } else if (escapeSequence == "The" || escapeSequence == "the") {
                                        //TODO: "The" escape sequence
                                        validEscapeSequence = true;
                                        break;
                                    } else if (escapeSequence == "A" || escapeSequence == "a" || escapeSequence == "An" || escapeSequence == "an") {
                                        //TODO: "A(n)" escape sequence
                                        validEscapeSequence = true;
                                        break;
                                    } else if (escapeSequence == "s") {
                                        //TODO: "s" escape sequence
                                        validEscapeSequence = true;
                                        break;
                                    } else if (escapeSequence == "ref") {
                                        //TODO: Ref escape sequence
                                        validEscapeSequence = true;
                                        break;
                                    } else if (escapeSequence == "improper") {
                                        //TODO: Improper escape sequence
                                        validEscapeSequence = true;
                                        break;
                                    } else if (escapeSequence == "proper") {
                                        //TODO: Proper escape sequence
                                        validEscapeSequence = true;
                                        break;
                                    }
                                }

                                if (!validEscapeSequence) {
                                    throw new Exception("Invalid escape sequence \"\\" + escapeSequence + "\"");
                                }
                            } else if (c != '"' && c != '\n') {
                                stringValue += c;
                            } else {
                                break;
                            }
                        } while (!IsAtEndOfFile());
                        if (c != '"') throw new Exception("Expected '\"' to end string");

                        Advance();
                        token = CreateToken(TokenType.DM_String, text, stringValue);
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
                                int value;
                                if (!Int32.TryParse(text, out value)) {
                                    value = Int32.MaxValue;
                                }

                                token = CreateToken(TokenType.DM_Integer, text, value);
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
