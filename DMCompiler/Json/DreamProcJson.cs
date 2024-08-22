using System.Collections.Generic;
using DMCompiler.DM;

namespace DMCompiler.Json;

public sealed class ProcDefinitionJson {
    public required int OwningTypeId { get; set; }
    public required string Name { get; set; }
    public required ProcAttributes Attributes { get; set; }

    public required int MaxStackSize { get; set; }
    public required List<ProcArgumentJson>? Arguments { get; set; }
    public required List<LocalVariableJson>? Locals { get; set; }
    public required List<SourceInfoJson> SourceInfo { get; set; }
    public required byte[]? Bytecode { get; set; }

    public required bool IsVerb { get; set; }
    public VerbSrc? VerbSrc { get; set; }
    public string? VerbName { get; set; }
    public string? VerbCategory { get; set; }
    public string? VerbDesc { get; set; }
    public sbyte Invisibility { get; set; }
}

public sealed class ProcArgumentJson {
    public required string Name { get; set; }
    public required DMValueType Type { get; set; }
}

public sealed class LocalVariableJson {
    public required int Offset { get; set; }
    public int? Remove { get; set; }
    public string? Add { get; set; }
}

public sealed class SourceInfoJson {
    public int Offset { get; set; }
    public int? File { get; set; }
    public int Line { get; set; }
}

public class LineComparer : IEqualityComparer<SourceInfoJson> {
    public bool Equals(SourceInfoJson? x, SourceInfoJson? y) {
        return x?.Line == y?.Line;
    }

    public int GetHashCode(SourceInfoJson obj) {
        return obj.Line.GetHashCode();
    }
}
