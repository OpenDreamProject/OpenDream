namespace OpenDreamShared.Compiler {
    struct CompilerError {
        public Token Token;
        public string Message;

        public CompilerError(Token token, string message) {
            Token = token;
            Message = message;
        }

        public override string ToString() {
            return "Error at " + Token.SourceFile + ":" + Token.Line + ":" + Token.Column + ": " + Message;
        }
    }

    struct CompilerWarning {
        public Token Token;
        public string Message;

        public CompilerWarning(Token token, string message) {
            Token = token;
            Message = message;
        }

        public override string ToString() {
            return "Warning at " + Token.SourceFile + ":" + Token.Line + ":" + Token.Column + ": " + Message;
        }
    }
}
