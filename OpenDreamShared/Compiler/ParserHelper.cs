using System.Collections.Generic;
using System.Diagnostics;
using Robust.Shared.Log;

namespace OpenDreamShared.Compiler {
    public partial class Parser<SourceType> {
        private Stack<Stack<Token>> _lookahead = new();

        protected void SavePosition() {
            Token? token = null;
            if (_lookahead.Count != 0) {
                token = _lookahead.Peek().Pop();
            }
            _lookahead.Push(new Stack<Token>());
            if (_lookahead.Count == 1) {
                _lookahead.Peek().Push(_currentToken);
            } else {
                Debug.Assert(token != null);
                _lookahead.Peek().Push(token);
            }
        }
        protected void RestorePosition() {
            var stack = _lookahead.Pop();
            while (stack.Count > 1) {
                _tokenStack.Push(stack.Pop());
            }
            if (stack.Count > 0) {
                _currentToken = stack.Pop();
            }
            if (_lookahead.Count > 0) {
                _lookahead.Peek().Push(_currentToken);
            }
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
