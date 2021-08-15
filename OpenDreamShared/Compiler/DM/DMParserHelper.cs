
using System;
using System.Collections.Generic;
namespace OpenDreamShared.Compiler.DM {

    public partial class DMParser : Parser<Token> {

        private bool _saveForToplevel = true;
        private Queue<Token> _topLevelTokens = new();

        protected override Token Advance() {
            Token t = base.Advance();

            if (_saveForToplevel) { _topLevelTokens.Enqueue(t); }

            return t;
        }

        protected bool PeekDelimiter() {
            return Current().Type == TokenType.Newline || Current().Type == TokenType.DM_Semicolon;
        }

        protected void LocateNextToplevel() {
            DMLexer lexer = _lexer as DMLexer;
            while (!PeekDelimiter() || lexer.CurrentIndentation() != 0) {
                if (lexer.AtEndOfSource) {
                    break;
                }
                Advance();
            }
        }
    }
}
