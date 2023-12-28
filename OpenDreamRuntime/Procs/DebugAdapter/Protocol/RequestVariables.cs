using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

[UsedImplicitly]
public sealed class RequestVariables : Request {
    [JsonPropertyName("arguments")] public required RequestVariablesArguments Arguments { get; set; }

    [UsedImplicitly]
    public sealed class RequestVariablesArguments {
        /**
         * The Variable reference.
         */
        [JsonPropertyName("variablesReference")] public int VariablesReference { get; set; }

        /**
         * Filter to limit the child variables to either named or indexed. If omitted,
         * both types are fetched.
         * Values: 'indexed', 'named'
         */
        [JsonPropertyName("filter")] public string? Filter { get; set; }
        public const string FilterIndexed = "indexed";
        public const string FilterNamed = "named";

        /**
         * The index of the first variable to return; if omitted children start at 0.
         */
        [JsonPropertyName("start")] public int? Start { get; set; }

        /**
         * The number of variables to return. If count is missing or 0, all variables
         * are returned.
         */
        [JsonPropertyName("count")] public int? Count { get; set; }

        /**
         * Specifies details on how to format the Variable values.
         * The attribute is only honored by a debug adapter if the corresponding
         * capability `supportsValueFormattingOptions` is true.
         */
        [JsonPropertyName("format")] public ValueFormat? Format { get; set; }
    }

    public void Respond(DebugAdapterClient client, IEnumerable<Variable> variables) {
        client.SendMessage(Response.NewSuccess(this, new { variables }));
    }
}
