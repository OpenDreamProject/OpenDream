using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace DMCompiler.Compiler.DMPreprocessor;

/// <summary>
/// This class acts as the first layer of digestion for the compiler, <br/>
/// taking in raw text and outputting vague tokens descriptive enough for the preprocessor to run on them.
/// </summary>
internal sealed class DMPreprocessorLexer {
    private static readonly StringBuilder TokenTextBuilder = new();

    public readonly string IncludeDirectory;
    public readonly string File;

    private readonly StreamReader _source;
    private char _current;
    private int _currentLine = 1, _currentColumn;
    private readonly Queue<Token> _pendingTokenQueue = new(); // TODO: Possible to remove this?

    public DMPreprocessorLexer(string includeDirectory, string file, string source) {
        IncludeDirectory = includeDirectory;
        File = file;

        _source = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(source)), Encoding.UTF8);
        Advance();
    }

    public DMPreprocessorLexer(string includeDirectory, string file) {
        IncludeDirectory = includeDirectory;
        File = file;

        _source = new StreamReader(Path.Combine(includeDirectory, file), Encoding.UTF8);
        Advance();
    }

    public Token NextToken(bool ignoreWhitespace = false) {
        if (_pendingTokenQueue.Count > 0) {
            Token token = _pendingTokenQueue.Dequeue();
            if (ignoreWhitespace) {
                do {
                    if (token.Type != TokenType.DM_Preproc_Whitespace)
                        return token;

                    if (_pendingTokenQueue.Count == 0)
                        break;

                    token = _pendingTokenQueue.Dequeue();
                } while (true);
            } else {
                return token;
            }
        }

        char c = GetCurrent();

        switch (c) {
            case '\0':
                return CreateToken(TokenType.EndOfFile, c);
            case '\r':
            case '\n':
                HandleLineEnd();

                return CreateToken(TokenType.Newline, "\n");
            case ' ':
            case '\t':
                int whitespaceLength = 1;
                while (Advance() is ' ' or '\t')
                    whitespaceLength++;

                return ignoreWhitespace
                    ? NextToken()
                    : CreateToken(TokenType.DM_Preproc_Whitespace, new string(c, whitespaceLength));
            case '}': Advance(); return CreateToken(TokenType.DM_Preproc_Punctuator, c);
            case ';': Advance(); return CreateToken(TokenType.DM_Preproc_Punctuator_Semicolon, c);
            case '.': Advance(); return CreateToken(TokenType.DM_Preproc_Punctuator_Period, c);
            case ':':
                switch (Advance()) {
                    case '=':
                        Advance();
                        return CreateToken(TokenType.DM_Preproc_Punctuator, ":=");
                    case ':':
                        Advance();
                        return CreateToken(TokenType.DM_Preproc_Punctuator, "::");
                    default:
                        return CreateToken(TokenType.DM_Preproc_Punctuator_Colon, c);
                }
            case ',': Advance(); return CreateToken(TokenType.DM_Preproc_Punctuator_Comma, c);
            case '(': Advance(); return CreateToken(TokenType.DM_Preproc_Punctuator_LeftParenthesis, c);
            case ')': Advance(); return CreateToken(TokenType.DM_Preproc_Punctuator_RightParenthesis, c);
            case '[': {
                if (Advance() == ']') {
                    if (Advance() == '=') {
                        Advance();
                        return CreateToken(TokenType.DM_Preproc_Punctuator, "[]=");
                    }

                    return CreateToken(TokenType.DM_Preproc_Punctuator, "[]");
                }

                return CreateToken(TokenType.DM_Preproc_Punctuator_LeftBracket, c);
            }
            case ']': Advance(); return CreateToken(TokenType.DM_Preproc_Punctuator_RightBracket, c);
            case '?': Advance(); return CreateToken(TokenType.DM_Preproc_Punctuator_Question, c);
            case '\\': {
                c = Advance();

                if (HandleLineEnd()) {
                    return CreateToken(TokenType.DM_Preproc_LineSplice, c);
                }

                //An escaped identifier.
                //The next character turns into an identifier.
                Advance();
                return CreateToken(TokenType.DM_Preproc_Identifier, c);
            }
            case '>': {
                switch (Advance()) {
                    case '>': {
                        if (Advance() == '=') {
                            Advance();

                            return CreateToken(TokenType.DM_Preproc_Punctuator, ">>=");
                        }

                        return CreateToken(TokenType.DM_Preproc_Punctuator, ">>");
                    }
                    case '=': Advance(); return CreateToken(TokenType.DM_Preproc_Punctuator, ">=");
                    default: return CreateToken(TokenType.DM_Preproc_Punctuator, '>');
                }
            }
            case '<': {
                switch (Advance()) {
                    case '<': {
                        if (Advance() == '=') {
                            Advance();

                            return CreateToken(TokenType.DM_Preproc_Punctuator, "<<=");
                        }

                        return CreateToken(TokenType.DM_Preproc_Punctuator, "<<");
                    }
                    case '=': Advance(); return CreateToken(TokenType.DM_Preproc_Punctuator, "<=");
                    case '>': Advance(); return CreateToken(TokenType.DM_Preproc_Punctuator, "<>");
                    default: return CreateToken(TokenType.DM_Preproc_Punctuator, '<');
                }
            }
            case '|': {
                switch (Advance()) {
                    case '|': {
                        switch (Advance()) {
                            case '=': Advance(); return CreateToken(TokenType.DM_Preproc_Punctuator, "||=");
                            default: return CreateToken(TokenType.DM_Preproc_Punctuator, "||");
                        }
                    }
                    case '=': Advance(); return CreateToken(TokenType.DM_Preproc_Punctuator, "|=");
                    default: return CreateToken(TokenType.DM_Preproc_Punctuator, '|');
                }
            }
            case '*': {
                switch (Advance()) {
                    case '*': Advance(); return CreateToken(TokenType.DM_Preproc_Punctuator, "**");
                    case '=': Advance(); return CreateToken(TokenType.DM_Preproc_Punctuator, "*=");
                    default: return CreateToken(TokenType.DM_Preproc_Punctuator, '*');
                }
            }
            case '+': {
                switch (Advance()) {
                    case '+': Advance(); return CreateToken(TokenType.DM_Preproc_Punctuator, "++");
                    case '=': Advance(); return CreateToken(TokenType.DM_Preproc_Punctuator, "+=");
                    default: return CreateToken(TokenType.DM_Preproc_Punctuator, '+');
                }
            }
            case '&': {
                switch (Advance()) {
                    case '&': {
                        switch (Advance()) {
                            case '=': Advance(); return CreateToken(TokenType.DM_Preproc_Punctuator, "&&=");
                            default: return CreateToken(TokenType.DM_Preproc_Punctuator, "&&");
                        }
                    }
                    case '=': Advance(); return CreateToken(TokenType.DM_Preproc_Punctuator, "&=");
                    default: return CreateToken(TokenType.DM_Preproc_Punctuator, '&');
                }
            }
            case '~': {
                switch (Advance()) {
                    case '=': Advance(); return CreateToken(TokenType.DM_Preproc_Punctuator, "~=");
                    case '!': Advance(); return CreateToken(TokenType.DM_Preproc_Punctuator, "~!");
                    default: return CreateToken(TokenType.DM_Preproc_Punctuator, '~');
                }
            }
            case '%': {
                switch (Advance()) {
                    case '=': Advance(); return CreateToken(TokenType.DM_Preproc_Punctuator, "%=");
                    case '%': {
                        if (Advance() == '=') {
                            Advance();
                            return CreateToken(TokenType.DM_Preproc_Punctuator, "%%=");
                        }

                        return CreateToken(TokenType.DM_Preproc_Punctuator, "%%");
                    }
                    default: return CreateToken(TokenType.DM_Preproc_Punctuator, '%');
                }
            }
            case '^': {
                switch (Advance()) {
                    case '=': Advance(); return CreateToken(TokenType.DM_Preproc_Punctuator, "^=");
                    default: return CreateToken(TokenType.DM_Preproc_Punctuator, '^');
                }
            }
            case '!': {
                switch (Advance()) {
                    case '=': Advance(); return CreateToken(TokenType.DM_Preproc_Punctuator, "!=");
                    default: return CreateToken(TokenType.DM_Preproc_Punctuator, '!');
                }
            }
            case '=': {
                switch (Advance()) {
                    case '=': Advance(); return CreateToken(TokenType.DM_Preproc_Punctuator, "==");
                    default: return CreateToken(TokenType.DM_Preproc_Punctuator, '=');
                }
            }
            case '-': {
                switch (Advance()) {
                    case '-': Advance(); return CreateToken(TokenType.DM_Preproc_Punctuator, "--");
                    case '=': Advance(); return CreateToken(TokenType.DM_Preproc_Punctuator, "-=");
                    default: return CreateToken(TokenType.DM_Preproc_Punctuator, '-');
                }
            }
            case '/': {
                switch (Advance()) {
                    case '/': {
                        do {
                            Advance();

                            if (c == '\\') {
                                Advance();

                                if (HandleLineEnd()) { //Line splice within a comment
                                    do {
                                        Advance();
                                    } while (GetCurrent() is ' ' or '\t' || HandleLineEnd());
                                }
                            }
                        } while (!AtLineEnd() && !AtEndOfSource());

                        return NextToken(ignoreWhitespace);
                    }
                    case '*': {
                        //Skip everything up to the "*/"
                        Advance();
                        var commentDepth = 1;
                        while (commentDepth > 0) {
                            if (GetCurrent() == '/') {
                                if (Advance() == '*') {
                                    // We found another comment - up the nest count
                                    commentDepth++;
                                    Advance();
                                } else if (GetCurrent() == '/') {
                                    // Encountered a line comment - skip to end of line
                                    do {
                                        Advance();
                                    } while (!AtLineEnd() && !AtEndOfSource());
                                }
                            } else if (GetCurrent() == '*') {
                                if (Advance() == '/') {
                                    // End of comment - decrease nest count
                                    commentDepth--;
                                    Advance();
                                }
                            } else if (AtEndOfSource()) {
                                return CreateToken(TokenType.Error, string.Empty,
                                    "Expected \"*/\" to end multiline comment");
                            } else if (!HandleLineEnd()) {
                                Advance();
                            }
                        }

                        while (GetCurrent() == ' ' || GetCurrent() == '\t') {
                            Advance();
                        }

                        return NextToken(ignoreWhitespace);
                    }
                    case '=': Advance(); return CreateToken(TokenType.DM_Preproc_Punctuator, "/=");
                    default: return CreateToken(TokenType.DM_Preproc_Punctuator, c);
                }
            }
            case '@': { //Raw string
                char delimiter = Advance();

                TokenTextBuilder.Clear();
                TokenTextBuilder.Append('@');
                TokenTextBuilder.Append(delimiter);

                bool isLong = false;
                c = Advance();
                if (delimiter == '{') {
                    TokenTextBuilder.Append(c);

                    if (c == '"') isLong = true;
                }

                if (isLong) {
                    c = Advance();
                    HandleLineEnd(); // A newline immediately following @{" is not appended
                    TokenTextBuilder.Append(GetCurrent()); // Since we're about to Advance() again we need to append what we have

                    bool nextCharCanTerm = false;
                    do {
                        c = Advance();

                        if (nextCharCanTerm && c == '}') {
                            break;
                        }

                        if (HandleLineEnd()) {
                            TokenTextBuilder.Append('\n');
                            c = GetCurrent();
                        }

                        TokenTextBuilder.Append(c);
                        nextCharCanTerm = c == '"';
                    } while (!AtEndOfSource());
                } else {
                    while (c != delimiter && !AtLineEnd() && !AtEndOfSource()) {
                        TokenTextBuilder.Append(c);
                        c = Advance();
                    }
                }

                TokenTextBuilder.Append(c);
                if (!HandleLineEnd())
                    Advance();

                string text = TokenTextBuilder.ToString();
                var lengthMinusFinalNewline = text[text.Length - 3] == '\n' ? 6 : 5;
                string value = isLong ? text.Substring(3, text.Length - lengthMinusFinalNewline) : text.Substring(2, text.Length - 3);
                return CreateToken(TokenType.DM_Preproc_ConstantString, text, value);
            }
            case '\'':
            case '"':
                return LexString(false);
            case '{':
                return Advance() == '"' ?
                    LexString(true) :
                    CreateToken(TokenType.DM_Preproc_Punctuator, c);
            case '#': {
                bool isConcat = (Advance() == '#');
                if (isConcat) Advance();

                // Whitespace after '#' is ignored
                while (GetCurrent() is ' ' or '\t') {
                    Advance();
                }

                TokenTextBuilder.Clear();
                while (char.IsAsciiLetter(GetCurrent()) || GetCurrent() == '_') {
                    TokenTextBuilder.Append(GetCurrent());
                    Advance();
                }

                string text = TokenTextBuilder.ToString();
                if (text == string.Empty) {
                    return NextToken(ignoreWhitespace); // Skip this token
                } else if (isConcat) {
                    return CreateToken(TokenType.DM_Preproc_TokenConcat, $"##{text}", text);
                }

                if (TryMacroKeyword(text, out var macroKeyword))
                    return macroKeyword.Value;

                string macroAttempt = text.ToLower();
                if (TryMacroKeyword(macroAttempt, out var attemptKeyword)) { // if they mis-capitalized the keyword
                    DMCompiler.Emit(WarningCode.MiscapitalizedDirective, attemptKeyword.Value.Location,
                        $"#{text} is not a valid macro keyword. Did you mean '#{macroAttempt}'?");
                }

                return CreateToken(TokenType.DM_Preproc_ParameterStringify, $"#{text}", text);
            }
            default: {
                if (char.IsAsciiLetter(c) || c == '_') {
                    TokenTextBuilder.Clear();
                    TokenTextBuilder.Append(c);
                    while ((char.IsAsciiLetterOrDigit(Advance()) || GetCurrent() == '_') && !AtEndOfSource())
                        TokenTextBuilder.Append(GetCurrent());

                    return CreateToken(TokenType.DM_Preproc_Identifier, TokenTextBuilder.ToString());
                } else if (char.IsAsciiDigit(c)) {
                    bool error = false;

                    TokenTextBuilder.Clear();
                    TokenTextBuilder.Append(c);

                    while (!AtEndOfSource()) {
                        char next = Advance();
                        if ((c == 'e' || c == 'E') && (next == '-' || next == '+')) { //1e-10 or 1e+10
                            TokenTextBuilder.Append(next);
                            next = Advance();
                        } else if (c == '#' && next == 'I') { //1.#INF and 1.#IND
                            if (Advance() != 'N' || Advance() != 'F' && GetCurrent() != 'D') {
                                error = true;

                                break;
                            }

                            TokenTextBuilder.Append("IN");
                            TokenTextBuilder.Append(GetCurrent());
                            next = Advance();
                        }

                        c = next;
                        if (char.IsAsciiHexDigit(c) || c == '.' || c == 'x' || c == '#' || c == 'e' || c == 'E' || c == 'p' || c == 'P') {
                            TokenTextBuilder.Append(c);
                        } else {
                            break;
                        }
                    }

                    return error
                        ? CreateToken(TokenType.Error, string.Empty, "Invalid number")
                        : CreateToken(TokenType.DM_Preproc_Number, TokenTextBuilder.ToString());
                }

                Advance();
                return CreateToken(TokenType.Error, string.Empty, $"Unknown character: {c.ToString()}");
            }
        }
    }

    /// <returns>True if token was successfully set to a macro keyword token, false if not.</returns>
    private bool TryMacroKeyword(string text, [NotNullWhen(true)] out Token? token) {
        switch (text) {
            case "warn":
            case "warning": {
                TokenTextBuilder.Clear();
                while (!AtEndOfSource() && !AtLineEnd()) {
                    TokenTextBuilder.Append(GetCurrent());
                    Advance();
                }

                token = CreateToken(TokenType.DM_Preproc_Warning, "#warn" + TokenTextBuilder);
                break;
            }
            case "error": {
                TokenTextBuilder.Clear();
                while (!AtEndOfSource() && !AtLineEnd()) {
                    TokenTextBuilder.Append(GetCurrent());
                    Advance();
                }

                token = CreateToken(TokenType.DM_Preproc_Error, "#error" + TokenTextBuilder);
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
    /// Lex a string <br/>
    ///</summary>
    ///<remarks>
    /// If it contains string interpolations, it splits the string tokens into parts and lex the expressions as normal <br/>
    /// For example, "There are [amount] of them" becomes: <br/>
    ///    DM_Preproc_StringBegin("There are "), DM_Preproc_Identifier(amount), DM_Preproc_StringEnd(" of them") <br/>
    /// If there is no string interpolation, it outputs a DM_Preproc_ConstantString token instead
    /// </remarks>
    private Token LexString(bool isLong) {
        char terminator = GetCurrent();
        StringBuilder textBuilder = new StringBuilder();
        Queue<Token> stringTokens = new();
        string tokenTextStart = isLong ? "{" + terminator : char.ToString(terminator);
        string tokenTextEnd = isLong ? terminator + "}" : char.ToString(terminator);
        bool isConstant = true;
        bool foundTerminator = false;

        Advance();
        while (!(!isLong && AtLineEnd()) && !AtEndOfSource()) {
            char stringC = GetCurrent();

            if (HandleLineEnd()) {
                textBuilder.Append('\n');
            } else if (stringC == '[') {
                textBuilder.Append(stringC);
                stringTokens.Enqueue(isConstant // First case of '['
                    ? CreateToken(TokenType.DM_Preproc_StringBegin, tokenTextStart + textBuilder, textBuilder.ToString())
                    : CreateToken(TokenType.DM_Preproc_StringMiddle, textBuilder.ToString(), textBuilder.ToString()));

                isConstant = false;
                textBuilder.Clear();

                Advance();

                Token exprToken = NextToken();
                int bracketNesting = 0;
                while (!(bracketNesting == 0 && exprToken.Type == TokenType.DM_Preproc_Punctuator_RightBracket) && !AtEndOfSource()) {
                    stringTokens.Enqueue(exprToken);

                    if (exprToken.Type == TokenType.DM_Preproc_Punctuator_LeftBracket) bracketNesting++;
                    if (exprToken.Type == TokenType.DM_Preproc_Punctuator_RightBracket) bracketNesting--;
                    exprToken = NextToken();
                }

                if (exprToken.Type != TokenType.DM_Preproc_Punctuator_RightBracket)
                    return CreateToken(TokenType.Error, string.Empty, "Expected ']' to end expression");
                textBuilder.Append(']');
            } else if (stringC == '\\') {
                Advance();

                if (HandleLineEnd()) { //Line splice
                    // Ignore newlines & all incoming whitespace
                    while (AtLineEnd() || GetCurrent() is ' ' or '\t') {
                        if (!HandleLineEnd())
                            Advance(); // Was a space or tab so advance it
                    }
                } else {
                    textBuilder.Append(stringC);
                    textBuilder.Append(GetCurrent());
                    Advance();
                }
            } else if (stringC == terminator) {
                if (isLong) {
                    if (Advance() == '}') {
                        foundTerminator = true;
                        break;
                    }

                    textBuilder.Append(stringC);
                } else {
                    foundTerminator = true;
                    break;
                }
            } else {
                if (stringC != '\r') // \r\n becomes \n
                    textBuilder.Append(stringC);

                Advance();
            }
        }

        if (!AtEndOfSource() && !HandleLineEnd())
            Advance();

        if (!isLong && !foundTerminator)
            return CreateToken(TokenType.Error, string.Empty, $"Expected '{terminator}' to end string");
        if (isLong && !foundTerminator)
            return CreateToken(TokenType.Error, string.Empty, "Expected '}' to end long string");

        var text = textBuilder.ToString();

        if (isConstant) {
            return CreateToken(TokenType.DM_Preproc_ConstantString, tokenTextStart + text + tokenTextEnd, text);
        } else {
            foreach (var token in stringTokens)
                _pendingTokenQueue.Enqueue(token);

            _pendingTokenQueue.Enqueue(CreateToken(TokenType.DM_Preproc_StringEnd, text + tokenTextEnd, text));

            return _pendingTokenQueue.Dequeue();
        }
    }

    /// <summary>
    /// Checks if the current position is at the end of a line (carriage return or newline)
    /// </summary>
    private bool AtLineEnd() {
        return GetCurrent() is '\r' or '\n';
    }

    /// <summary>
    /// Handles the end of a line by consuming all carriage returns before a newline or the absence of a newline
    /// </summary>
    /// <remarks>If you skip a line ending without using this, the line counter will be incorrect</remarks>
    /// <returns>True if this handled a line ending, otherwise false</returns>
    private bool HandleLineEnd() {
        char c = GetCurrent();

        switch (c) {
            case '\r':
                while (c == '\r')
                    c = Advance();

                goto case '\n';
            case '\n':
                _currentLine++;
                _currentColumn = 1;

                if (c == '\n') // This line could have ended with only \r
                    Advance();

                return true;
            default:
                return false; // No line ending
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private char GetCurrent() {
        return _current;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private char Advance() {
        int value = _source.Read();

        if (value == -1) {
            _current = '\0';
        }  else {
            _currentColumn++;
            _current = (char)value;
        }

        return GetCurrent();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool AtEndOfSource() {
        return _current == '\0';
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Token CreateToken(TokenType type, string text, object? value = null) {
        return new Token(type, text, new Location(File, _currentLine, _currentColumn), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Token CreateToken(TokenType type, char text, object? value = null) {
        return new Token(type, text.ToString(), new Location(File, _currentLine, _currentColumn), value);
    }
}
