using System.Collections.Generic;

namespace DMCompiler.Json;

public enum JsonVariableType {
    Resource = 0,
    Type = 1,
    Proc = 2,
    List = 3,
    PositiveInfinity = 4,
    NegativeInfinity = 5
}

public sealed class DreamTypeJson {
    public required string Path { get; set; }
    public int? Parent { get; set; }
    public int? InitProc { get; set; }
    public List<List<int>>? Procs { get; set; }
    public List<int>? Verbs { get; set; }
    public Dictionary<string, object>? Variables { get; set; }
    public Dictionary<string, int>? GlobalVariables { get; set; }
    public HashSet<string>? ConstVariables { get; set; }
    public HashSet<string>? TmpVariables { get; set; }
}

public sealed class GlobalListJson {
    public int GlobalCount { get; set; }
    public required List<string> Names { get; set; }
    public required Dictionary<int, object> Globals { get; set; }
}
