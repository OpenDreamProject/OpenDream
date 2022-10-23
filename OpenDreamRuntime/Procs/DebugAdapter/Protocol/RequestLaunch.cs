using System.Text.Json.Serialization;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

public sealed class RequestLaunch : Request {
    [JsonPropertyName("arguments")] public RequestLaunchArguments Arguments { get; set; }

    public sealed class RequestLaunchArguments {
        [JsonPropertyName("noDebug")] public bool NoDebug { get; set; }
        [JsonPropertyName("__restart")] public object? RestartData { get; set; }

        // OpenDream debugger specific
        [JsonPropertyName("json_path")] public string? JsonPath { get; set; }
    }

    private sealed class LaunchResponse : Response {
        public LaunchResponse(Request respondingTo) : base(respondingTo, true) { }
    }

    public void Respond(DebugAdapterClient client) {
        client.SendMessage(new LaunchResponse(this));
    }
}
