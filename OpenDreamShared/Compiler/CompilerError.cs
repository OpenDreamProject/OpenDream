using System;
using Robust.Shared.Analyzers;

namespace OpenDreamShared.Compiler {

    /// <remarks>
    /// All values should be unique.
    /// </remarks>
    public enum WarningCode
    {
        // 0 - 999 are reserved for giving codes to fatal errors which cannot reasonably be demoted to a warning/notice/disable.
        Unknown = 0,
        // 1000 - 1999 are reserved for compiler configuration of actual behaviour.

        // 2000 - 2999 are reserved for stylistic configuration.

        // 3000 - 3999 are reserved for runtime configuration. (TODO: Runtime doesn't know about configs yet!)
    }

    public enum ErrorLevel
    {
        //When this warning is emitted:
        Disabled, // Nothing happens.
        Notice, // Nothing happens unless the user provides a '--wall' argument. (TODO)
        Warning, // A warning is always emitted.
        Error // An error is always emitted.
    }

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
    [Obsolete("This is not a desirable way for the compiler to emit an error. Use CompileAbortException or ForceError() if it needs to be fatal, or an EmitWarning() otherwise.")]
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
