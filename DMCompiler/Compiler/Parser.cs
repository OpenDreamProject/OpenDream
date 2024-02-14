using System;
using System.Collections.Generic;

namespace DMCompiler.Compiler;

public class Parser<SourceType> {
    protected Lexer<SourceType> _lexer;
    private Token _currentToken;
    private readonly Stack<Token> _tokenStack = new(1);

    protected Parser(Lexer<SourceType> lexer) {
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
                Error((string)_currentToken.Value!, throwException: false);
                Advance();
            } else if (_currentToken.Type == TokenType.Warning) {
                Warning((string)_currentToken.Value!);
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

    [Obsolete("This throws, which is not a desirable way for the compiler to emit an error.")]
    protected void Consume(TokenType type, string errorMessage) {
        if (!Check(type)) {
            Error(errorMessage);
        }
    }

    /// <returns>The <see cref="TokenType"/> that was found.</returns>
    [Obsolete("This throws, which is not a desirable way for the compiler to emit an error.")]
    protected TokenType Consume(TokenType[] types, string errorMessage) {
        foreach (TokenType type in types) {
            if (Check(type)) return type;
        }

        Error(errorMessage);
        return TokenType.Unknown;
    }

    /// <summary>
    /// Emits an error discovered during parsing, optionally causing a throw.
    /// </summary>
    /// <remarks> This implementation on <see cref="Parser{SourceType}"/> does not make use of <see cref="WarningCode"/> <br/>
    /// since there are some parsers that aren't always in the compilation context, like the ones for DMF and DMM. <br/>
    /// </remarks>
    protected void Error(string message, bool throwException = true) {
        DMCompiler.ForcedError(_currentToken.Location, message);

        if (throwException)
            throw new CompileErrorException(message);
    }

    /// <summary>
    /// Emits a warning discovered during parsing, optionally causing a throw.
    /// </summary>
    /// <remarks> This implementation on <see cref="Parser{SourceType}"/> does not make use of <see cref="WarningCode"/> <br/>
    /// since there are some parsers that aren't always in the compilation context, like the ones for DMF and DMM. <br/>
    /// </remarks>
    protected void Warning(string message, Token? token = null) {
        token ??= _currentToken;
        DMCompiler.ForcedWarning(token.Value.Location, message);
    }
}
