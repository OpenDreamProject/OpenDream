namespace DMCompiler.Compiler.DMPreprocessor;

internal sealed class NtslPreprocessorLexer : DMPreprocessorLexer {
    private bool _onFirstToken = true;

    public NtslPreprocessorLexer(string includeDirectory, string file, string source) : base(includeDirectory, file, source) { }

    public NtslPreprocessorLexer(string includeDirectory, string file) : base(includeDirectory, file) { }

    public override Token NextToken(bool ignoreWhitespace = false) {
        if (_onFirstToken) {
            _onFirstToken = false;

            _pendingTokenQueue.Enqueue(CreateToken(TokenType.DM_Preproc_Whitespace, ' '));
            return CreateToken(TokenType.NTSL_StartFile, '\0');
        }

        var token = base.NextToken(ignoreWhitespace);

        return token.Type == TokenType.EndOfFile ?
            CreateToken(TokenType.NTSL_EndFile, '\0') :
            token;
    }
}
