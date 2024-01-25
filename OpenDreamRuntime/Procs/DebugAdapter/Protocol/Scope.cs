using System.Text.Json.Serialization;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

public sealed class Scope {
    /**
     * Name of the scope such as 'Arguments', 'Locals', or 'Registers'. This
     * string is shown in the UI as is and can be translated.
     */
    [JsonPropertyName("name")] public required string Name { get; set; }

    /**
     * A hint for how to present this scope in the UI. If this attribute is
     * missing, the scope is shown with a generic UI.
     * Values:
     * 'arguments': Scope contains method arguments.
     * 'locals': Scope contains local variables.
     * 'registers': Scope contains registers. Only a single `registers` scope
     * should be returned from a `scopes` request.
     * etc.
     */
    [JsonPropertyName("presentationHint")] public string? PresentationHint { get; set; }
    public const string PresentationHintArguments = "arguments";
    public const string PresentationHintLocals = "locals";
    public const string PresentationHintRegisters = "registers";

    /**
     * The variables of this scope can be retrieved by passing the value of
     * `variablesReference` to the `variables` request.
     */
    [JsonPropertyName("variablesReference")] public int VariablesReference { get; set; }

    /**
     * The number of named variables in this scope.
     * The client can use this information to present the variables in a paged UI
     * and fetch them in chunks.
     */
    [JsonPropertyName("namedVariables")] public int? NamedVariables { get; set; }

    /**
     * The number of indexed variables in this scope.
     * The client can use this information to present the variables in a paged UI
     * and fetch them in chunks.
     */
    [JsonPropertyName("indexedVariables")] public int? IndexedVariables { get; set; }

    /**
     * If true, the number of variables in this scope is large or expensive to
     * retrieve.
     */
    [JsonPropertyName("expensive")] public bool? Expensive { get; set; }

    /**
     * The source for this scope.
     */
    [JsonPropertyName("source")] public Source? Source { get; set; }

    /**
     * The start line of the range covered by this scope.
     */
    [JsonPropertyName("line")] public int? Line { get; set; }

    /**
     * Start position of the range covered by the scope. It is measured in UTF-16
     * code units and the client capability `columnsStartAt1` determines whether
     * it is 0- or 1-based.
     */
    [JsonPropertyName("column")] public int? Column { get; set; }

    /**
     * The end line of the range covered by this scope.
     */
    [JsonPropertyName("endLine")] public int? EndLine { get; set; }

    /**
     * End position of the range covered by the scope. It is measured in UTF-16
     * code units and the client capability `columnsStartAt1` determines whether
     * it is 0- or 1-based.
     */
    [JsonPropertyName("endColumn")] public int? EndColumn { get; set; }
}
