using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

[UsedImplicitly]
public sealed class RequestLaunch : Request {
    [JsonPropertyName("arguments")] public required RequestLaunchArguments Arguments { get; set; }

    [UsedImplicitly]
    public sealed class RequestLaunchArguments {
        [JsonPropertyName("noDebug")] public bool NoDebug { get; set; }
        [JsonPropertyName("__restart")] public object? RestartData { get; set; }

        // VSCode specific
        [JsonPropertyName("stopOnEntry")] public bool? StopOnEntry { get; set; }

        // OpenDream debugger specific
        [JsonPropertyName("json_path")] public string? JsonPath { get; set; }
    }

    public void Respond(DebugAdapterClient client) {
        client.SendMessage(Response.NewSuccess(this));
    }
}
