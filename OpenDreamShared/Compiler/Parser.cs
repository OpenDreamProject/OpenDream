using Robust.Shared.Analyzers;
using System.Collections.Generic;

namespace OpenDreamShared.Compiler {
    [Virtual]
    public partial class Parser<SourceType> {
        public List<CompilerError> Errors = new();
        public List<CompilerWarning> Warnings = new();

        protected Lexer<SourceType> _lexer;
        private Token _currentToken;
        private Stack<Token> _tokenStack = new();

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
                    Error((string)_currentToken.Value, throwException: false);
                    Advance();
                } else if (_currentToken.Type == TokenType.Warning) {
                    Warning((string)_currentToken.Value);
                    Advance();
                }
            }

            if (_lookahead.Count > 0) {
                _lookahead.Peek().Push(_currentToken);
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

        protected void Error(string message, bool throwException = true) {
            CompilerError error = new CompilerError(_currentToken, message);

            Errors.Add(error);
            if (throwException) throw new CompileErrorException(error);
        }

        protected void Warning(string message, Token token = null) {
            token ??= _currentToken;
            Warnings.Add(new CompilerWarning(token, message));
        }
    }
}
