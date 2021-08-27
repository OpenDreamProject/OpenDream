
using System;
using System.Collections.Generic;
namespace OpenDreamShared.Compiler.DM {

    public partial class DMParser : Parser<Token> {

        private bool _saveForToplevel = true;

        // NOTE: reused tokens can find their way in here, filter them out before using
        private Queue<Token> _topLevelTokens = new();

        // NOTE: Modify this function when debugging to single out particular errors when there are too many being printed
        private bool IsDebugSource(string filename) {
            // return filename.Contains("donk");
            return true;
        }

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
            var found_top_level = false;
            while (!found_top_level) {
                while (!PeekDelimiter() || lexer.CurrentIndentation() != 0) {
                    if (lexer.AtEndOfSource) {
                        break;
                    }
                    Advance();
                }
                if (lexer.AtEndOfSource) {
                    break;
                }
                Whitespace();
                if (Current().Type != TokenType.DM_Slash) {
                    found_top_level = true;
                }
                else {
                    Advance();
                }
            }
        }

        protected void HandleBlockInnerErrors(List<CompilerError> errors) {
            List<string> sources = new();
            List<int> lines = new();
            List<Token> errortokens = new();
            foreach (var error in Errors) {
                if (!IsDebugSource(error.Token.SourceFile)) {
                    continue;
                }
                errortokens.Add(error.Token);
            }
            if (errortokens.Count > 0) {
                Console.WriteLine(new String('=', 120));
            }
            foreach (var error in Errors) {
                if (!IsDebugSource(error.Token.SourceFile)) {
                    continue;
                }
                Console.WriteLine(error.ToString());
                Console.WriteLine(error.FilteredStackTrace());
                Console.WriteLine(new String('-', 60));
                sources.Add(error.Token.SourceFile);
                lines.Add(error.Token.Line);
            }
            List<Token> viewedTokens = new();
            List<Token> textTokens = new();
            foreach (var token in _topLevelTokens) {
                if (!IsDebugSource(token.SourceFile)) {
                    continue;
                }
                if (textTokens.Contains(token)) {
                    continue;
                }
                var idx = sources.IndexOf(token.SourceFile);
                if (idx > -1) {
                    if (Math.Abs(lines[idx] - token.Line) < 3) {
                        viewedTokens.Add(token);
                    }
                    if (Math.Abs(lines[idx] - token.Line) < 5) {
                        Console.Write(token.Text);
                        textTokens.Add(token);
                    }
                }
            }
            Console.WriteLine('\n');
            if (errortokens.Count > 0) {
                Console.WriteLine(new String('-', 120));
                Console.WriteLine(viewedTokens.ToLongString(errortokens));
            }
        }
    }
}
