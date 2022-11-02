using System.Text.Json.Serialization;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

public sealed class RequestPause : Request {
    [JsonPropertyName("arguments")] public RequestPauseArguments Arguments { get; set; }

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
