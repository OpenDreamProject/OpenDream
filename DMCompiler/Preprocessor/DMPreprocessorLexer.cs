using OpenDreamShared.Compiler;
using System;
using System.Collections.Generic;
using System.Text;

namespace DMCompiler.Preprocessor {
    class DMPreprocessorLexer : Lexer {
        public DMPreprocessorLexer(string source) : base(source) { }

        public Token GetNextTokenIgnoringWhitespace() {
            Token nextToken = GetNextToken();
            while (nextToken.Type == TokenType.DM_Preproc_Whitespace) nextToken = GetNextToken();

            return nextToken;
        }

        protected override Token ParseNextToken() {
            Token token = base.ParseNextToken();

            if (token.Type == TokenType.Unknown) {
                char c = GetCurrent();

                switch (c) {
                    case ' ':
                    case '\t': Advance(); token = CreateToken(TokenType.DM_Preproc_Whitespace, c); break;
                    case '}':
                    case '!':
                    case '&':
                    case '|':
                    case '%':
                    case '>':
                    case '<':
                    case '^':
                    case ':':
                    case ';':
                    case '?':
                    case '+':
                    case '-':
                    case '*':
                    case '~':
                    case '=': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator, c); break;
                    case ',': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator_Comma, c); break;
                    case '(': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator_LeftParenthesis, c); break;
                    case ')': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator_RightParenthesis, c); break;
                    case '[': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator_LeftBracket, c); break;
                    case ']': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator_RightBracket, c); break;
                    case '/': {
                        if (Advance() == '/') {
                            while (Advance() != '\n' && !IsAtEndOfFile()) ;

                            token = CreateToken(TokenType.Skip, "//");
                        } else if (GetCurrent() == '*') {
                            //Skip everything up to the "*/"
                            while (true) {
                                Advance();

                                if (GetCurrent() == '*' && Advance() == '/') break;
                                else if (IsAtEndOfFile()) throw new Exception("Expected \"*/\" to end multiline comment");
                            }

                            Advance();
                            token = CreateToken(TokenType.Skip, "/* */");
                        } else {
                            token = CreateToken(TokenType.DM_Preproc_Punctuator, c);
                        }

                        break;
                    }
                    case '\'':
                    case '"': {
                        token = LexString(false);

                        break;
                    }
                    case '{': {
                        if (Advance() == '"') {
                            token = LexString(true);
                        } else {
                            token = CreateToken(TokenType.DM_Preproc_Punctuator, c);
                        }

                        break;
                    }
                    case '#': {
                        StringBuilder textBuilder = new StringBuilder(Convert.ToString(c));
                        while ((IsAlphabetic(Advance()) ||GetCurrent() == '_' || GetCurrent() == '#') && !IsAtEndOfFile()) {
                            textBuilder.Append(GetCurrent());
                        }

                        string text = textBuilder.ToString();
                        if (text == "#include") {
                            token = CreateToken(TokenType.DM_Preproc_Include, text);
                        } else if (text == "#define") {
                            token = CreateToken(TokenType.DM_Preproc_Define, text);
                        } else if (text == "#undef") {
                            token = CreateToken(TokenType.DM_Preproc_Undefine, text);
                        } else if (text.StartsWith("##")) {
                            token = CreateToken(TokenType.DM_Preproc_TokenConcat, text, text.Substring(2));
                        } else {
                            token = CreateToken(TokenType.DM_Preproc_ParameterStringify, text, text.Substring(1));
                        }

                        break;
                    }
                    default: {
                        if (IsAlphabetic(c) || c == '_') {
                            StringBuilder textBuilder = new StringBuilder(Convert.ToString(c));
                            while ((IsAlphanumeric(Advance()) || GetCurrent() == '_') && !IsAtEndOfFile()) textBuilder.Append(GetCurrent());

                            token = CreateToken(TokenType.DM_Preproc_Identifier, textBuilder.ToString());
                        } else if (IsNumeric(c) || c == '.') {
                            StringBuilder textBuilder = new StringBuilder(Convert.ToString(c));

                            if (c == '.') {
                                c = Advance();

                                if (!IsNumeric(c)) token = CreateToken(TokenType.DM_Preproc_Punctuator_Period, '.');
                                else textBuilder.Append(c);
                            }

                            if (IsNumeric(c)) {
                                while (!IsAtEndOfFile()) {
                                    c = Advance();

                                    if (IsNumeric(c) || c == 'e' || c == 'E' || c == 'p' || c == 'P') {
                                        textBuilder.Append(c);
                                    } else {
                                        break;
                                    }
                                }

                                token = CreateToken(TokenType.DM_Preproc_Number, textBuilder.ToString());
                            }
                        } else {
                            Advance();
                        }

                        break;
                    }
                }
            }

            return token;
        }

        private Token LexString(bool isLong) {
            char terminator = GetCurrent();
            StringBuilder textBuilder = new StringBuilder(isLong ? "{" + terminator : Convert.ToString(terminator));
            Queue<Token> stringTokens = new();

            Advance();
            while (!(!isLong && GetCurrent() == '\n') && !IsAtEndOfFile()) {
                char stringC = GetCurrent();

                textBuilder.Append(stringC);
                if (stringC == '[') {
                    stringTokens.Enqueue(CreateToken(TokenType.DM_Preproc_String, textBuilder.ToString()));
                    textBuilder.Clear();

                    Advance();

                    Token exprToken = GetNextToken();
                    int bracketNesting = 0;
                    while (!(bracketNesting == 0 && exprToken.Type == TokenType.DM_Preproc_Punctuator_RightBracket) && !IsAtEndOfFile()) {
                        stringTokens.Enqueue(exprToken);

                        if (exprToken.Type == TokenType.DM_Preproc_Punctuator_LeftBracket) bracketNesting++;
                        if (exprToken.Type == TokenType.DM_Preproc_Punctuator_RightBracket) bracketNesting--;
                        exprToken = GetNextToken();
                    }

                    if (exprToken.Type != TokenType.DM_Preproc_Punctuator_RightBracket) throw new Exception("Expected ']' to end expression");
                    textBuilder.Append(']');
                } else if (stringC == '\\') {
                    Advance();
                    textBuilder.Append(GetCurrent());
                    Advance();
                } else if (stringC == terminator) {
                    if (isLong) {
                        stringC = Advance();

                        if (stringC == '}') {
                            textBuilder.Append('}');

                            break;
                        }
                    } else {
                        break;
                    }
                } else {
                    Advance();
                }
            }

            Advance();

            string text = textBuilder.ToString();
            if (!isLong && !text.EndsWith(terminator)) throw new Exception("Expected '" + terminator + "' to end string");
            else if (isLong && !text.EndsWith("}")) throw new Exception("Expected '}' to end long string");

            if (stringTokens.Count == 0) {
                return CreateToken(TokenType.DM_Preproc_ConstantString, text, text.Substring(1, text.Length - 2));
            } else {
                stringTokens.Enqueue(CreateToken(TokenType.DM_Preproc_String, textBuilder.ToString()));

                foreach (Token stringToken in stringTokens) {
                    _pendingTokenQueue.Enqueue(stringToken);
                }

                return CreateToken(TokenType.Skip, null);
            }
        }
    }
}
