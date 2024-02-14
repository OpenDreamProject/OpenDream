using System;

namespace DMCompiler.Compiler;

/// <remarks>
/// All values should be unique.
/// </remarks>
public enum WarningCode {
    // 0 - 999 are reserved for giving codes to fatal errors which cannot reasonably be demoted to a warning/notice/disable.
    Unknown = 0,
    BadToken = 1,
    BadDirective = 10,
    BadExpression = 11,
    MissingExpression = 12,
    InvalidArgumentCount = 13,
    InvalidVarDefinition = 14,
    MissingBody = 15,
    BadLabel = 19,
    InvalidReference = 50,
    BadArgument = 100,
    InvalidArgumentKey = 101,
    ArglistOnlyArgument = 102,
    HardReservedKeyword = 200, // For keywords that CANNOT be un-reserved.
    ItemDoesntExist = 404,
    DanglingOverride = 405,
    StaticOverride = 406,
    // ReSharper disable once InconsistentNaming
    IAmATeaPot = 418, // TODO: Implement the HTCPC protocol for OD
    HardConstContext = 500,
    WriteToConstant = 501,
    InvalidInclusion = 900,

    // 1000 - 1999 are reserved for preprocessor configuration.
    FileAlreadyIncluded = 1000,
    MissingIncludedFile = 1001,
    MisplacedDirective = 1100,
    UndefineMissingDirective = 1101,
    DefinedMissingParen = 1150,
    ErrorDirective = 1200,
    WarningDirective = 1201,
    MiscapitalizedDirective = 1300,

    // 2000 - 2999 are reserved for compiler configuration of actual behaviour.
    SoftReservedKeyword = 2000, // For keywords that SHOULD be reserved, but don't have to be. 'null' and 'defined', for instance
    DuplicateVariable = 2100,
    DuplicateProcDefinition = 2101,
    PointlessParentCall = 2205,
    PointlessBuiltinCall = 2206, // For pointless calls to issaved() or initial()
    SuspiciousMatrixCall = 2207, // Calling matrix() with seemingly the wrong arguments
    FallbackBuiltinArgument = 2208, // A builtin (sin(), cos(), etc) with an invalid/fallback argument
    MalformedRange = 2300,
    InvalidRange = 2301,
    InvalidSetStatement = 2302,
    InvalidOverride = 2303,
    DanglingVarType = 2401, // For types inferred by a particular var definition and nowhere else, that ends up not existing (not forced-fatal because BYOND doesn't always error)
    MissingInterpolatedExpression = 2500, // A text macro is missing a required interpolated expression
    AmbiguousResourcePath = 2600,

    // 3000 - 3999 are reserved for stylistic configuration.
    EmptyBlock = 3100,
    EmptyProc = 3101,
    UnsafeClientAccess = 3200,
    SuspiciousSwitchCase = 3201, // "else if" cases are actually valid DM, they just spontaneously end the switch context and begin an if-else ladder within the else case of the switch
    AssignmentInConditional = 3202,

    // 4000 - 4999 are reserved for runtime configuration. (TODO: Runtime doesn't know about configs yet!)
}

public enum ErrorLevel {
    //When this warning is emitted:
    Disabled, // Nothing happens.
    Notice, // Nothing happens unless the user provides a '--wall' argument.
    Warning, // A warning is always emitted.
    Error // An error is always emitted.
}

/// <summary>
/// Stores the location and message of a notice/warning/error.
/// </summary>
public struct CompilerEmission {
    public ErrorLevel Level;
    public WarningCode Code;
    public Location Location;
    public string Message;

    public CompilerEmission(ErrorLevel level, Location? location, string message) {
        Level = level;
        Code = WarningCode.Unknown;
        Location = location ?? Location.Unknown;
        Message = message;
    }

    public CompilerEmission(ErrorLevel level, WarningCode code, Location? location, string message) {
        Level = level;
        Code = code;
        Location = location ?? Location.Unknown;
        Message = message;
    }

    public override string ToString() => Level switch {
        ErrorLevel.Disabled => "",
        ErrorLevel.Notice => $"Notice OD{(int)Code:d4} at {Location.ToString()}: {Message}",
        ErrorLevel.Warning => $"Warning OD{(int)Code:d4} at {Location.ToString()}: {Message}",
        ErrorLevel.Error => $"Error OD{(int)Code:d4} at {Location.ToString()}: {Message}",
        _ => "",
    };
}

[Obsolete("This is not a desirable way for the compiler to emit an error. Use CompileAbortException or ForceError() if it needs to be fatal, or an DMCompiler.Emit() otherwise.")]
public class CompileErrorException : Exception {
    public CompilerEmission Error;

    public CompileErrorException(CompilerEmission error) : base(error.Message) {
        Error = error;
    }

    public CompileErrorException(Location location, string message) : base(message) {
        Error = new CompilerEmission(ErrorLevel.Error, location, message);
    }

    public CompileErrorException(string message) {
        Error = new CompilerEmission(ErrorLevel.Error, Location.Unknown, message);
    }
}


/// <summary>
/// Represents an internal compiler error that should cause parsing for a particular block to cease. <br/>
/// This should be ideally used for exceptions that are the fault of the compiler itself, <br/>
/// like an abnormal state being reached or something.
/// </summary>
public sealed class CompileAbortException : CompileErrorException {
    public CompileAbortException(CompilerEmission error) : base(error) {
    }

    public CompileAbortException(Location location, string message) : base(location, message) {
    }

    public CompileAbortException(string message) : base(message) {
    }
}

public sealed class UnknownIdentifierException(Location location, string identifierName) : CompileErrorException(location, $"Unknown identifier \"{identifierName}\"") {
    public string IdentifierName = identifierName;
}
