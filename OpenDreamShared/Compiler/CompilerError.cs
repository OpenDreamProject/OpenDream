using System;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;

namespace OpenDreamShared.Compiler {
    public struct CompilerError {
        public Token Token;
        public string Message;
        public StackTrace StackTrace;

        public CompilerError(Token token, string message) {
            Token = token;
            Message = message;
            StackTrace = new StackTrace(true);
        }

        public override string ToString() {
            string location;

            if (Token != null) {
                location = Token.Location.ToString();
            } else {
                location = Location.Unknown.ToString();
            }

            return $"Error at {location}: {Message}";
        }
    }

    public struct CompilerWarning {
        public Token Token;
        public string Message;

        public CompilerWarning(Token token, string message) {
            Token = token;
            Message = message;
        }

        public override string ToString() {
            string location;

            if (Token != null) {
                location = Token.Location.ToString();
            } else {
                location = Location.Unknown.ToString();
            }

            return $"Warning at {location}: {Message}";
        }
    }

    public class CompileErrorException : Exception {
        public CompilerError Error;

        public CompileErrorException(CompilerError error) : base(error.Message) {
            Error = error;
        }

        public CompileErrorException(string message) : base(message) {
            Error = new CompilerError(null, message);
        }
    }
}
