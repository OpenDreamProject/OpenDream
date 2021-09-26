using OpenDreamShared.Compiler;

namespace DMCompiler.Compiler.DM {
    public partial class DMParser : Parser<Token> {
        protected bool PeekDelimiter() {
            return Current().Type == TokenType.Newline || Current().Type == TokenType.DM_Semicolon;
        }

        protected void LocateNextStatement() {
            while (!PeekDelimiter() && Current().Type != TokenType.DM_Dedent) {
                Advance();

                if (Current().Type == TokenType.EndOfFile) {
                    break;
                }
            }
        }

        protected void LocateNextTopLevel() {
            do {
                LocateNextStatement();

                Delimiter();
                while (Current().Type == TokenType.DM_Dedent) {
                    Advance();
                }

                if (Current().Type == TokenType.EndOfFile) break;
            } while (((DMLexer)_lexer).CurrentIndentation() != 0);

            Delimiter();
        }

        private void ConsumeRightParenthesis() {
            //A missing right parenthesis has to subtract 1 from the lexer's bracket nesting counter
            //To keep indentation working correctly
            if (!Check(TokenType.DM_RightParenthesis)) {
                ((DMLexer)_lexer).BracketNesting--;
                Error("Expected ')'");
            }
        }

        private void ConsumeRightBracket() {
            //Similar to ConsumeRightParenthesis()
            if (!Check(TokenType.DM_RightBracket)) {
                ((DMLexer)_lexer).BracketNesting--;
                Error("Expected ']'");
            }
        }
    }
}
