using System.Collections.Generic;
using Robust.Shared.Log;

namespace OpenDreamShared.Compiler {
    public partial class Parser<SourceType> {
        private Stack<Stack<Token>> _lookahead = new();

        protected void SavePosition() {
            _lookahead.Push(new Stack<Token>());
            _lookahead.Peek().Push(_currentToken);
        }
        protected void RestorePosition() {
            var stack = _lookahead.Pop();
            while (stack.Count > 1) {
                _tokenStack.Push(stack.Pop());
            }
            _currentToken = stack.Pop();
        }
        protected void AcceptPosition() {
            foreach (var token in _lookahead.Pop()) {
                if (_lookahead.Count > 0) {
                    _lookahead.Peek().Push(token);
                }
            }
        }
        protected void Fatal(string error) {
            foreach (var err in Errors) {
                Logger.Fatal(err.ToString());

            }
            throw new System.Exception(error);
        }

    }
}
