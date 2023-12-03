using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

[UsedImplicitly]
public sealed class RequestStepIn : Request {
    [JsonPropertyName("arguments")] public required RequestStepInArguments Arguments { get; set; }

    [UsedImplicitly]
    public sealed class RequestStepInArguments {
        /**
         * Specifies the thread for which to resume execution for one step-into (of
         * the given granularity).
         */
        [JsonPropertyName("threadId")] public int ThreadId { get; set; }

        /**
         * If this flag is true, all other suspended threads are not resumed.
         */
        [JsonPropertyName("singleThread")] public bool? SingleThread { get; set; }

        /**
         * Id of the target to step into.
         */
        [JsonPropertyName("targetId")] public int? TargetId { get; set; }

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
