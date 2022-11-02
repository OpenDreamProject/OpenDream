using System.Text.Json.Serialization;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

public sealed class ODReadyEvent : IEvent {
    Event IEvent.ToEvent() => new("$opendream/ready", this);

    [JsonPropertyName("gamePort")] public int Port { get; set; }

    public ODReadyEvent(int port) {
        Port = port;
    }
}
