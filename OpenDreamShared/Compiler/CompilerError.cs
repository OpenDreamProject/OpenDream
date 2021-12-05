using System;

namespace OpenDreamShared.Compiler {
    public struct CompilerError {
        public Location? Location;
        public string Message;

        public CompilerError(Token token, string message) {
            Location = token?.Location ?? Compiler.Location.Unknown;
            Message = message;
        }

        public CompilerError(Location location, string message) {
            Location = location;
            Message = message;
        }

        public override string ToString() {
            return $"Error at {Location.ToString()}: {Message}";
        }
    }

    public struct CompilerWarning {
        public Location? Location;
        public string Message;

        public CompilerWarning(Token token, string message) {
            Location = token?.Location ?? Compiler.Location.Unknown;
            Message = message;
        }

        public CompilerWarning(Location location, string message) {
            Location = location;
            Message = message;
        }

        public override string ToString() {
            return $"Warning at {Location.ToString()}: {Message}";
        }
    }

    public class CompileErrorException : Exception {
        public CompilerError Error;

        public CompileErrorException(CompilerError error) : base(error.Message)
        {
            Error = error;
        }

        public CompileErrorException(Token token, string message) : base(message) {
            Error = new CompilerError(token, message);
        }

        public CompileErrorException(Location location, string message) : base(message) {
            Error = new CompilerError(location, message);
        }
    }
}
