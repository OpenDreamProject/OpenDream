using System.Text.Json.Serialization;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

public sealed class Breakpoint {
    [JsonPropertyName("id")] public int? Id { get; set; }
    [JsonPropertyName("verified")] public bool Verified { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
    [JsonPropertyName("source")] public Source? Source { get; set; }
    [JsonPropertyName("line")] public int? Line { get; set; }
    [JsonPropertyName("column")] public int? Column { get; set; }
    [JsonPropertyName("endLine")] public int? EndLine { get; set; }
    [JsonPropertyName("endColumn")] public int? EndColumn { get; set; }
    [JsonPropertyName("instructionReference")] public string? InstructionReference { get; set; }
    [JsonPropertyName("offset")] public int? Offset { get; set; }

    public Breakpoint(string message) {
        Verified = false;
        Message = message;
    }

    public Breakpoint(int id, bool verified) {
        Id = id;
        Verified = true;
    }

    public Breakpoint(int id, Source source, int line) {
        Id = id;
        Verified = true;
        Message = null;
        Source = source;
        Line = line;
        Column = null;
        EndLine = line;
        EndColumn = null;
        InstructionReference = null;
        Offset = null;
    }
}
