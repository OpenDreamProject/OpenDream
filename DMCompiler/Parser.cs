using System;

namespace DMCompiler {
    class Parser {
        private Lexer _lexer;
        private Token _currentToken;

        public Parser(Lexer lexer) {
            _lexer = lexer;

            Advance();
        }

        protected Token Current() {
            return _currentToken;
        }

        protected Token Advance() {
            _currentToken = _lexer.GetNextToken();

            return Current();
        }

        protected bool Check(TokenType type) {
            if (Current().Type == type) {
                Advance();

                return true;
            }

            return false;
        }

        protected bool Check(TokenType[] types) {
            TokenType currentType = Current().Type;
            foreach (TokenType type in types) {
                if (currentType == type) {
                    Advance();

                    return true;
                }
            }

            return false;
        }

        protected void Consume(TokenType type, string errorMessage) {
            if (!Check(type)) {
                throw new Exception(errorMessage);
            }
        }
    }
}
