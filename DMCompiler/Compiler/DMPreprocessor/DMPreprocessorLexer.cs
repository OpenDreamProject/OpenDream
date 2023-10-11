using System;
using System.Collections.Generic;
using System.Text;
using OpenDreamShared.Compiler;

namespace DMCompiler.Compiler.DMPreprocessor {
    /// <summary>
    /// This class acts as the first layer of digestion for the compiler, <br/>
    /// taking in raw text and outputting vague tokens descriptive enough for the preprocessor to run on them.
    /// </summary>
    internal sealed class DMPreprocessorLexer : TextLexer {
        public string IncludeDirectory;

        public DMPreprocessorLexer(string includeDirectory, string sourceName, string source) : base(sourceName, source) {
            IncludeDirectory = includeDirectory;
        }

        public Token GetNextTokenIgnoringWhitespace() {
            Token nextToken = GetNextToken();
            if (nextToken.Type == TokenType.DM_Preproc_Whitespace) nextToken = GetNextToken();

            return nextToken;
        }

        protected override Token ParseNextToken() {
            Token token = base.ParseNextToken();

            if (token.Type == TokenType.Unknown) {
                char c = GetCurrent();

                switch (c) {
                    case ' ':
                    case '\t':
                        int whitespaceLength = 1;
                        while (Advance() is ' ' or '\t')
                            whitespaceLength++;

                        token = CreateToken(TokenType.DM_Preproc_Whitespace, new string(c, whitespaceLength));
                        break;
                    case '}': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator, c); break;
                    case ';': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator_Semicolon, c); break;
                    case '.': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator_Period, c); break;
                    case ':':
                        if(Advance() == '=') {
                            Advance();
                            token = CreateToken(TokenType.DM_Preproc_Punctuator, ":=");
                        } else
                            token = CreateToken(TokenType.DM_Preproc_Punctuator_Colon, c);
                        break;
                    case ',': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator_Comma, c); break;
                    case '(': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator_LeftParenthesis, c); break;
                    case ')': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator_RightParenthesis, c); break;
                    case '[': {
                        if(Advance() == ']'){
                            if(Advance() == '=') {
                                Advance();
                                token = CreateToken(TokenType.DM_Preproc_Punctuator, "[]=");
                            } else
                                token = CreateToken(TokenType.DM_Preproc_Punctuator, "[]");
                        } else {
                            token = CreateToken(TokenType.DM_Preproc_Punctuator_LeftBracket, c);
                        }
                        break;
                    }
                    case ']': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator_RightBracket, c); break;
                    case '?': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator_Question, c); break;
                    case '\\': {
                        if (Advance() == '\n') {
                            token = CreateToken(TokenType.DM_Preproc_LineSplice, c);
                        } else {
                            //An escaped identifier.
                            //The next character turns into an identifier.
                            token = CreateToken(TokenType.DM_Preproc_Identifier, GetCurrent());
                        }

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
                            case '>': Advance(); token = CreateToken(TokenType.DM_Preproc_Punctuator, "<>"); break;
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
                            case '%': {
                                if (Advance() == '=') {
                                    Advance();
                                    token = CreateToken(TokenType.DM_Preproc_Punctuator, "%%=");
                                    break;
                                }
                                token = CreateToken(TokenType.DM_Preproc_Punctuator, "%%");
                                break;
                            }
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
                                do {
                                    Advance();

                                    if (GetCurrent() == '\\' && Advance() == '\n') { //Line splice within a comment
                                        do {
                                            Advance();
                                        } while (GetCurrent() is ' ' or '\t' or '\n');
                                    }
                                } while (GetCurrent() != '\n');

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
                            bool nextCharCanTerm = false;
                            do {
                                c = Advance();


                                if(nextCharCanTerm && c == '}')
                                    break;
                                else {
                                    textBuilder.Append(c);
                                    nextCharCanTerm = false;
                                }


                                if (c == '"')
                                    nextCharCanTerm = true;

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
                        bool isConcat = (Advance() == '#');
                        if (isConcat) Advance();

                        // Whitespace after '#' is ignored
                        while (GetCurrent() is ' ' or '\t') {
                            Advance();
                        }

                        StringBuilder textBuilder = new StringBuilder();
                        while (char.IsAsciiLetter(GetCurrent()) || GetCurrent() == '_') {
                            textBuilder.Append(GetCurrent());
                            Advance();
                        }

                        string text = textBuilder.ToString();
                        if (text == String.Empty) {
                            token = CreateToken(TokenType.Skip, isConcat ? "##" : "#");
                        } else if (isConcat) {
                            token = CreateToken(TokenType.DM_Preproc_TokenConcat, $"##{text}", text);
                        } else {
                            if(!TryMacroKeyword(text,out token)) { // if not macro (sets it here otherwise)
                                token = CreateToken(TokenType.DM_Preproc_ParameterStringify, $"#{text}", text);
                                string macroAttempt = text.ToLower();
                                if (TryMacroKeyword(macroAttempt, out _)) { // if they miscapitalized the keyword
                                    DMCompiler.Emit(WarningCode.MiscapitalizedDirective, token.Location, $"#{text} is not a valid macro keyword. Did you mean '#{macroAttempt}'?");
                                }
                            }
                        }

                        break;
                    }
                    default: {
                        if (char.IsAsciiLetter(c) || c == '_') {
                            StringBuilder textBuilder = new StringBuilder(char.ToString(c));
                            while ((char.IsAsciiLetterOrDigit(Advance()) || GetCurrent() == '_') && !AtEndOfSource) textBuilder.Append(GetCurrent());

                            token = CreateToken(TokenType.DM_Preproc_Identifier, textBuilder.ToString());
                        } else if (char.IsAsciiDigit(c)) {
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
                                if (char.IsAsciiHexDigit(c) || c == '.' || c == 'x' || c == '#' || c == 'e' || c == 'E' || c == 'p' || c == 'P') {
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

        /// <returns>True if token was successfully set to a macro keyword token, false if not.</returns>
        private bool TryMacroKeyword(string text, out Token token) {
            switch (text) {
                case "warn":
                case "warning": {
                    StringBuilder message = new StringBuilder();

                    while (GetCurrent() is not '\0' and not '\n') {
                        message.Append(GetCurrent());
                        Advance();
                    }

                    token = CreateToken(TokenType.DM_Preproc_Warning, "#warn" + message.ToString());
                    break;
                }
                case "error": {
                    StringBuilder message = new StringBuilder();

                    while (GetCurrent() is not '\0' and not '\n') {
                        message.Append(GetCurrent());
                        Advance();
                    }

                    token = CreateToken(TokenType.DM_Preproc_Error, "#error" + message.ToString());
                    break;
                }
                case "include": token = CreateToken(TokenType.DM_Preproc_Include, "#include"); break;
                case "define": token = CreateToken(TokenType.DM_Preproc_Define, "#define"); break;
                case "undef": token = CreateToken(TokenType.DM_Preproc_Undefine, "#undef"); break;
                case "if": token = CreateToken(TokenType.DM_Preproc_If, "#if"); break;
                case "ifdef": token = CreateToken(TokenType.DM_Preproc_Ifdef, "#ifdef"); break;
                case "ifndef": token = CreateToken(TokenType.DM_Preproc_Ifndef, "#ifndef"); break;
                case "elif": token = CreateToken(TokenType.DM_Preproc_Elif, "#elif"); break;
                case "else": token = CreateToken(TokenType.DM_Preproc_Else, "#else"); break;
                case "endif": token = CreateToken(TokenType.DM_Preproc_EndIf, "#endif"); break;
                //OD-specific directives
                case "pragma": token = CreateToken(TokenType.DM_Preproc_Pragma, "#pragma"); break;
                default:
                    token = null; // maybe should use ref instead of out?
                    return false;
            }
            return true;
        }


        ///<summary>
        /// Lexes a string <br/>
        ///</summary>
        ///<remarks>
        /// If it contains string interpolations, it splits the string tokens into parts and lexes the expressions as normal <br/>
        /// For example, "There are [amount] of them" becomes: <br/>
        ///    DM_Preproc_String("There are "), DM_Preproc_Identifier(amount), DM_Preproc_String(" of them") <br/>
        /// If there is no string interpolation, it outputs a DM_Preproc_ConstantString token instead
        /// </remarks>
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
                    if (Advance() == '\n') { //Line splice
                        //Remove the '\' from textBuilder and ignore newlines & all incoming whitespace
                        textBuilder.Remove(textBuilder.Length - 1, 1);
                        do {
                            Advance();
                        } while (GetCurrent() == '\n' || GetCurrent() == ' ' || GetCurrent() == '\t');
                    } else {
                        textBuilder.Append(GetCurrent());
                        Advance();
                    }
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
