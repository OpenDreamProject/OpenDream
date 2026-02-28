using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

[UsedImplicitly]
public sealed class RequestHotReloadBytecode : Request {
    [JsonPropertyName("arguments")] public required RequestHotReloadBytecodeArguments Arguments { get; set; }

    [UsedImplicitly]
    public sealed class RequestHotReloadBytecodeArguments {
        [JsonPropertyName("file")] public string? FilePath { get; set; }
    }

    public void Respond(DebugAdapterClient client) {
        client.SendMessage(Response.NewSuccess(this));
    }
}
