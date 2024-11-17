using System.IO;

namespace DMCompiler.Compiler;

internal class Lexer<TSourceType> {
    /// <summary>
    /// Location of token that'll be output by <see cref="GetCurrent"/>. If you skip through more
    /// </summary>
    public Location CurrentLocation { get; protected set; }
    /// <summary>
    /// Location of a previous token.
    /// </summary>
    public Location PreviousLocation { get; private set; }
    public IEnumerable<TSourceType> Source { get; private set; }
    public bool AtEndOfSource { get; private set; }

    protected Queue<Token> _pendingTokenQueue = new();

    private readonly IEnumerator<TSourceType> _sourceEnumerator;
    private TSourceType _current;

    /// <summary>
    /// Given a stream of some type, allows to advance through it and create <see cref="Token"/> tokens
    /// </summary>
    /// <param name="sourceName">Used to build the initial Location, access through <see cref="CurrentLocation"/></param>
    /// <param name="source">Source of <see cref="TSourceType"/> input</param>
    /// <exception cref="FileNotFoundException">Thrown if <paramref name="source"/> is null</exception>
    protected Lexer(string sourceName, IEnumerable<TSourceType> source) {
        CurrentLocation = new Location(sourceName, 1, 0);
        PreviousLocation = CurrentLocation;
        Source = source;
        if (source == null)
            throw new FileNotFoundException("Source file could not be read: " + sourceName);
        _sourceEnumerator = Source.GetEnumerator();
    }

    public Token GetNextToken() {
        if (_pendingTokenQueue.Count > 0)
            return _pendingTokenQueue.Dequeue();

        var nextToken = ParseNextToken();
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

    /// <summary>
    /// Creates a new <see cref="Token"/> located at <see cref="PreviousLocation"/>
    /// </summary>
    /// <remarks>
    /// If you have used <see cref="Advance"/> more than once, the <see cref="Location"/> will be incorrect,
    /// and you'll need to use <see cref="CreateToken(TokenType, string, Location, object?)"/>
    /// with a previously recorded <see cref="CurrentLocation"/>
    /// </remarks>
    protected Token CreateToken(TokenType type, string text, object? value = null) {
        return CreateToken(type, text, PreviousLocation, value);
    }

    /// <inheritdoc cref="CreateToken(TokenType, string, object?)"/>
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
    /// <inheritdoc/>
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
