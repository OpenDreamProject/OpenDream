namespace DMCompiler.Compiler;

internal class Parser<SourceType> {
    public DMCompiler Compiler;
    protected Lexer<SourceType> _lexer;
    private Token _currentToken;
    private readonly Stack<Token> _tokenStack = new(1);

    internal Parser(Lexer<SourceType> lexer) {
        Compiler = lexer.Compiler;
        _lexer = lexer;

        Advance();
    }

    /// <summary>
    /// Does not consume; this is simply a friendly getter.
    /// </summary>
    protected Token Current() {
        return _currentToken;
    }

    protected virtual Token Advance() {
        if (_tokenStack.Count > 0) {
            _currentToken = _tokenStack.Pop();
        } else {
            _currentToken = _lexer.GetNextToken();

            if (_currentToken.Type == TokenType.Error) {
                Emit(WarningCode.BadToken, _currentToken.ValueAsString());
                Advance();
            } else if (_currentToken.Type == TokenType.Warning) {
                Warning(_currentToken.ValueAsString());
                Advance();
            }
        }

        return Current();
    }

    protected void ReuseToken(Token token) {
        _tokenStack.Push(_currentToken);
        _currentToken = token;
    }

    protected bool Check(TokenType type) {
        if (Current().Type == type) {
            Advance();

            return true;
        }

        return false;
    }

    protected bool Check(Span<TokenType> types) {
        return Check(types, out _);
    }

    protected bool Check(Span<TokenType> types, out Token matchedToken) {
        TokenType currentType = Current().Type;
        foreach (TokenType type in types) {
            if (currentType == type) {
                matchedToken = Current();
                Advance();

                return true;
            }
        }

        matchedToken = default;
        return false;
    }

    protected void Consume(TokenType type, string errorMessage) {
        if (!Check(type)) {
            Emit(WarningCode.BadToken, errorMessage);
        }
    }

    /// <returns>The <see cref="TokenType"/> that was found.</returns>
    protected TokenType Consume(TokenType[] types, string errorMessage) {
        foreach (TokenType type in types) {
            if (Check(type)) return type;
        }

        Emit(WarningCode.BadToken, errorMessage);
        return TokenType.Unknown;
    }

    /// <summary>
    /// Emits a warning discovered during parsing, optionally causing a throw.
    /// </summary>
    /// <remarks> This implementation on <see cref="Parser{SourceType}"/> does not make use of <see cref="WarningCode"/> <br/>
    /// since there are some parsers that aren't always in the compilation context, like the ones for DMF and DMM. <br/>
    /// </remarks>
    protected void Warning(string message, Token? token = null) {
        token ??= _currentToken;
        Compiler.ForcedWarning(token.Value.Location, message);
    }

    /// <returns> True if this will raise an error, false if not. You can use this return value to help improve error emission around this (depending on how permissive we're being)</returns>
    protected bool Emit(WarningCode code, Location location, string message) {
        return Compiler.Emit(code, location, message);
    }

    protected bool Emit(WarningCode code, string message) {
        return Emit(code, Current().Location, message);
    }
}
