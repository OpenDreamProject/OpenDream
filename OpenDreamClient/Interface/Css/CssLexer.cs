namespace OpenDreamClient.Interface.Css;

public sealed class CssLexer(string source) {
    private int _currentIndex;

    public enum TokenType {
        EndOfSource,
        Unknown,
        Whitespace,
        Semicolon,
        Colon,
        Comma,
        Asterisk,
        Period,
        OpeningBrace,
        ClosingBrace,
        Identifier,
        String,
        Hash,
        Number,
        Dimension
    }

    public readonly struct Token(TokenType type, ReadOnlyMemory<char> text) {
        public readonly TokenType Type = type;
        public readonly ReadOnlyMemory<char> Text = text;
    }

    public Token GetNextToken() {
        var tokenStart = _currentIndex;
        var tokenType = AdvanceNextToken();
        var tokenText = source.AsMemory(tokenStart, _currentIndex - tokenStart);

        if (tokenType is TokenType.Identifier) {
            tokenType = tokenText switch {
                _ => TokenType.Identifier
            };
        }

        return new Token(tokenType, tokenText);
    }

    private TokenType AdvanceNextToken() {
        if (_currentIndex >= source.Length)
            return TokenType.EndOfSource;

        var c = source[_currentIndex];
        switch (c) {
            case ';': Advance(); return TokenType.Semicolon;
            case ':': Advance(); return TokenType.Colon;
            case ',': Advance(); return TokenType.Comma;
            case '*': Advance(); return TokenType.Asterisk;
            case '.': Advance(); return TokenType.Period;
            case '{': Advance(); return TokenType.OpeningBrace;
            case '}': Advance(); return TokenType.ClosingBrace;
            case '#':
                do {
                    c = Advance();
                } while (IsIdentifierChar(c));

                return TokenType.Hash;
            case '"':
            case '\'':
                var terminator = c;
                do {
                    c = Advance();
                } while (!IsNewline(c) && c != terminator);

                Advance();
                return TokenType.String;
            default:
                if (char.IsWhiteSpace(c) || IsNewline(c)) {
                    do {
                        c = Advance();
                    } while (char.IsWhiteSpace(c) || IsNewline(c));

                    return TokenType.Whitespace;
                }

                if (AdvanceIdentifier(c)) {
                    return TokenType.Identifier;
                } else if (char.IsAsciiDigit(c) || c is '.') {
                    do {
                        c = Advance();
                    } while (char.IsAsciiDigit(c));

                    if (c is '.') {
                        do {
                            c = Advance();
                        } while (char.IsAsciiDigit(c));
                    }

                    return AdvanceIdentifier(c) ? TokenType.Dimension : TokenType.Number;
                }

                return TokenType.Unknown;
        }
    }

    private char Advance() {
        _currentIndex++;

        return _currentIndex < source.Length ? source[_currentIndex] : '\0';
    }

    private bool AdvanceText(string text) {
        foreach (var c in text) {
            if (Advance() != c)
                return false;
        }

        return true;
    }

    private bool AdvanceIdentifier(char c) {
        if (char.IsAsciiLetter(c) || c is '-' or '_') {
            do {
                c = Advance();
            } while (IsIdentifierChar(c));

            return true;
        }

        return false;
    }

    private static bool IsNewline(char c) => c is '\n' or '\r' or '\f';
    private static bool IsIdentifierChar(char c) => char.IsAsciiLetterOrDigit(c) || c is '-' or '_';
}
