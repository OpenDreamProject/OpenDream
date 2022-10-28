using System.Text.Json.Serialization;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

public sealed class OutputEvent : IEvent {
    Event IEvent.ToEvent() => new("output", this);

    [JsonPropertyName("category")] public string Category { get; set; }
    [JsonPropertyName("output")] public string Output { get; set; }

    public OutputEvent(string category, string output) {
        Category = category;
        Output = output;
    }
}
