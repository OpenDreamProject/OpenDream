using System.Text.Json.Serialization;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

public sealed class StackFrame {
    /**
     * An identifier for the stack frame. It must be unique across all threads.
     * This id can be used to retrieve the scopes of the frame with the `scopes`
     * request or to restart the execution of a stackframe.
     */
    [JsonPropertyName("id")] public int Id { get; set; }

    /**
     * The name of the stack frame, typically a method name.
     */
    [JsonPropertyName("name")] public required string Name { get; set; }

    /**
     * The source of the frame.
     */
    [JsonPropertyName("source")] public Source? Source { get; set; }

    /**
     * The line within the source of the frame. If the source attribute is missing
     * or doesn't exist, `line` is 0 and should be ignored by the client.
     */
    [JsonPropertyName("line")] public int Line { get; set; }

    /**
     * Start position of the range covered by the stack frame. It is measured in
     * UTF-16 code units and the client capability `columnsStartAt1` determines
     * whether it is 0- or 1-based. If attribute `source` is missing or doesn't
     * exist, `column` is 0 and should be ignored by the client.
     */
    [JsonPropertyName("column")] public int Column { get; set; }

    /**
     * The end line of the range covered by the stack frame.
     */
    [JsonPropertyName("endLine")] public int? EndLine { get; set; }

    /**
     * End position of the range covered by the stack frame. It is measured in
     * UTF-16 code units and the client capability `columnsStartAt1` determines
     * whether it is 0- or 1-based.
     */
    [JsonPropertyName("endColumn")] public int? EndColumn { get; set; }

    /**
     * Indicates whether this frame can be restarted with the `restart` request.
     * Clients should only use this if the debug adapter supports the `restart`
     * request and the corresponding capability `supportsRestartRequest` is true.
     */
    [JsonPropertyName("canRestart")] public bool? CanRestart { get; set; }

    /**
     * A memory reference for the current instruction pointer in this frame.
     */
    [JsonPropertyName("instructionPointerReference")] public string? InstructionPointerReference { get; set; }

    /**
     * The module associated with this frame, if any.
     */
    [JsonPropertyName("moduleId")] public object? ModuleId { get; set; }
    // number | string

    /**
     * A hint for how to present this frame in the UI.
     * A value of `label` can be used to indicate that the frame is an artificial
     * frame that is used as a visual label or separator. A value of `subtle` can
     * be used to change the appearance of a frame in a 'subtle' way.
     * Values: 'normal', 'label', 'subtle'
     */
    [JsonPropertyName("presentationHint")] public string? PresentationHint { get; set; }

    public const string PresentationHintNormal = "normal";
    public const string PresentationHintLabel = "label";
    public const string PresentationHintSubtle = "subtle";
}
