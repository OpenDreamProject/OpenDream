using System.Text.Json.Serialization;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

public sealed class Event : ProtocolMessage, IEvent {
    [JsonPropertyName("event")] public string EventName { get; set; }
    [JsonPropertyName("body")] public object? Body { get; set; }

    public Event(string eventName, object? body = null) : base("event") {
        EventName = eventName;
        Body = body;
    }

    Event IEvent.ToEvent() => this;
}

public interface IEvent {
    public Event ToEvent();
}
