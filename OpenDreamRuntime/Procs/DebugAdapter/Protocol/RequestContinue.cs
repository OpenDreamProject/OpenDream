using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

[UsedImplicitly]
public sealed class RequestContinue : Request {
    [JsonPropertyName("arguments")] public required RequestContinueArguments Arguments { get; set; }

    [UsedImplicitly]
    public sealed class RequestContinueArguments {
        /**
         * Specifies the active thread. If the debug adapter supports single thread
         * execution (see `supportsSingleThreadExecutionRequests`) and the argument
         * `singleThread` is true, only the thread with this ID is resumed.
         */
        [JsonPropertyName("threadId")] public int ThreadId { get; set; }

        /**
         * If this flag is true, execution is resumed only for the thread with given
         * `threadId`.
         */
        [JsonPropertyName("singleThread")] public bool? SingleThread { get; set; }
    }

    public void Respond(DebugAdapterClient client, bool? allThreadsContinued = null) {
        client.SendMessage(Response.NewSuccess(this, new { allThreadsContinued }));
    }
}
