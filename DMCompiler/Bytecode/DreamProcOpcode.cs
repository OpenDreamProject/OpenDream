using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace DMCompiler.Bytecode;

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
    //0x7
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
    [OpcodeMetadata(-1, OpcodeArgType.Label)]
    JumpIfTrue = 0xD,
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
    [OpcodeMetadata(-3)]
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
    //0x30
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
    [OpcodeMetadata(-1)]
    CreateListEnumerator = 0x3A,
    [OpcodeMetadata(0, OpcodeArgType.Reference, OpcodeArgType.Label)]
    Enumerate = 0x3B,
    [OpcodeMetadata]
    DestroyEnumerator = 0x3C,
    [OpcodeMetadata(-3)]
    Browse = 0x3D,
    [OpcodeMetadata(-3)]
    BrowseResource = 0x3E,
    [OpcodeMetadata(-3)]
    OutputControl = 0x3F,
    [OpcodeMetadata(-1)]
    BitShiftRight = 0x40,
    [OpcodeMetadata(-1, OpcodeArgType.FilterId)]
    CreateFilteredListEnumerator = 0x41,
    [OpcodeMetadata(-1)]
    Power = 0x42,
    //0x43,
    //0x44
    [OpcodeMetadata(-3, OpcodeArgType.TypeId)]
    Prompt = 0x45,
    [OpcodeMetadata(-3)]
    Ftp = 0x46,
    [OpcodeMetadata(-1)]
    Initial = 0x47,
    //0x48
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
    [OpcodeMetadata(0, OpcodeArgType.Reference, OpcodeArgType.Label)]
    JumpIfNullDereference = 0x50,
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
    [OpcodeMetadata(-1)]
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
    [OpcodeMetadata(0, OpcodeArgType.Label)]
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
    [OpcodeMetadata(0, OpcodeArgType.Reference, OpcodeArgType.Label)]
    PushRefandJumpIfNotNull = 0x84,
    [OpcodeMetadata(-1, OpcodeArgType.Reference)]
    AssignPop = 0x85,
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
    [OpcodeMetadata(1, OpcodeArgType.Label)]
    JumpIfNotNull = 0x94,
    [OpcodeMetadata(0, OpcodeArgType.TypeId)]
    IsTypeDirect = 0x95,
    [OpcodeMetadata(0, OpcodeArgType.Reference)]
    NullRef = 0x96
}

/// <summary>
/// Handles how we write format data into our strings.
/// </summary>
public static class StringFormatEncoder {
    /// <summary>
    /// This is the upper byte of the 2-byte markers we use for storing formatting data within our UTF16 strings.<br/>
    /// </summary>
    /// <remarks>
    /// It is not const because (<see langword="TODO"/>) eventually it would be desirable to make it something else<br/>
    /// (even though doing so would slightly break parity)<br/>
    /// because 0xFFxx actually maps to meaningful Unicode code points under UTF16<br/>
    /// (DM uses this because it uses UTF8 and 0xFF is just an invalid character in that encoding, no biggie)<br/>
    /// See: "Halfwidth and Fullwidth Forms" on https://en.wikibooks.org/wiki/Unicode/Character_reference/F000-FFFF
    /// </remarks>
    public static UInt16 FormatPrefix = 0xFF00;

    /// <summary>
    /// The lower byte of the aforementioned formatting marker thingies we stuff into our UTF16 strings.<br/>
    /// To avoid clashing with the (ALREADY ASSIGNED!) 0xFFxx code point space, these values should not exceed 0x005e (94)
    /// </summary>
    /// <remarks>
    /// <see langword="DO NOT CAST TO CHAR!"/> This requires FormatPrefix to be added to it in order to be a useful formatting character!!
    /// </remarks>
    public enum FormatSuffix : UInt16 {
        //States that Interpolated values can have (the [] thingies)
        StringifyWithArticle = 0x0,    //[] and we include an appropriate article for the resulting value, if necessary
        StringifyNoArticle = 0x1,      //[] and we never include an article (because it's elsewhere)
        ReferenceOfValue = 0x2,        //\ref[]

        //States that macros can have
        //(these can have any arbitrary value as long as compiler/server/cilent all agree)
        //(Some of these values may not align with what they are internally in BYOND; too bad!!)
        UpperDefiniteArticle,     //The
        LowerDefiniteArticle,     //the
        UpperIndefiniteArticle,   //A, An, Some
        LowerIndefiniteArticle,   //a, an, some
        UpperSubjectPronoun,      //He, She, They, It
        LowerSubjectPronoun,      //he, she, they, it
        UpperPossessiveAdjective, //His, Her, Their, Its
        LowerPossessiveAdjective, //his, her, their, its
        ObjectPronoun,            //him, her, them, it
        ReflexivePronoun,         //himself, herself, themself, it
        UpperPossessivePronoun,   //His, Hers, Theirs, Its
        LowerPossessivePronoun,   //his, hers, theirs, its

