using Robust.Shared.Analyzers;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace OpenDreamShared.Compiler {
    [Virtual]
    public partial class Parser<SourceType> {
        /// <summary> Includes errors and warnings acccumulated by this parser. </summary>
        /// <remarks> These initial capacities are arbitrary. We just assume there's a decent chance you'll get a handful of errors/warnings. </remarks>
        public List<CompilerEmission> Emissions = new(8);

        protected Lexer<SourceType> _lexer;
        private Token _currentToken;
        private readonly Stack<Token> _tokenStack = new(1);
        /// <summary>The maximum number of errors or warnings we'd ever place into <see cref="Emissions"/>.</summary>
        protected const int MAX_EMISSIONS_RECORDED = 50_000_000;

        protected Parser(Lexer<SourceType> lexer) {
            _lexer = lexer;

            Advance();
        }

        /// <summary>
        /// Does not consume; this is simply a friendly getter.
        /// </summary>
        protected Token Current() {
            return _currentToken;
        }

        protected virtual Token Advance() {
            if (_tokenStack.Count > 0) {
                _currentToken = _tokenStack.Pop();
            } else {
                _currentToken = _lexer.GetNextToken();

                if (_currentToken.Type == TokenType.Error) {
                    Error((string)_currentToken.Value!, throwException: false);
                    Advance();
                } else if (_currentToken.Type == TokenType.Warning) {
                    Warning((string)_currentToken.Value!);
                    Advance();
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

        /// <returns>The <see cref="TokenType"/> that was found.</returns>
        protected TokenType Consume(TokenType[] types, string errorMessage) {
            foreach (TokenType type in types) {
                if (Check(type)) return type;
            }

            Error(errorMessage);
            return TokenType.Unknown;
        }

        /// <summary>
        /// Emits an error discovered during parsing, optionally causing a throw.
        /// </summary>
        /// <remarks> This implementation on <see cref="Parser{SourceType}"/> does not make use of <see cref="WarningCode"/> <br/>
        /// since there are some parsers that aren't always in the compilation context, like the ones for DMF and DMM. <br/>
        /// </remarks>
        [AssertionMethod]
        protected void Error(string message, [AssertionCondition(AssertionConditionType.IS_TRUE)] bool throwException = true) {
            CompilerEmission error = new CompilerEmission(ErrorLevel.Error, _currentToken.Location, message);

            if(Emissions.Count < MAX_EMISSIONS_RECORDED)
                Emissions.Add(error);
            if (throwException)
                throw new CompileErrorException(error);
        }

        /// <summary>
        /// Emits a warning discovered during parsing, optionally causing a throw.
        /// </summary>
        /// <remarks> This implementation on <see cref="Parser{SourceType}"/> does not make use of <see cref="WarningCode"/> <br/>
        /// since there are some parsers that aren't always in the compilation context, like the ones for DMF and DMM. <br/>
        /// </remarks>
        protected void Warning(string message, Token? token = null) {
            token ??= _currentToken;
            Emissions.Add(new CompilerEmission(ErrorLevel.Warning, token?.Location, message));
        }
    }
}
