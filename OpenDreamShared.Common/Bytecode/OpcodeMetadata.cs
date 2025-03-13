namespace OpenDreamShared.Common.Bytecode;

/// <summary>
/// Custom attribute for declaring <see cref="OpcodeMetadata"/> metadata for individual opcodes
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class OpcodeMetadataAttribute : Attribute {
    public OpcodeMetadata Metadata;

    public OpcodeMetadataAttribute(int stackDelta = 0, params OpcodeArgType[] requiredArgs) {
        Metadata = new OpcodeMetadata(stackDelta, false, requiredArgs);
    }

    public OpcodeMetadataAttribute(bool variableArgs, int stackDelta,
        params OpcodeArgType[] requiredArgs) {
        Metadata = new OpcodeMetadata(stackDelta, variableArgs, requiredArgs);
    }
}

/// <summary>
/// Miscellaneous metadata associated with individual <see cref="DreamProcOpcode"/> opcodes using the <see cref="OpcodeMetadataAttribute"/> attribute
/// </summary>
public struct OpcodeMetadata(int stackDelta = 0, bool variableArgs = false, params OpcodeArgType[] requiredArgs) {
    public readonly int StackDelta = stackDelta; // Net change in stack size caused by this opcode
    public readonly List<OpcodeArgType> RequiredArgs = [..requiredArgs]; // The types of arguments this opcode requires
    public readonly bool VariableArgs = variableArgs; // Whether this opcode takes a variable number of arguments
}


public enum OpcodeArgType {
    ArgType,
    StackDelta,
    Resource,
    TypeId,
    ProcId,
    EnumeratorId,
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