        Proper,                   //String represents a proper noun
        Improper,                 //String represents an improper noun

        LowerRoman,               //i, ii, iii, iv, v
        UpperRoman,               //I, II, III, IV, V

        OrdinalIndicator,        //1st, 2nd, 3rd, 4th, ...
        PluralSuffix,            //-s suffix at the end of a plural noun

        Icon,                    //Use an atom's icon

        ColorRed,
        ColorBlue,
        ColorGreen,
        ColorBlack,
        ColorYellow,
        ColorNavy,
        ColorTeal,
        ColorCyan,
        Bold,
        Italic
    }

    /// <summary>The default stringification state of a [] within a DM string.</summary>
    public static FormatSuffix InterpolationDefault => FormatSuffix.StringifyWithArticle;

    /// <returns>The UTF16 character we should be actually storing to articulate this format marker.</returns>
    public static char Encode(FormatSuffix suffix) {
        return (char)(FormatPrefix | ((UInt16)suffix));
    }

    /// <returns>true if the input character was actually a formatting codepoint. false if not.</returns>
    public static bool Decode(char c, [NotNullWhen(true)] out FormatSuffix? suffix) {
        UInt16 bytes = c; // this is an implicit reinterpret_cast, in C++ lingo
        suffix = null;
        if((bytes & FormatPrefix) != FormatPrefix)
            return false;
        suffix = (FormatSuffix)(bytes & 0x00FF); // 0xFFab & 0x00FF == 0x00ab
        return true;
    }

    public static bool Decode(char c) {
        UInt16 bytes = c;
        return (bytes & FormatPrefix) == FormatPrefix; // Could also check that the lower byte is a valid enum but... ehhhhh
    }

    /// <returns>true if argument is a marker for an interpolated value, one of them [] things. false if not.</returns>
    public static bool IsInterpolation(FormatSuffix suffix) {
        //This logic requires that all the interpolated-value enums keep separated from the others.
        //I'd write some type-engine code to catch a discrepancy in that but alas, this language is just not OOPy enough.
        return suffix <= FormatSuffix.ReferenceOfValue;
    }

    /// <returns>A new version of the string, with all formatting characters removed.</returns>
    public static string RemoveFormatting(string input) {
        StringBuilder ret = new StringBuilder(input.Length); // Trying to keep it to one malloc here
        foreach(char c in input) {
            if(!Decode(c))
                ret.Append(c);
        }

        return ret.ToString();
    }

    public static string PrettyPrint(string input) {
        StringBuilder ret = new StringBuilder(input.Length);
        foreach (char c in input) {
            if (Decode(c, out var suffix)) {
                ret.Append($"[{SuffixToString(suffix)}]");
            } else {
                ret.Append(c);
            }
        }

        return ret.ToString();
    }

    public static string SuffixToString(FormatSuffix? suffix) {
        if (suffix == null) return "null";
        return suffix.ToString()!;
    }
}

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

public struct DMReference {
    public static readonly DMReference Src = new() { RefType = Type.Src };
    public static readonly DMReference Self = new() { RefType = Type.Self };
    public static readonly DMReference Usr = new() { RefType = Type.Usr };
    public static readonly DMReference Args = new() { RefType = Type.Args };
    public static readonly DMReference SuperProc = new() { RefType = Type.SuperProc };
    public static readonly DMReference ListIndex = new() { RefType = Type.ListIndex };

    public enum Type : byte {
        Src,
        Self,
        Usr,
        Args,
        SuperProc,
        ListIndex,
        Argument,
        Local,
        Global,
        GlobalProc,
        Field,
        SrcField,
        SrcProc,
    }

    public Type RefType;

    //Argument, Local, Global, GlobalProc
    public int Index;

