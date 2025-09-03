using System.Text.Json.Serialization;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

public sealed class StoppedEvent : IEvent {
    Event IEvent.ToEvent() => new("stopped", this);

    /**
     * The reason for the event.
     * For backward compatibility this string is shown in the UI if the
     * `description` attribute is missing (but it must not be translated).
     * Values: 'step', 'breakpoint', 'exception', 'pause', 'entry', 'goto',
     * 'function breakpoint', 'data breakpoint', 'instruction breakpoint', etc.
     */
    [JsonPropertyName("reason")] public required string Reason { get; set; }

    public const string ReasonStep = "step";
    public const string ReasonBreakpoint = "breakpoint";
    public const string ReasonException = "exception";
    public const string ReasonPause = "pause";
    public const string ReasonEntry = "entry";
    public const string ReasonGoto = "goto";
    public const string ReasonFunctionBreakpoint = "function breakpoint";
    public const string ReasonDataBreakpoint = "data breakpoint";
    public const string ReasonInstructionBreakpoint = "instruction breakpoint";

    /**
     * The full reason for the event, e.g. 'Paused on exception'. This string is
     * shown in the UI as is and can be translated.
     */
    [JsonPropertyName("description")] public string? Description { get; set; }

    /**
     * The thread which was stopped.
     */
    [JsonPropertyName("threadId")] public int? ThreadId { get; set; }

    /**
     * A value of true hints to the client that this event should not change the
     * focus.
     */
    [JsonPropertyName("preserveFocusHint")] public bool? PreserveFocusHint { get; set; }

    /**
     * Additional information. E.g. if reason is `exception`, text contains the
     * exception name. This string is shown in the UI.
     */
    [JsonPropertyName("text")] public string? Text { get; set; }

    /**
     * If `allThreadsStopped` is true, a debug adapter can announce that all
     * threads have stopped.
     * - The client should use this information to enable that all threads can
     * be expanded to access their stacktraces.
     * - If the attribute is missing or false, only the thread with the given
     * `threadId` can be expanded.
     */
    [JsonPropertyName("allThreadsStopped")] public bool? AllThreadsStopped { get; set; }

    /**
     * Ids of the breakpoints that triggered the event. In most cases there is
     * only a single breakpoint but here are some examples for multiple
     * breakpoints:
     * - Different types of breakpoints map to the same location.
     * - Multiple source breakpoints get collapsed to the same instruction by
     * the compiler/runtime.
     * - Multiple function breakpoints with different function names map to the
     * same location.
     */
    [JsonPropertyName("hitBreakpointIds")] public IEnumerable<int>? HitBreakpointIds { get; set; }
}
