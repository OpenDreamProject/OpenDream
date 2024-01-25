using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

[UsedImplicitly]
public sealed class RequestPause : Request {
    [JsonPropertyName("arguments")] public required RequestPauseArguments Arguments { get; set; }

    [UsedImplicitly]
    public sealed class RequestPauseArguments {
        /**
         * Pause execution for this thread.
         */
        [JsonPropertyName("threadId")] public int ThreadId { get; set; }
    }

    public void Respond(DebugAdapterClient client) {
        client.SendMessage(Response.NewSuccess(this));
    }
}
