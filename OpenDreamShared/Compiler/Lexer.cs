using Robust.Shared.Analyzers;
using System.Collections.Generic;
using System.IO;

namespace OpenDreamShared.Compiler {
    [Virtual]
    public class Lexer<SourceType> {
        public Location CurrentLocation { get; protected set; }
        public IEnumerable<SourceType> Source { get; protected set; }
        public bool AtEndOfSource { get; protected set; } = false;

        protected Queue<Token> _pendingTokenQueue = new();

        private IEnumerator<SourceType> _sourceEnumerator = null;
        private SourceType _current;

        public Lexer(string sourceName, IEnumerable<SourceType> source) {
            CurrentLocation = new Location(sourceName, 1, 0);
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
            return CreateToken(TokenType.Unknown, GetCurrent().ToString());
        }

        protected Token CreateToken(TokenType type, string text, object value = null) {
            return new Token(type, text, CurrentLocation, value);
        }

        protected Token CreateToken(TokenType type, char text, object value = null) {
            return CreateToken(type, char.ToString(text), value);
        }

        protected virtual SourceType GetCurrent() {
            return _current;
        }

        protected virtual SourceType Advance() {
            if (_sourceEnumerator.MoveNext()) {
                _current = _sourceEnumerator.Current;
            } else {
                AtEndOfSource = true;
            }

            return GetCurrent();
        }
    }

    [Virtual]
    public class TextLexer : Lexer<char> {
        protected string _source;
        protected int _currentPosition = 0;

        public TextLexer(string sourceName, string source) : base(sourceName, source) {
            _source = source;

            Advance();
        }

        protected override Token ParseNextToken() {
            char c = GetCurrent();

            Token token;
            switch (c) {
                case '\n': token = CreateToken(TokenType.Newline, c); Advance(); break;
                case '\0': token = CreateToken(TokenType.EndOfFile, c); Advance(); break;
                default: token = null; break;
            }

            return token;
        }

        protected override char GetCurrent() {
            if (AtEndOfSource) return '\0';
            else return base.GetCurrent();
        }

        protected override char Advance() {
            if (GetCurrent() == '\n') {
                CurrentLocation = new Location(
                    CurrentLocation.SourceFile,
                    CurrentLocation.Line + 1,
                    1
                );
            } else {
                CurrentLocation = new Location(
                    CurrentLocation.SourceFile,
                    CurrentLocation.Line,
                    CurrentLocation.Column + 1
                );
            }

            _currentPosition++;
            return base.Advance();
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

        protected bool IsHex(char c) {
            return IsNumeric(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
        }
    }

    [Virtual]
    public class TokenLexer : Lexer<Token> {
        public TokenLexer(string sourceName, IEnumerable<Token> source) : base(sourceName, source) {
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
}
