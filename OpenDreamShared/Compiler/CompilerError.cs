using System;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;

namespace OpenDreamShared.Compiler {
    public struct CompilerError {
        public Token Token;
        public Location? Location;
        public string Message;
        public StackTrace StackTrace;

        public CompilerError(Token token, string message) {
            Token = token;
            Location = token?.Location ?? Compiler.Location.Unknown;
            Message = message;
            StackTrace = new StackTrace(true);
        }

        public CompilerError(Location location, string message)
        {
            Token = null;
            Location = location;
            Message = message;
            StackTrace = new StackTrace(true);
            //throw new Exception(message);
        }

        public override string ToString() {
            return $"Error at {Location.ToString()}: {Message}";
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

        public CompileErrorException(CompilerError error) : base(error.Message)
        {
            Error = error;
        }

        public CompileErrorException(Token token, string message) : base(message) {
            Error = new CompilerError(null, message);
        }

        public CompileErrorException(Location location, string message) : base(message) {
            Error = new CompilerError(location, message);
        }
    }
}
