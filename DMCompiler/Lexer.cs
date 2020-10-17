using System;
using System.Collections.Generic;

namespace DMCompiler {
    class Lexer {
        public string Source { get; private set; }
        public string[] Lines { get => Source.Split("\n"); }

        protected Queue<Token> _pendingTokenQueue = new Queue<Token>();
        protected int _currentPosition = -1;
        protected int _currentLine = 1;
        protected int _currentColumn = 0;

        public Lexer(string source) {
            Source = source.Replace("\r\n", "\n");

            Advance();
        }

        public Token GetNextToken() {
            if (_pendingTokenQueue.Count > 0)
                return _pendingTokenQueue.Dequeue();

            Token nextToken = ParseNextToken();
            while (nextToken.Type == TokenType.Skip) nextToken = ParseNextToken();

            return nextToken;
        }

        protected virtual Token ParseNextToken() {
            char c = GetCurrent();

            switch (c) {
                case '\n': Advance(); return CreateToken(TokenType.Newline, c);
                case '\0': return CreateToken(TokenType.EndOfFile, c);
                default: return CreateToken(TokenType.Unknown, c);
            }
        }

        protected bool IsAlphabetic(char c) {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }

        protected bool IsNumeric(char c) {
            return (c >= '0' && c <= '9');
        }

        protected bool IsAlphanumeric(char c) {
            return IsAlphabetic(c) || IsNumeric(c);
        }

        protected bool IsAtEndOfFile() {
            return _currentPosition >= Source.Length;
        }

        protected Token CreateToken(TokenType type, string text, object value = null) {
            return new Token(type, text, _currentLine, _currentColumn, value);
        }

        protected Token CreateToken(TokenType type, char text, object value = null) {
            return CreateToken(type, Convert.ToString(text), value);
        }

        protected char GetCurrent() {
            if (IsAtEndOfFile()) return '\0';

            return Source[_currentPosition];
        }

        protected virtual char Advance() {
            if (_currentPosition >= 0) {
                if (GetCurrent() == '\n') {
                    _currentLine++;
                    _currentColumn = 1;
                } else {
                    _currentColumn++;
                }
            }

            _currentPosition++;
            return GetCurrent();
        }
    }
}
