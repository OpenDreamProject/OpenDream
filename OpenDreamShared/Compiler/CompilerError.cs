namespace OpenDreamShared.Compiler {
    struct CompilerError {
        public Token Token;
        public string Message;

        public CompilerError(Token token, string message) {
            Token = token;
            Message = message;
        }

        public override string ToString() {
            return Token.SourceFile + ":" + Token.Line + ":" + Token.Column + ": " + Message;
        }
    }
}
