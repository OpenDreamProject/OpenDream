using System.Text.Json.Serialization;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

public sealed class RequestInitialize : Request {
    [JsonPropertyName("arguments")] public RequestInitializeArguments Arguments { get; set; }

    public sealed class RequestInitializeArguments {
        [JsonPropertyName("clientID")] public string? ClientId { get; set; }
        [JsonPropertyName("clientName")] public string? ClientName { get; set; }
        [JsonPropertyName("adapterID")] public string AdapterId { get; set; }
        [JsonPropertyName("locale")] public string? Locale { get; set; }
        [JsonPropertyName("linesStartAt1")] public bool LinesStartAt1 { get; set; } = true;
        [JsonPropertyName("columnsStartAt1")] public bool ColumnsStartAt1 { get; set; } = true;
        [JsonPropertyName("pathFormat")] public string PathFormat { get; set; } = "path";
        [JsonPropertyName("supportsVariableType")] public bool SupportsVariableType { get; set; }
        [JsonPropertyName("supportsVariablePaging")] public bool SupportsVariablePaging { get; set; }
        [JsonPropertyName("supportsRunInTerminalRequest")] public bool SupportsRunInTerminalRequest { get; set; }
        [JsonPropertyName("supportsMemoryReferences")] public bool SupportsMemoryReferences { get; set; }
        [JsonPropertyName("supportsProgressReporting")] public bool SupportsProgressReporting { get; set; }
        [JsonPropertyName("supportsInvalidatedEvent")] public bool SupportsInvalidatedEvent { get; set; }
        [JsonPropertyName("supportsMemoryEvent")] public bool SupportsMemoryEvent { get; set; }
        [JsonPropertyName("supportsArgsCanBeInterpretedByShell")] public bool SupportsArgsCanBeInterpretedByShell { get; set; }
    }

    public void Respond(DebugAdapterClient client, Capabilities body) {
        client.SendMessage(Response.NewSuccess(this, body));
    }
}
