using System.Collections.Generic;

namespace DMCompiler.Json;

public sealed class DreamCompiledJson {
    public required DreamCompiledJsonMetadata Metadata { get; set; }
    public required List<string>? Strings { get; set; }
    public required string[]? Resources { get; set; }
    public int[]? GlobalProcs { get; set; }
    public GlobalListJson? Globals { get; set; }
    public ProcDefinitionJson? GlobalInitProc { get; set; }
    public required List<DreamMapJson>? Maps { get; set; }
    public required string? Interface { get; set; }
    public required DreamTypeJson[]? Types { get; set; }
    public required ProcDefinitionJson[]? Procs { get; set; }
}

public sealed class DreamCompiledJsonMetadata {
    /// <summary>
    ///  Hash of all the <c>DreamProcOpcode</c>s
    /// </summary>
    public required string Version { get; set; }
}
