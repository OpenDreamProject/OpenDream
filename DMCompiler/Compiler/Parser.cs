using System;
using System.Collections.Generic;

namespace DMCompiler.Compiler {
    class Parser {
        private Lexer _lexer;
        private Token _currentToken;
        private Queue<Token> _tokenQueue = new Queue<Token>();

        public Parser(Lexer lexer) {
            _lexer = lexer;

            Advance();
        }

        protected Token Current() {
            return _currentToken;
        }

        protected Token Advance() {
            if (_tokenQueue.Count > 0) {
                _currentToken = _tokenQueue.Dequeue();
            } else {
                _currentToken = _lexer.GetNextToken();
            }

            return Current();
        }

        protected void ReuseToken(Token token) {
            _tokenQueue.Enqueue(_currentToken);
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
                throw new Exception(errorMessage);
            }
        }

        protected void Consume(TokenType[] types, string errorMessage) {
            foreach (TokenType type in types) {
                if (Check(type)) return;
            }
            
            throw new Exception(errorMessage);
        }
    }
}
