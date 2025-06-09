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
    FinalOverride = 407,
    // ReSharper disable once InconsistentNaming
    IAmATeaPot = 418, // TODO: Implement the HTCPC protocol for OD
    HardConstContext = 500,
    WriteToConstant = 501,
    InvalidInclusion = 900,

    // 1000 - 1999 are reserved for preprocessor configuration.
    FileAlreadyIncluded = 1000,
    MissingIncludedFile = 1001,
    InvalidWarningCode = 1002,
    InvalidFileDirDefine = 1003,
    MisplacedDirective = 1100,
    UndefineMissingDirective = 1101,
    DefinedMissingParen = 1150,
    ErrorDirective = 1200,
    WarningDirective = 1201,
    MiscapitalizedDirective = 1300,

    // 2000 - 2999 are reserved for compiler configuration of actual behaviour.
    SoftReservedKeyword = 2000, // For keywords that SHOULD be reserved, but don't have to be. 'null' and 'defined', for instance
    ScopeOperandNamedType = 2001, // Scope operator is used on a var named type or parent_type, maybe unintentionally
    DuplicateVariable = 2100,
    DuplicateProcDefinition = 2101,
    PointlessParentCall = 2205,
    PointlessBuiltinCall = 2206, // For pointless calls to issaved() or initial()
    SuspiciousMatrixCall = 2207, // Calling matrix() with seemingly the wrong arguments
    FallbackBuiltinArgument = 2208, // A builtin (sin(), cos(), etc) with an invalid/fallback argument
    PointlessScopeOperator = 2209,
    PointlessPositionalArgument = 2210,
    ProcArgumentGlobal = 2211, // Prepending "/" on a proc arg (e.g. "/proc/example(/var/foo)" makes the arg a global var. Ref https://www.byond.com/forum/post/2830750
    AmbiguousVarStatic = 2212, // Referencing a static variable when an instance variable with the same name exists
    MalformedRange = 2300,
    InvalidRange = 2301,
    InvalidSetStatement = 2302,
    InvalidOverride = 2303,
    InvalidIndexOperation = 2304,
    DanglingVarType = 2401, // For types inferred by a particular var definition and nowhere else, that ends up not existing (not forced-fatal because BYOND doesn't always error)
    MissingInterpolatedExpression = 2500, // A text macro is missing a required interpolated expression
    AmbiguousResourcePath = 2600,
    UnsupportedTypeCheck = 2700,
    InvalidReturnType = 2701, // Proc static typing
    InvalidVarType = 2702, // Var static typing
    ImplicitNullType = 2703, //  Raised when a null variable isn't explicitly statically typed as nullable
    LostTypeInfo = 2704, // An operation led to lost type information
    UnimplementedAccess = 2800, // When accessing unimplemented procs and vars
    UnsupportedAccess  = 2801, // accessing procs and vars that wont be implemented

    // 3000 - 3999 are reserved for stylistic configuration.
    EmptyBlock = 3100,
    EmptyProc = 3101,
    UnsafeClientAccess = 3200,
    SuspiciousSwitchCase = 3201, // "else if" cases are actually valid DM, they just spontaneously end the switch context and begin an if-else ladder within the else case of the switch
    AssignmentInConditional = 3202,
    PickWeightedSyntax = 3203,
    AmbiguousInOrder = 3204,
    ExtraToken = 3205,
    RuntimeSearchOperator = 3300,

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
    public readonly ErrorLevel Level;
    public readonly WarningCode Code;
    public Location Location;
    public readonly string Message;

    public static readonly Dictionary<WarningCode, ErrorLevel> DefaultErrorConfig = new(Enum.GetValues<WarningCode>().Length) {
        //0-999, must all be error
        {WarningCode.Unknown, ErrorLevel.Error},
        {WarningCode.BadToken, ErrorLevel.Error},
        {WarningCode.BadDirective, ErrorLevel.Error},
        {WarningCode.BadExpression, ErrorLevel.Error},
        {WarningCode.MissingExpression, ErrorLevel.Error},
        {WarningCode.InvalidArgumentCount, ErrorLevel.Error},
        {WarningCode.InvalidVarDefinition, ErrorLevel.Error},
        {WarningCode.MissingBody, ErrorLevel.Error},
        {WarningCode.BadLabel, ErrorLevel.Error},
        {WarningCode.InvalidReference, ErrorLevel.Error},
        {WarningCode.BadArgument, ErrorLevel.Error},
        {WarningCode.InvalidArgumentKey, ErrorLevel.Error},
        {WarningCode.ArglistOnlyArgument, ErrorLevel.Error},
        {WarningCode.HardReservedKeyword, ErrorLevel.Error},
        {WarningCode.ItemDoesntExist, ErrorLevel.Error},
        {WarningCode.DanglingOverride, ErrorLevel.Error},
        {WarningCode.StaticOverride, ErrorLevel.Error},
        {WarningCode.FinalOverride, ErrorLevel.Error},
        {WarningCode.IAmATeaPot, ErrorLevel.Error},
        {WarningCode.HardConstContext, ErrorLevel.Error},
        {WarningCode.WriteToConstant, ErrorLevel.Error},
        {WarningCode.InvalidInclusion, ErrorLevel.Error},

        //1000-1999
        {WarningCode.FileAlreadyIncluded, ErrorLevel.Warning},
        {WarningCode.MissingIncludedFile, ErrorLevel.Error},
        {WarningCode.InvalidWarningCode, ErrorLevel.Warning},
        {WarningCode.InvalidFileDirDefine, ErrorLevel.Warning},
        {WarningCode.MisplacedDirective, ErrorLevel.Error},
        {WarningCode.UndefineMissingDirective, ErrorLevel.Warning},
        {WarningCode.DefinedMissingParen, ErrorLevel.Error},
        {WarningCode.ErrorDirective, ErrorLevel.Error},
        {WarningCode.WarningDirective, ErrorLevel.Warning},
        {WarningCode.MiscapitalizedDirective, ErrorLevel.Warning},

        //2000-2999
        {WarningCode.SoftReservedKeyword, ErrorLevel.Error},
        {WarningCode.ScopeOperandNamedType, ErrorLevel.Warning},
        {WarningCode.DuplicateVariable, ErrorLevel.Warning},
        {WarningCode.DuplicateProcDefinition, ErrorLevel.Error},
        {WarningCode.PointlessParentCall, ErrorLevel.Warning},
        {WarningCode.PointlessBuiltinCall, ErrorLevel.Warning},
        {WarningCode.SuspiciousMatrixCall, ErrorLevel.Warning},
        {WarningCode.FallbackBuiltinArgument, ErrorLevel.Warning},
        {WarningCode.PointlessScopeOperator, ErrorLevel.Warning},
        {WarningCode.MalformedRange, ErrorLevel.Warning},
        {WarningCode.InvalidRange, ErrorLevel.Error},
        {WarningCode.InvalidSetStatement, ErrorLevel.Error},
        {WarningCode.InvalidOverride, ErrorLevel.Warning},
        {WarningCode.InvalidIndexOperation, ErrorLevel.Warning},
        {WarningCode.DanglingVarType, ErrorLevel.Warning},
        {WarningCode.MissingInterpolatedExpression, ErrorLevel.Warning},
        {WarningCode.AmbiguousResourcePath, ErrorLevel.Warning},
        {WarningCode.SuspiciousSwitchCase, ErrorLevel.Warning},
        {WarningCode.PointlessPositionalArgument, ErrorLevel.Warning},
        {WarningCode.ProcArgumentGlobal, ErrorLevel.Warning}, // Ref BYOND issue https://www.byond.com/forum/post/2830750
        {WarningCode.AmbiguousVarStatic, ErrorLevel.Warning}, // https://github.com/OpenDreamProject/OpenDream/issues/997
        // NOTE: The next few pragmas are for OpenDream's experimental type checker
        // This feature is still in development, elevating these pragmas outside of local testing is discouraged
        // An RFC to finalize this feature is coming soon(TM)
        // BEGIN TYPEMAKER
        {WarningCode.UnsupportedTypeCheck, ErrorLevel.Notice},
        {WarningCode.InvalidReturnType, ErrorLevel.Notice},
        {WarningCode.InvalidVarType, ErrorLevel.Notice},
        {WarningCode.ImplicitNullType, ErrorLevel.Notice},
        {WarningCode.LostTypeInfo, ErrorLevel.Notice},
        // END TYPEMAKER
        {WarningCode.UnimplementedAccess, ErrorLevel.Warning},

        //3000-3999
        {WarningCode.EmptyBlock, ErrorLevel.Notice},
        {WarningCode.EmptyProc, ErrorLevel.Disabled}, // NOTE: If you enable this in OD's default pragma config file, it will emit for OD's DMStandard. Put it in your codebase's pragma config file.
        {WarningCode.UnsafeClientAccess, ErrorLevel.Disabled}, // NOTE: Only checks for unsafe accesses like "client.foobar" and doesn't consider if the client was already null-checked earlier in the proc
        {WarningCode.AssignmentInConditional, ErrorLevel.Warning},
        {WarningCode.PickWeightedSyntax, ErrorLevel.Disabled},
        {WarningCode.AmbiguousInOrder, ErrorLevel.Warning},
        {WarningCode.ExtraToken, ErrorLevel.Warning},
        {WarningCode.RuntimeSearchOperator, ErrorLevel.Disabled}
    };

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
        _ => ""
    };
}
