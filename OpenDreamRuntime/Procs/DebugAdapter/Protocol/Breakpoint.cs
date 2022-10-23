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

    public Breakpoint(int id, Source source, int line, int column) {
        Id = id;
        Verified = true;
        Message = null;
        Source = source;
        Line = line;
        Column = column;
        EndLine = line;
        EndColumn = column;
        InstructionReference = null;
        Offset = null;
    }
}
