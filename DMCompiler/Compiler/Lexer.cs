using System.IO;

namespace DMCompiler.Compiler;

internal class Lexer<TSourceType> {
    public Location CurrentLocation { get; protected set; }
    public Location PreviousLocation { get; protected set; }
    public string SourceName { get; protected set; }
    public IEnumerable<TSourceType> Source { get; protected set; }
    public bool AtEndOfSource { get; protected set; } = false;

    protected Queue<Token> _pendingTokenQueue = new();

    private readonly IEnumerator<TSourceType> _sourceEnumerator;
    private TSourceType _current;

    protected Lexer(string sourceName, IEnumerable<TSourceType> source) {
        CurrentLocation = new Location(sourceName, 1, 0);
        PreviousLocation = CurrentLocation;
        SourceName = sourceName;
        Source = source;
        if (source == null)
            throw new FileNotFoundException("Source file could not be read: " + sourceName);
        _sourceEnumerator = Source.GetEnumerator();
    }

    public Token GetNextToken() {
        if (_pendingTokenQueue.Count > 0)
            return _pendingTokenQueue.Dequeue();

        Token nextToken = ParseNextToken();
        while (nextToken.Type == TokenType.Skip) nextToken = ParseNextToken();

        if (_pendingTokenQueue.Count > 0) {
            _pendingTokenQueue.Enqueue(nextToken);
            return _pendingTokenQueue.Dequeue();
        } else {
            return nextToken;
        }
    }

    protected virtual Token ParseNextToken() {
        return CreateToken(TokenType.Unknown, GetCurrent()?.ToString() ?? string.Empty);
    }

    protected Token CreateToken(TokenType type, string text, Location location, object? value = null) {
        var token = new Token(type, text, location, value);
        return token;
    }

    protected Token CreateToken(TokenType type, string text, object? value = null) {
        return CreateToken(type, text, PreviousLocation, value);
    }

    protected Token CreateToken(TokenType type, char text, object? value = null) {
        return CreateToken(type, char.ToString(text), value);
    }

    protected virtual TSourceType GetCurrent() {
        return _current;
    }

    /// <remarks>Call before CreateToken to make sure the location is correct</remarks>
    protected virtual TSourceType Advance() {
        PreviousLocation = CurrentLocation;

        if (_sourceEnumerator.MoveNext()) {
            _current = _sourceEnumerator.Current;
        } else {
            AtEndOfSource = true;
        }

        return GetCurrent();
    }
}

internal class TokenLexer : Lexer<Token> {
    protected TokenLexer(string sourceName, IEnumerable<Token> source) : base(sourceName, source) {
        Advance();
    }

    protected override Token Advance() {
        Token current = base.Advance();

        //Warnings and errors go straight to output, no processing
        while (current.Type is TokenType.Warning or TokenType.Error && !AtEndOfSource) {
            _pendingTokenQueue.Enqueue(current);
            current = base.Advance();
        }

        CurrentLocation = current.Location;
        return current;
    }
}
