using System;
using System.Collections.Generic;
using System.Text;
using OpenDreamShared.Compiler;

namespace DMCompiler.Compiler.DMPreprocessor {
    class DMPreprocessorLexer : TextLexer {
        public DMPreprocessorLexer(string sourceName, string source) : base(sourceName, source) { }

        public Token GetNextTokenIgnoringWhitespace() {
            Token nextToken = GetNextToken();
            while (nextToken.Type == TokenType.DM_Preproc_Whitespace) nextToken = GetNextToken();

            return nextToken;
        }

        protected override Token ParseNextToken() {
            Token token = base.ParseNextToken();

            if (token == null) {
                char c = GetCurrent();

                switch (c) {
                    case ' ':
                    case '\t': token = CreateToken(TokenType.DM_Preproc_Whitespace, c); Advance(); break;
                    case '}':
                    case ';': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator, c); break;
                    case '.': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator_Period, c); break;
                    case ':': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator_Colon, c); break;
                    case ',': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator_Comma, c); break;
                    case '(': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator_LeftParenthesis, c); break;
                    case ')': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator_RightParenthesis, c); break;
                    case '[': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator_LeftBracket, c); break;
                    case ']': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator_RightBracket, c); break;
                    case '?': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator_Question, c); break;
                    case '\\': {
                        //An escaped identifier.
                        //The next character turns into an identifier.
                        token = CreateToken(TokenType.DM_Preproc_Identifier, Advance());

                        Advance();
                        break;
                    }
                    case '>': {
                        switch (Advance()) {
                            case '>': {
                                if (Advance() == '=') {
                                    Advance();

                                    token = CreateToken(TokenType.DM_Preproc_Punctuator, ">>=");
                                } else {
                                    token = CreateToken(TokenType.DM_Preproc_Punctuator, ">>");
                                }

                                break;
                            }
                            case '=': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator, ">="); break;
                            default: token = CreateToken(TokenType.DM_Preproc_Punctuator, '>'); break;
                        }

                        break;
                    }
                    case '<': {
                        switch (Advance()) {
                            case '<': {
                                if (Advance() == '=') {
                                    Advance();

                                    token = CreateToken(TokenType.DM_Preproc_Punctuator, "<<=");
                                } else {
                                    token = CreateToken(TokenType.DM_Preproc_Punctuator, "<<");
                                }

                                break;
                            }
                            case '=': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator, "<="); break;
                            default: token = CreateToken(TokenType.DM_Preproc_Punctuator, '<'); break;
                        }

                        break;
                    }
                    case '|': {
                        switch (Advance()) {
                            case '|': {
                                switch (Advance()) {
                                    case '=': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator, "||="); break;
                                    default: token = CreateToken(TokenType.DM_Preproc_Punctuator, "||"); break;
                                }
                                break;
                            }
                            case '=': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator, "|="); break;
                            default: token = CreateToken(TokenType.DM_Preproc_Punctuator, '|'); break;
                        }

                        break;
                    }
                    case '*': {
                        switch (Advance()) {
                            case '*': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator, "**"); break;
                            case '=': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator, "*="); break;
                            default: token = CreateToken(TokenType.DM_Preproc_Punctuator, '*'); break;
                        }

                        break;
                    }
                    case '+': {
                        switch (Advance()) {
                            case '+': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator, "++"); break;
                            case '=': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator, "+="); break;
                            default: token = CreateToken(TokenType.DM_Preproc_Punctuator, '+'); break;
                        }

                        break;
                    }
                    case '&': {
                        switch (Advance()) {
                            case '&': {
                                switch (Advance()) {
                                    case '=': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator, "&&="); break;
                                    default: token = CreateToken(TokenType.DM_Preproc_Punctuator, "&&"); break;
                                }
                                break;
                            }
                            case '=': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator, "&="); break;
                            default: token = CreateToken(TokenType.DM_Preproc_Punctuator, '&'); break;
                        }

                        break;
                    }
                    case '~': {
                        switch (Advance()) {
                            case '=': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator, "~="); break;
                            case '!': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator, "~!"); break;
                            default: token = CreateToken(TokenType.DM_Preproc_Punctuator, '~'); break;
                        }

                        break;
                    }
                    case '%': {
                        switch (Advance()) {
                            case '=': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator, "%="); break;
                            default: token = CreateToken(TokenType.DM_Preproc_Punctuator, '%'); break;
                        }

                        break;
                    }
                    case '^': {
                        switch (Advance()) {
                            case '=': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator, "^="); break;
                            default: token = CreateToken(TokenType.DM_Preproc_Punctuator, '^'); break;
                        }

                        break;
                    }
                    case '!': {
                        switch (Advance()) {
                            case '=': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator, "!="); break;
                            default: token = CreateToken(TokenType.DM_Preproc_Punctuator, '!'); break;
                        }

                        break;
                    }
                    case '=': {
                        switch (Advance()) {
                            case '=': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator, "=="); break;
                            default: token = CreateToken(TokenType.DM_Preproc_Punctuator, '='); break;
                        }

                        break;
                    }
                    case '-': {
                        switch (Advance()) {
                            case '-': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator, "--"); break;
                            case '=': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator, "-="); break;
                            default: token = CreateToken(TokenType.DM_Preproc_Punctuator, '-'); break;
                        }

                        break;
                    }
                    case '/': {
                        switch (Advance()) {
                            case '/': {
                                while (Advance() != '\n' && !AtEndOfSource) {
                                }

                                token = CreateToken(TokenType.Skip, "//");
                                break;
                            }
                            case '*': {
                                //Skip everything up to the "*/"
                                Advance();
                                var comment_depth = 1;
                                while (comment_depth > 0) {
                                    if (GetCurrent() == '/') {
                                        Advance();
                                        if (GetCurrent() == '*') {
                                            // We found another comment - up the nest count
                                            comment_depth++;
                                            Advance();
                                        } else if (GetCurrent() == '/') {
                                            // Encountered a line comment - skip to end of line
                                            while (Advance() != '\n' && !AtEndOfSource) ;
                                        }
                                    } else if (GetCurrent() == '*') {
                                        if (Advance() == '/') {
                                            // End of comment - decrease nest count
                                            comment_depth--;
                                            Advance();
                                        }
                                    } else if (AtEndOfSource) {
                                        return CreateToken(TokenType.Error, null, "Expected \"*/\" to end multiline comment");
                                    } else {
                                        Advance();
                                    }
                                }
                                
                                while (GetCurrent() == ' ' || GetCurrent() == '\t') {
                                    Advance();
                                }

                                token = CreateToken(TokenType.Skip, "/* */");
                                break;
                            }
                            case '=': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator, "/="); break;
                            default: token = CreateToken(TokenType.DM_Preproc_Punctuator, c); break;
                        }

                        break;
                    }
                    case '@': { //Raw string
                        char delimiter = Advance();
                        StringBuilder textBuilder = new StringBuilder();

                        textBuilder.Append('@');
                        textBuilder.Append(delimiter);

                        bool isLong = false;
                        c = Advance();
                        if (delimiter == '{') {
                            textBuilder.Append(c);

                            if (c == '"') isLong = true;
                        }

                        if (isLong) {
                            do {
                                c = Advance();

                                textBuilder.Append(c);
                                if (c == '"') {
                                    c = Advance();
                                    if (c == '}') break;
                                }
                            } while (!AtEndOfSource);
                        } else {
                            while (c != delimiter && c != '\n' && !AtEndOfSource) {
                                textBuilder.Append(c);
                                c = Advance();
                            }
                        }

                        textBuilder.Append(c);
                        Advance();

                        string text = textBuilder.ToString();
                        string value = isLong ? text.Substring(3, text.Length - 5) : text.Substring(2, text.Length - 3);
                        token = CreateToken(TokenType.DM_Preproc_ConstantString, text, value);
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
                        StringBuilder textBuilder = new StringBuilder(char.ToString(c));
                        while ((IsAlphabetic(Advance()) ||GetCurrent() == '_' || GetCurrent() == '#') && !AtEndOfSource) {
                            textBuilder.Append(GetCurrent());
                        }

                        string text = textBuilder.ToString();
                        switch (text) {
                            case "#include": token = CreateToken(TokenType.DM_Preproc_Include, text); break;
                            case "#define": token = CreateToken(TokenType.DM_Preproc_Define, text); break;
                            case "#undef": token = CreateToken(TokenType.DM_Preproc_Undefine, text); break;
                            case "#if": token = CreateToken(TokenType.DM_Preproc_If, text); break;
                            case "#ifdef": token = CreateToken(TokenType.DM_Preproc_Ifdef, text); break;
                            case "#ifndef": token = CreateToken(TokenType.DM_Preproc_Ifndef, text); break;
                            case "#else": token = CreateToken(TokenType.DM_Preproc_Else, text); break;
                            case "#endif": token = CreateToken(TokenType.DM_Preproc_EndIf, text); break;
                            case "#error": token = CreateToken(TokenType.DM_Preproc_Error, text); break;
                            case "#warn":
                            case "#warning": token = CreateToken(TokenType.DM_Preproc_Warning, text); break;
                            default: {
                                if (text.StartsWith("##")) {
                                    token = CreateToken(TokenType.DM_Preproc_TokenConcat, text, text.Substring(2));
                                } else {
                                    token = CreateToken(TokenType.DM_Preproc_ParameterStringify, text, text.Substring(1));
                                }

                                break;
                            }
                        }

                        break;
                    }
                    default: {
                        if (IsAlphabetic(c) || c == '_') {
                            StringBuilder textBuilder = new StringBuilder(char.ToString(c));
                            while ((IsAlphanumeric(Advance()) || GetCurrent() == '_') && !AtEndOfSource) textBuilder.Append(GetCurrent());

                            token = CreateToken(TokenType.DM_Preproc_Identifier, textBuilder.ToString());
                        } else if (IsNumeric(c)) {
                            StringBuilder textBuilder = new StringBuilder(char.ToString(c));
                            bool error = false;

                            while (!AtEndOfSource) {
                                char next = Advance();
                                if ((c == 'e' || c == 'E') && (next == '-' || next == '+')) { //1e-10 or 1e+10
                                    textBuilder.Append(next);
                                    next = Advance();
                                } else if (c == '#' && next == 'I') { //1.#INF and 1.#IND
                                    if (Advance() != 'N' || Advance() != 'F' && GetCurrent() != 'D') {
                                        error = true;

                                        break;
                                    }

                                    textBuilder.Append("IN");
                                    textBuilder.Append(GetCurrent());
                                    next = Advance();
                                }

                                c = next;
                                if (IsHex(c) || c == '.' || c == 'x' || c == '#' || c == 'e' || c == 'E' || c == 'p' || c == 'P') {
                                    textBuilder.Append(c);
                                } else {
                                    break;
                                }
                            }

                            if (!error) token = CreateToken(TokenType.DM_Preproc_Number, textBuilder.ToString());
                            else token = CreateToken(TokenType.Error, null, "Invalid number");
                        } else {
                            Advance();
                            token = CreateToken(TokenType.Error, null, $"Unknown character: {c.ToString()}");
                        }

                        break;
                    }
                }
            }

            return token;
        }

        protected override char Advance() {
            char current = base.Advance();
            if (current == '\\') {
                if (_source[_currentPosition] == '\n') { //Skip a newline if it comes after a backslash
                    base.Advance();

                    current = Advance();
                    while (current == ' ' || current == '\t' || current == '\n')
                    {
                        current = Advance();
                    }
                }
            }

            return current;
        }

        //Lexes a string
        //If it contains string interpolations, it splits the string tokens into parts and lexes the expressions as normal
        //For example, "There are [amount] of them" becomes:
        //    DM_Preproc_String("There are "), DM_Preproc_Identifier(amount), DM_Preproc_String(" of them")
        //If there is no string interpolation, it outputs a DM_Preproc_ConstantString token instead
        private Token LexString(bool isLong) {
            char terminator = GetCurrent();
            StringBuilder textBuilder = new StringBuilder(isLong ? "{" + terminator : char.ToString(terminator));
            Queue<Token> stringTokens = new();

            Advance();
            while (!(!isLong && GetCurrent() == '\n') && !AtEndOfSource) {
                char stringC = GetCurrent();

                textBuilder.Append(stringC);
                if (stringC == '[') {
                    stringTokens.Enqueue(CreateToken(TokenType.DM_Preproc_String, textBuilder.ToString()));
                    textBuilder.Clear();

                    Advance();

                    Token exprToken = GetNextToken();
                    int bracketNesting = 0;
                    while (!(bracketNesting == 0 && exprToken.Type == TokenType.DM_Preproc_Punctuator_RightBracket) && !AtEndOfSource) {
                        stringTokens.Enqueue(exprToken);

                        if (exprToken.Type == TokenType.DM_Preproc_Punctuator_LeftBracket) bracketNesting++;
                        if (exprToken.Type == TokenType.DM_Preproc_Punctuator_RightBracket) bracketNesting--;
                        exprToken = GetNextToken();
                    }

                    if (exprToken.Type != TokenType.DM_Preproc_Punctuator_RightBracket) return CreateToken(TokenType.Error, null, "Expected ']' to end expression");
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
            if (!isLong && !(text.EndsWith(terminator) && text.Length != 1)) return CreateToken(TokenType.Error, null, "Expected '" + terminator + "' to end string");
            else if (isLong && !text.EndsWith("}")) return CreateToken(TokenType.Error, null, "Expected '}' to end long string");

            if (stringTokens.Count == 0) {
                string stringValue = isLong ? text.Substring(2, text.Length - 4) : text.Substring(1, text.Length - 2);

                return CreateToken(TokenType.DM_Preproc_ConstantString, text, stringValue);
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
