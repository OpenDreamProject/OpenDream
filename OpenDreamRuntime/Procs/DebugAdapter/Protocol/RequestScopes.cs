using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

[UsedImplicitly]
public sealed class RequestScopes : Request {
    [JsonPropertyName("arguments")] public required RequestScopesArguments Arguments { get; set; }

    [UsedImplicitly]
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
