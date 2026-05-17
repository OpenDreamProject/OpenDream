namespace DMCompiler.Bytecode;

// ReSharper disable MissingBlankLines
public enum DreamProcOpcode : byte {
    [OpcodeMetadata(-1)]
    BitShiftLeft = 0x1,
    [OpcodeMetadata(1, OpcodeArgType.TypeId)]
    PushType = 0x2,
    [OpcodeMetadata(1, OpcodeArgType.String)]
    PushString = 0x3,
    [OpcodeMetadata(0, OpcodeArgType.String, OpcodeArgType.FormatCount)]
    FormatString = 0x4,
    [OpcodeMetadata(-2, OpcodeArgType.Label)]
    SwitchCaseRange = 0x5, //This could either shrink the stack by 2 or 3. Assume 2.
    [OpcodeMetadata(1, OpcodeArgType.Reference)]
    PushReferenceValue = 0x6, // TODO: Local refs should be pure, and other refs that aren't modified
    [OpcodeMetadata(0, OpcodeArgType.ArgType, OpcodeArgType.StackDelta)]
    Rgb = 0x7,
    [OpcodeMetadata(-1)]
    Add = 0x8,
    [OpcodeMetadata(0, OpcodeArgType.Reference)]
    Assign = 0x9,
    [OpcodeMetadata(0, OpcodeArgType.Reference, OpcodeArgType.ArgType, OpcodeArgType.StackDelta)]
    Call = 0xA,
    [OpcodeMetadata(0, OpcodeArgType.Reference)]
    MultiplyReference = 0xB,
    [OpcodeMetadata(-1, OpcodeArgType.Label)]
    JumpIfFalse = 0xC,
    [OpcodeMetadata(0, OpcodeArgType.ListSize)]
    CreateStrictAssociativeList = 0xD,
    [OpcodeMetadata(0, OpcodeArgType.Label)]
    Jump = 0xE,
    [OpcodeMetadata(-1)]
    CompareEquals = 0xF,
    [OpcodeMetadata(-1)]
    Return = 0x10,
    [OpcodeMetadata(1)]
    PushNull = 0x11,
    [OpcodeMetadata(-1)]
    Subtract = 0x12,
    [OpcodeMetadata(-1)]
    CompareLessThan = 0x13,
    [OpcodeMetadata(-1)]
    CompareGreaterThan = 0x14,
    [OpcodeMetadata(-1, OpcodeArgType.Label)]
    BooleanAnd = 0x15, //Either shrinks the stack 1 or 0. Assume 1.
    [OpcodeMetadata]
    BooleanNot = 0x16,
    [OpcodeMetadata(0, OpcodeArgType.Reference)]
    DivideReference = 0x17,
    [OpcodeMetadata]
    Negate = 0x18,
    [OpcodeMetadata(-1)]
    Modulus = 0x19,
    [OpcodeMetadata(0, OpcodeArgType.Reference)]
    Append = 0x1A,
    [OpcodeMetadata(-3, OpcodeArgType.EnumeratorId)]
    CreateRangeEnumerator = 0x1B,
    [OpcodeMetadata(0, OpcodeArgType.Reference, OpcodeArgType.Reference)]
    Input = 0x1C,
    [OpcodeMetadata(-1)]
    CompareLessThanOrEqual = 0x1D,
    [OpcodeMetadata(0, OpcodeArgType.ListSize)]
    CreateAssociativeList = 0x1E,
    [OpcodeMetadata(0, OpcodeArgType.Reference)]
    Remove = 0x1F,
    [OpcodeMetadata(-1)]
    DeleteObject = 0x20,
    [OpcodeMetadata(1, OpcodeArgType.Resource)]
    PushResource = 0x21,
    [OpcodeMetadata(0, OpcodeArgType.ListSize)]
    CreateList = 0x22,
    [OpcodeMetadata(0, OpcodeArgType.ArgType, OpcodeArgType.StackDelta)]
    CallStatement = 0x23,
    [OpcodeMetadata(-1)]
    BitAnd = 0x24,
    [OpcodeMetadata(-1)]
    CompareNotEquals = 0x25,
    [OpcodeMetadata(1, OpcodeArgType.ProcId)]
    PushProc = 0x26,
    [OpcodeMetadata(-1)]
    Divide = 0x27,
    [OpcodeMetadata(-1)]
    Multiply = 0x28,
    [OpcodeMetadata(0, OpcodeArgType.Reference)]
    BitXorReference = 0x29,
    [OpcodeMetadata(-1)]
    BitXor = 0x2A,
    [OpcodeMetadata(-1)]
    BitOr = 0x2B,
    [OpcodeMetadata]
    BitNot = 0x2C,
    [OpcodeMetadata(0, OpcodeArgType.Reference)]
    Combine = 0x2D,
    [OpcodeMetadata(0, OpcodeArgType.ArgType, OpcodeArgType.StackDelta)]
    CreateObject = 0x2E,
    [OpcodeMetadata(-1, OpcodeArgType.Label)]
    BooleanOr = 0x2F, // Shrinks the stack by 1 or 0. Assume 1.
    [OpcodeMetadata(0, OpcodeArgType.ListSize)]
    CreateMultidimensionalList = 0x30,
    [OpcodeMetadata(-1)]
    CompareGreaterThanOrEqual = 0x31,
    [OpcodeMetadata(-1, OpcodeArgType.Label)]
    SwitchCase = 0x32, //This could either shrink the stack by 1 or 2. Assume 1.
    [OpcodeMetadata(0, OpcodeArgType.Reference)]
    Mask = 0x33,
    //0x34
    [OpcodeMetadata]
    Error = 0x35,
    [OpcodeMetadata(-1)]
    IsInList = 0x36,
    //0x37
    [OpcodeMetadata(1, OpcodeArgType.Float)]
    PushFloat = 0x38,
    [OpcodeMetadata(0, OpcodeArgType.Reference)]
    ModulusReference = 0x39,
    [OpcodeMetadata(-1, OpcodeArgType.EnumeratorId)]
    CreateListEnumerator = 0x3A,
    [OpcodeMetadata(0, OpcodeArgType.EnumeratorId, OpcodeArgType.Reference, OpcodeArgType.Label)]
    Enumerate = 0x3B,
    [OpcodeMetadata(0, OpcodeArgType.EnumeratorId)]
    DestroyEnumerator = 0x3C,
    [OpcodeMetadata(-3)]
    Browse = 0x3D,
    [OpcodeMetadata(-3)]
    BrowseResource = 0x3E,
    [OpcodeMetadata(-3)]
    OutputControl = 0x3F,
    [OpcodeMetadata(-1)]
    BitShiftRight = 0x40,
    [OpcodeMetadata(-1, OpcodeArgType.EnumeratorId, OpcodeArgType.FilterId)]
    CreateFilteredListEnumerator = 0x41,
    [OpcodeMetadata(-1)]
    Power = 0x42,
    [OpcodeMetadata(0, OpcodeArgType.EnumeratorId, OpcodeArgType.Reference, OpcodeArgType.Reference, OpcodeArgType.Label)]
    EnumerateAssoc = 0x43,
    [OpcodeMetadata(-2)]
    Link = 0x44,
    [OpcodeMetadata(-3, OpcodeArgType.TypeId)]
    Prompt = 0x45,
    [OpcodeMetadata(-3)]
    Ftp = 0x46,
    [OpcodeMetadata(-1)]
    Initial = 0x47,
    [OpcodeMetadata(-1)]
    AsType = 0x48,
    [OpcodeMetadata(-1)]
    IsType = 0x49,
    [OpcodeMetadata(-2)]
    LocateCoord = 0x4A,
    [OpcodeMetadata(-1)]
    Locate = 0x4B,
    [OpcodeMetadata]
    IsNull = 0x4C,
    [OpcodeMetadata(-1, OpcodeArgType.Label)]
    Spawn = 0x4D,
    [OpcodeMetadata(-1, OpcodeArgType.Reference)]
    OutputReference = 0x4E,
    [OpcodeMetadata(-2)]
    Output = 0x4F,
    // 0x50
    [OpcodeMetadata(-1)]
    Pop = 0x51,
    [OpcodeMetadata]
    Prob = 0x52,
    [OpcodeMetadata(-1)]
    IsSaved = 0x53,
    [OpcodeMetadata(0, OpcodeArgType.PickCount)]
    PickUnweighted = 0x54,
    [OpcodeMetadata(0, OpcodeArgType.PickCount)]
    PickWeighted = 0x55,
    [OpcodeMetadata(1, OpcodeArgType.Reference)]
    Increment = 0x56,
    [OpcodeMetadata(1, OpcodeArgType.Reference)]
    Decrement = 0x57,
    [OpcodeMetadata(-1)]
    CompareEquivalent = 0x58,
    [OpcodeMetadata(-1)]
    CompareNotEquivalent = 0x59,
    [OpcodeMetadata]
    Throw = 0x5A,
    [OpcodeMetadata(-2)]
    IsInRange = 0x5B,
    [OpcodeMetadata(0, OpcodeArgType.ConcatCount)]
    MassConcatenation = 0x5C,
    [OpcodeMetadata(-1, OpcodeArgType.EnumeratorId)]
    CreateTypeEnumerator = 0x5D,
    //0x5E
    [OpcodeMetadata(1)]
    PushGlobalVars = 0x5F,
    [OpcodeMetadata(-1)]
    ModulusModulus = 0x60,
    [OpcodeMetadata(0, OpcodeArgType.Reference)]
    ModulusModulusReference = 0x61,
    //0x62
    //0x63
    [OpcodeMetadata(0, OpcodeArgType.Label)]
    JumpIfNull = 0x64,
    [OpcodeMetadata(0, OpcodeArgType.Label)]
    JumpIfNullNoPop = 0x65,
    [OpcodeMetadata(0, OpcodeArgType.Reference, OpcodeArgType.Label)]
    JumpIfTrueReference = 0x66,
    [OpcodeMetadata(0, OpcodeArgType.Reference, OpcodeArgType.Label)]
    JumpIfFalseReference = 0x67,
    [OpcodeMetadata(0, OpcodeArgType.String)]
    DereferenceField = 0x68,
    [OpcodeMetadata(-1)]
    DereferenceIndex = 0x69,
    [OpcodeMetadata(0, OpcodeArgType.String, OpcodeArgType.ArgType, OpcodeArgType.StackDelta)]
    DereferenceCall = 0x6A,
    [OpcodeMetadata(0, OpcodeArgType.Reference)]
    PopReference = 0x6B,
    //0x6C
    [OpcodeMetadata(0, OpcodeArgType.Reference)]
    BitShiftLeftReference = 0x6D,
    [OpcodeMetadata(0, OpcodeArgType.Reference)]
    BitShiftRightReference = 0x6E,
    [OpcodeMetadata(0, OpcodeArgType.Label, OpcodeArgType.Reference)]
    Try = 0x6F,
    [OpcodeMetadata(0, OpcodeArgType.Label)]
    TryNoValue = 0x70,
    [OpcodeMetadata]
    EndTry = 0x71,
    [OpcodeMetadata(0, OpcodeArgType.EnumeratorId, OpcodeArgType.Label)]
    EnumerateNoAssign = 0x72,
    [OpcodeMetadata(0, OpcodeArgType.ArgType, OpcodeArgType.StackDelta)]
    Gradient = 0x73,
    [OpcodeMetadata(0, OpcodeArgType.Reference)]
    AssignInto = 0x74,
    [OpcodeMetadata(-1)]
    GetStep = 0x75,
    [OpcodeMetadata]
    Length = 0x76,
    [OpcodeMetadata(-1)]
    GetDir = 0x77,
    [OpcodeMetadata]
    DebuggerBreakpoint = 0x78,
    [OpcodeMetadata]
    Sin = 0x79,
    [OpcodeMetadata]
    Cos = 0x7A,
    [OpcodeMetadata]
    Tan = 0x7B,
    [OpcodeMetadata]
    ArcSin = 0x7C,
    [OpcodeMetadata]
    ArcCos = 0x7D,
    [OpcodeMetadata]
    ArcTan = 0x7E,
    [OpcodeMetadata(-1)]
    ArcTan2 = 0x7F,
    [OpcodeMetadata]
    Sqrt = 0x80,
    [OpcodeMetadata(-1)]
    Log = 0x81,
    [OpcodeMetadata]
    LogE = 0x82,
    [OpcodeMetadata]
    Abs = 0x83,
    // Peephole optimization opcodes
    [OpcodeMetadata(-1, OpcodeArgType.Reference)]
    AppendNoPush = 0x84,
    [OpcodeMetadata(-1, OpcodeArgType.Reference)]
    AssignNoPush = 0x85,
    [OpcodeMetadata(1, OpcodeArgType.Reference, OpcodeArgType.String)]
    PushRefAndDereferenceField = 0x86,
    [OpcodeMetadata(true, 0, OpcodeArgType.Int)]
    PushNRefs = 0x87,
    [OpcodeMetadata(true, 0, OpcodeArgType.Int)]
    PushNFloats = 0x88,
    [OpcodeMetadata(true, 0, OpcodeArgType.Int)]
    PushNResources = 0x89,
    [OpcodeMetadata(2, OpcodeArgType.String, OpcodeArgType.Float)]
    PushStringFloat = 0x8A,
    [OpcodeMetadata(0, OpcodeArgType.Reference, OpcodeArgType.Label)]
    JumpIfReferenceFalse = 0x8B,
    [OpcodeMetadata(true, 0, OpcodeArgType.Int)]
    PushNStrings = 0x8C,
    [OpcodeMetadata(0, OpcodeArgType.Float, OpcodeArgType.Label)]
    SwitchOnFloat = 0x8D,
    [OpcodeMetadata(true, 0, OpcodeArgType.Int)]
    PushNOfStringFloats = 0x8E,
    [OpcodeMetadata(true, 1, OpcodeArgType.Int)]
    CreateListNFloats = 0x8F,
    [OpcodeMetadata(true, 1, OpcodeArgType.Int)]
    CreateListNStrings = 0x90,
    [OpcodeMetadata(true, 1, OpcodeArgType.Int)]
    CreateListNRefs = 0x91,
    [OpcodeMetadata(true, 1, OpcodeArgType.Int)]
    CreateListNResources = 0x92,
    [OpcodeMetadata(0, OpcodeArgType.String, OpcodeArgType.Label)]
    SwitchOnString = 0x93,
    //0x94
    [OpcodeMetadata(0, OpcodeArgType.TypeId)]
    IsTypeDirect = 0x95,
    [OpcodeMetadata(0, OpcodeArgType.Reference)]
    NullRef = 0x96,
    [OpcodeMetadata(0, OpcodeArgType.Reference)]
    ReturnReferenceValue = 0x97,
    [OpcodeMetadata(0, OpcodeArgType.Float)]
    ReturnFloat = 0x98,
    [OpcodeMetadata(1, OpcodeArgType.Reference, OpcodeArgType.String)]
    IndexRefWithString = 0x99,
    [OpcodeMetadata(2, OpcodeArgType.Float, OpcodeArgType.Reference)]
    PushFloatAssign = 0x9A,
    [OpcodeMetadata(true, 0, OpcodeArgType.Int)]
    NPushFloatAssign = 0x9B,
    [OpcodeMetadata(0, OpcodeArgType.ArgType, OpcodeArgType.StackDelta)]
    Animate = 0x9C,
}
// ReSharper restore MissingBlankLines

/// <summary>
/// An operand given to opcodes that call a proc with arguments.
/// Determines where the arguments come from.
/// </summary>
public enum DMCallArgumentsType {
    // There are no arguments
    None,

    // The arguments are stored on the stack
    FromStack,

    // Also stored on the stack, but every arg has a key associated with it (named arguments)
    FromStackKeyed,

    // Arguments are provided from a list on the top of the stack ( arglist() )
    FromArgumentList,

    // Same arguments as the ones given to the proc doing the calling (implicit ..() arguments)
    FromProcArguments
}