    //Field, SrcField, Proc, SrcProc
    public string Name;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DMReference CreateArgument(int argId) {
        if (argId > 255) throw new Exception("Argument id is greater than the maximum of 255");

        return new DMReference() { RefType = Type.Argument, Index = (byte)argId };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DMReference CreateLocal(int local) {
        if (local > 255) throw new Exception("Local variable id is greater than the maximum of 255");

        return new DMReference() { RefType = Type.Local, Index = (byte)local };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DMReference CreateGlobal(int global) {
        return new DMReference() { RefType = Type.Global, Index = global };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DMReference CreateField(string fieldName) {
        return new DMReference() { RefType = Type.Field, Name = fieldName };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DMReference CreateSrcField(string fieldName) {
        return new DMReference() { RefType = Type.SrcField, Name = fieldName };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DMReference CreateGlobalProc(int procId) {
        return new DMReference() { RefType = Type.GlobalProc, Index = procId };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DMReference CreateSrcProc(string procName) {
        return new DMReference() { RefType = Type.SrcProc, Name = procName };
    }

    public override string ToString() {
        switch (RefType) {
            case Type.Local:
            case Type.Global:
            case Type.Argument:
            case Type.GlobalProc:
                return $"{RefType} {Index}";

            case Type.SrcField:
            case Type.Field:
            case Type.SrcProc:
                return $"{RefType} \"{Name}\"";

            default: return RefType.ToString();
        }
    }
}

// Dummy class-as-namespace because C# just kinda be like this
public static class OpcodeVerifier {
    /// <summary>
    /// Calculates a hash of all the <c>DreamProcOpcode</c>s for warning on incompatibilities.
    /// </summary>
    /// <returns>A MD5 hash string</returns>
    public static string GetOpcodesHash() {
        Array allOpcodes = Enum.GetValues(typeof(DreamProcOpcode));
        List<byte> opcodesBytes = new List<byte>();

        foreach (var value in allOpcodes) {
            byte[] nameBytes = Encoding.ASCII.GetBytes(value.ToString()!);
            opcodesBytes.AddRange(nameBytes);
            opcodesBytes.Add((byte)value);
        }

        byte[] hashBytes = MD5.HashData(opcodesBytes.ToArray());
        return BitConverter.ToString(hashBytes).Replace("-", "");
    }
}

/// <summary>
/// Custom attribute for declaring <see cref="OpcodeMetadata"/> metadata for individual opcodes
/// </summary>
[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
internal sealed class OpcodeMetadataAttribute : Attribute {
    public OpcodeMetadata Metadata;

    public OpcodeMetadataAttribute(int stackDelta = 0, params OpcodeArgType[] requiredArgs) {
        Metadata = new OpcodeMetadata(stackDelta, false, requiredArgs);
    }

    public OpcodeMetadataAttribute(bool variableArgs, int stackDelta,
        params OpcodeArgType[] requiredArgs) {
        Metadata = new OpcodeMetadata(stackDelta, variableArgs, requiredArgs);
    }
}

public enum OpcodeArgType {
    ArgType,
    StackDelta,
    Resource,
    TypeId,
    ProcId,
    FilterId,
    ListSize,
    Int,
    Label,
    Float,
    String,
    Reference,
    FormatCount,
    PickCount,
    ConcatCount,
}

/// <summary>
/// Miscellaneous metadata associated with individual <see cref="DreamProcOpcode"/> opcodes using the <see cref="OpcodeMetadataAttribute"/> attribute
/// </summary>
public struct OpcodeMetadata {
    public readonly int StackDelta; // Net change in stack size caused by this opcode
    public readonly List<OpcodeArgType> RequiredArgs; // The types of arguments this opcode requires
    public readonly bool VariableArgs; // Whether this opcode takes a variable number of arguments

    public OpcodeMetadata(int stackDelta = 0, bool variableArgs = false, params OpcodeArgType[] requiredArgs) {
        StackDelta = stackDelta;
        RequiredArgs = new List<OpcodeArgType>(requiredArgs);
        VariableArgs = variableArgs;
    }
}

/// <summary>
/// Automatically builds a cache of the <see cref="OpcodeMetadata"/> attribute for each opcode
/// </summary>
public static class OpcodeMetadataCache {
    private static readonly OpcodeMetadata[] MetadataCache = new OpcodeMetadata[256];

    static OpcodeMetadataCache() {
        foreach (DreamProcOpcode opcode in Enum.GetValues(typeof(DreamProcOpcode))) {
            var field = typeof(DreamProcOpcode).GetField(opcode.ToString());
            var attribute = Attribute.GetCustomAttribute(field!, typeof(OpcodeMetadataAttribute));
            var metadataAttribute = (OpcodeMetadataAttribute)attribute;
            MetadataCache[(byte)opcode] = metadataAttribute?.Metadata ?? new OpcodeMetadata();
        }
    }

    public static OpcodeMetadata GetMetadata(DreamProcOpcode opcode) {
        return MetadataCache[(byte)opcode];
    }
}
