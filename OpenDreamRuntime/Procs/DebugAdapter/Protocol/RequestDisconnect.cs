using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

[UsedImplicitly]
public sealed class RequestDisconnect : Request {
    [JsonPropertyName("arguments")] public required RequestDisconnectArguments Arguments { get; set; }

    [UsedImplicitly]
    public sealed class RequestDisconnectArguments {
        [JsonPropertyName("restart")] public bool Restart { get; set; }
        [JsonPropertyName("terminateDebuggee")] public bool TerminateDebuggee { get; set; }
        [JsonPropertyName("suspendDebuggee")] public bool SuspendDebuggee { get; set; }
    }

    public void Respond(DebugAdapterClient client) {
        client.SendMessage(Response.NewSuccess(this));
    }
}
