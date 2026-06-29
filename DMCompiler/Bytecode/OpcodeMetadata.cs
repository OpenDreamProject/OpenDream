using System.Runtime.InteropServices;

namespace DMCompiler.Bytecode;

/// <summary>
/// Custom attribute for declaring <see cref="OpcodeMetadata"/> metadata for individual opcodes
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class OpcodeMetadataAttribute(int stackDelta = 0, params OpcodeArgType[] requiredArgs) : Attribute {
    private int StackDelta { get; } = stackDelta;
    private OpcodeArgType[] RequiredArgs { get; } = requiredArgs;
    public OpcodeArgType[] RepeatedArgs { get; set; } = [];

    public OpcodeMetadata Metadata => new(StackDelta, RequiredArgs, RepeatedArgs);
}

/// <summary>
/// Miscellaneous metadata associated with individual <see cref="DreamProcOpcode"/> opcodes using the <see cref="OpcodeMetadataAttribute"/> attribute
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct OpcodeMetadata(
    int stackDelta = 0,
    OpcodeArgType[]? requiredArgs = null,
    OpcodeArgType[]? repeatedArgs = null) {
    public readonly OpcodeArgType[] RequiredArgs = requiredArgs ?? []; // The types of arguments this opcode requires
    public readonly OpcodeArgType[] RepeatedArgs = repeatedArgs ?? []; // For variable-arg opcodes, the repeated argument pattern after the count operand
    public readonly int StackDelta = stackDelta; // Net change in stack size caused by this opcode
    public readonly ProcOperandShape OperandShape = GetOperandShape(repeatedArgs);
    public readonly int JumpDestinationOperandIndex = Array.IndexOf(requiredArgs ?? [], OpcodeArgType.Label); // Cache the index of the jump label arg
    public readonly bool VariableArgs = repeatedArgs != null; // Whether this opcode takes a variable number of arguments

    /// <summary>
    /// All opcodes with a variable argument length have a pattern to their argument types, which this method deduces and stores in metadata
    /// </summary>
    /// <remarks>The purpose of this is to optimize formatting and decoding by avoiding a bunch of argument typechecking during enumeration</remarks>
    private static ProcOperandShape GetOperandShape(OpcodeArgType[]? repeatedArgs) {
        if (repeatedArgs is null)
            return ProcOperandShape.Fixed;

        return repeatedArgs switch {
            [OpcodeArgType.Float] => ProcOperandShape.RepeatedFloat,
            [OpcodeArgType.String] => ProcOperandShape.RepeatedString,
            [OpcodeArgType.Resource] => ProcOperandShape.RepeatedResource,
            [OpcodeArgType.Reference] => ProcOperandShape.RepeatedReference,
            [OpcodeArgType.String, OpcodeArgType.Float] => ProcOperandShape.RepeatedStringFloat,
            [OpcodeArgType.Float, OpcodeArgType.Reference] => ProcOperandShape.RepeatedFloatReference,
            _ => ProcOperandShape.Unsupported
        };
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
            var metadataAttribute = (OpcodeMetadataAttribute?)attribute;
            OpcodeMetadata metadata = metadataAttribute?.Metadata ?? new OpcodeMetadata();
            ValidateMetadata(opcode, metadata);
            MetadataCache[(byte)opcode] = metadata;
        }
    }

    public static OpcodeMetadata GetMetadata(DreamProcOpcode opcode) {
        return MetadataCache[(byte)opcode];
    }

    private static void ValidateMetadata(DreamProcOpcode opcode, OpcodeMetadata metadata) {
        if (!metadata.VariableArgs) {
            if (metadata.RepeatedArgs.Length != 0)
                throw new InvalidOperationException($"{opcode} has repeated args but is not variable-arg");

            return;
        }

        if (metadata.RequiredArgs is not [OpcodeArgType.Int])
            throw new InvalidOperationException($"{opcode} variable-arg metadata must use an int count operand");

        if (metadata.OperandShape == ProcOperandShape.Unsupported)
            throw new InvalidOperationException($"{opcode} has unsupported repeated args");
    }
}

///<summary>
/// Pattern of arguments for variable-arg <see cref="DreamProcOpcode"/> opcodes
/// </summary>
/// <remarks>Fixed indicates a fixed non-variable arg length</remarks>
public enum ProcOperandShape {
    Fixed,
    RepeatedFloat,
    RepeatedString,
    RepeatedResource,
    RepeatedReference,
    RepeatedStringFloat,
    RepeatedFloatReference,
    Unsupported,
}

/// <summary>
/// Every type of argument a <see cref="DreamProcOpcode"/> can have
/// </summary>
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
    ValueType,
}
