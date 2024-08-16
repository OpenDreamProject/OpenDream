using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

[UsedImplicitly]
public sealed class RequestHotReloadResource : Request {
    [JsonPropertyName("arguments")] public required RequestHotReloadResourceArguments Arguments { get; set; }

    [UsedImplicitly]
    public sealed class RequestHotReloadResourceArguments {
        [JsonPropertyName("file")] public string? filePath { get; set; }
    }

    public void Respond(DebugAdapterClient client) {
        client.SendMessage(Response.NewSuccess(this));
    }
}
