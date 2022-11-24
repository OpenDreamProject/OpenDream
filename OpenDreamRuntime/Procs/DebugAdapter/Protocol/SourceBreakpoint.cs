using System.Text.Json.Serialization;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

public sealed class SourceBreakpoint {
    [JsonPropertyName("line")] public int Line { get; set; }
    [JsonPropertyName("column")] public int? Column { get; set; }
    [JsonPropertyName("condition")] public string? Condition { get; set; }
    [JsonPropertyName("hitCondition")] public string? HitCondition { get; set; }
    [JsonPropertyName("logMessage")] public string? LogMessage { get; set; }
}
