namespace OpenDreamShared.Common.Json;

public sealed class DreamCompiledJson {
    public required DreamCompiledJsonMetadata Metadata { get; set; }
    public List<string>? Strings { get; set; }
    public string[]? Resources { get; set; }
    public int[]? GlobalProcs { get; set; }
    public GlobalListJson? Globals { get; set; }
    public ProcDefinitionJson? GlobalInitProc { get; set; }
    public List<DreamMapJson>? Maps { get; set; }
    public string? Interface { get; set; }
    public DreamTypeJson[]? Types { get; set; }
    public ProcDefinitionJson[]? Procs { get; set; }
}

public sealed class DreamCompiledJsonMetadata {
    /// <summary>
    ///  Hash of all the <c>DreamProcOpcode</c>s
    /// </summary>
    public required string Version { get; set; }
}
