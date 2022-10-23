using System.Text.Json.Serialization;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

public sealed class RequestDisconnect : Request {
    [JsonPropertyName("arguments")] public RequestDisconnectArguments Arguments { get; set; }

    public sealed class RequestDisconnectArguments {
        [JsonPropertyName("restart")] public bool Restart { get; set; }
        [JsonPropertyName("terminateDebuggee")] public bool TerminateDebuggee { get; set; }
        [JsonPropertyName("suspendDebuggee")] public bool SuspendDebuggee { get; set; }
    }

    private sealed class DisconnectResponse : Response {
        public DisconnectResponse(Request respondingTo) : base(respondingTo, true) { }
    }

    public void Respond(DebugAdapterClient client) {
        client.SendMessage(new DisconnectResponse(this));
    }
}
