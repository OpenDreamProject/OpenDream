using System.Text.Json.Serialization;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

public sealed class RequestScopes : Request {
    [JsonPropertyName("arguments")] public RequestScopesArguments Arguments { get; set; }

    public sealed class RequestScopesArguments {
        /**
         * Retrieve the scopes for this stackframe.
         */
        [JsonPropertyName("frameId")] public int FrameId { get; set; }
    }

    public void Respond(DebugAdapterClient client, IEnumerable<Scope> scopes) {
        client.SendMessage(Response.NewSuccess(this, new { scopes }));
    }
}
