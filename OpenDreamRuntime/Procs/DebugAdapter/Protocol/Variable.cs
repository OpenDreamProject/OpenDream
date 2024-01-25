using System.Text.Json.Serialization;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

public sealed class Variable {
    /**
     * The variable's name.
     */
    [JsonPropertyName("name")] public required string Name { get; set; }

    /**
     * The variable's value.
     * This can be a multi-line text, e.g. for a function the body of a function.
     * For structured variables (which do not have a simple value), it is
     * recommended to provide a one-line representation of the structured object.
     * This helps to identify the structured object in the collapsed state when
     * its children are not yet visible.
     * An empty string can be used if no value should be shown in the UI.
     */
    [JsonPropertyName("value")] public required string Value { get; set; }

    /**
     * The type of the variable's value. Typically shown in the UI when hovering
     * over the value.
     * This attribute should only be returned by a debug adapter if the
     * corresponding capability `supportsVariableType` is true.
     */
    [JsonPropertyName("type")] public string? Type { get; set; }

    /**
     * Properties of a variable that can be used to determine how to render the
     * variable in the UI.
     */
    [JsonPropertyName("presentationHint")] public VariablePresentationHint? PresentationHint { get; set; }

    /**
     * The evaluatable name of this variable which can be passed to the `evaluate`
     * request to fetch the variable's value.
     */
    [JsonPropertyName("evaluateName")] public string? EvaluateName { get; set; }

    /**
     * If `variablesReference` is > 0, the variable is structured and its children
     * can be retrieved by passing `variablesReference` to the `variables`
     * request.
     */
    [JsonPropertyName("variablesReference")] public int VariablesReference { get; set; }

    /**
     * The number of named child variables.
     * The client can use this information to present the children in a paged UI
     * and fetch them in chunks.
     */
    [JsonPropertyName("namedVariables")] public int? NamedVariables { get; set; }

    /**
     * The number of indexed child variables.
     * The client can use this information to present the children in a paged UI
     * and fetch them in chunks.
     */
    [JsonPropertyName("indexedVariables")] public int? IndexedVariables { get; set; }

    /**
     * The memory reference for the variable if the variable represents executable
     * code, such as a function pointer.
     * This attribute is only required if the corresponding capability
     * `supportsMemoryReferences` is true.
     */
    [JsonPropertyName("memoryReference")] public string? MemoryReference { get; set; }
}
