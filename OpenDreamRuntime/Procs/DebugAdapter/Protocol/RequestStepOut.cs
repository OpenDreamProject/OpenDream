using System.Text.Json.Serialization;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

public sealed class RequestStepOut : Request {
    [JsonPropertyName("arguments")] public RequestStepOutArguments Arguments { get; set; }

    public sealed class RequestStepOutArguments {
        /**
         * Specifies the thread for which to resume execution for one step (of the
         * given granularity).
         */
        [JsonPropertyName("threadId")] public int ThreadId { get; set; }

        /**
         * If this flag is true, all other suspended threads are not resumed.
         */
        [JsonPropertyName("singleThread")] public bool? SingleThread { get; set; }

        /**
         * Stepping granularity. If no granularity is specified, a granularity of
         * `statement` is assumed.
         */
        [JsonPropertyName("granularity")] public string? Granularity { get; set; }
    }

    public void Respond(DebugAdapterClient client) {
        client.SendMessage(Response.NewSuccess(this));
    }
}
