using System;
using System.Collections.Generic;

namespace OpenDreamShared.Compiler {
    class Parser<SourceType> {
        public List<CompilerError> Errors = new();

        private Lexer<SourceType> _lexer;
        private Token _currentToken;
        private Stack<Token> _tokenStack = new Stack<Token>();

        public Parser(Lexer<SourceType> lexer) {
            _lexer = lexer;

            Advance();
        }

        protected Token Current() {
            return _currentToken;
        }

        protected virtual Token Advance() {
            if (_tokenStack.Count > 0) {
                _currentToken = _tokenStack.Pop();
            } else {
                _currentToken = _lexer.GetNextToken();

                if (_currentToken.Type == TokenType.Error) {
                    Error((string)_currentToken.Value);
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
                Error(errorMessage);
            }
        }

        protected void Consume(TokenType[] types, string errorMessage) {
            foreach (TokenType type in types) {
                if (Check(type)) return;
            }

            Error(errorMessage);
        }

        protected void Error(string message) {
            Errors.Add(new CompilerError(_currentToken, message));
        }
    }
}
