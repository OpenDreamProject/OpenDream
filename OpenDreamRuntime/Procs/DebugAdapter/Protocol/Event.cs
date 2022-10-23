using System.Text.Json.Serialization;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

[Virtual]
public class Event : ProtocolMessage {
    [JsonPropertyName("event")] public string EventName { get; set; }
    [JsonPropertyName("body")] public object? Body { get; set; }

    protected Event(string eventName) : base("event") {
        EventName = eventName;
    }
}
