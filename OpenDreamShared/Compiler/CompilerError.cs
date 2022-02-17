using System;
using Robust.Shared.Analyzers;

namespace OpenDreamShared.Compiler {
    public struct CompilerError {
        public Location Location;
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
        public Location Location;
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

    [Virtual]
    public class CompileErrorException : Exception {
        public CompilerError Error;
        public CompileErrorException(CompilerError error) : base(error.Message)
        {
            Error = error;
        }
        public CompileErrorException(Location location, string message) : base(message) {
            Error = new CompilerError(location, message);
        }
    }


    /// <summary>
    /// Represents an internal compiler error that should cause parsing for a particular block to cease.
    /// </summary>
    public sealed class CompileAbortException : CompileErrorException
    {
        public CompileAbortException(CompilerError error) : base(error) {}
        public CompileAbortException(Location location, string message) : base(location, message) {}
    }
}
