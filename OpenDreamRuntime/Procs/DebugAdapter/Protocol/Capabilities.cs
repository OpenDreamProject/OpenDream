using System.Text.Json.Serialization;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

public sealed class Capabilities {
    /**
     * The debug adapter supports the `configurationDone` request.
     */
    [JsonPropertyName("supportsConfigurationDoneRequest")] public bool? SupportsConfigurationDoneRequest { get; set; }

    /**
     * The debug adapter supports function breakpoints.
     */
    [JsonPropertyName("supportsFunctionBreakpoints")] public bool? SupportsFunctionBreakpoints { get; set; }

    /**
     * The debug adapter supports conditional breakpoints.
     */
    [JsonPropertyName("supportsConditionalBreakpoints")] public bool? SupportsConditionalBreakpoints { get; set; }

    /**
     * The debug adapter supports breakpoints that break execution after a
     * specified number of hits.
     */
    [JsonPropertyName("supportsHitConditionalBreakpoints")] public bool? SupportsHitConditionalBreakpoints { get; set; }

    /**
     * The debug adapter supports a (side effect free) `evaluate` request for data
     * hovers.
     */
    [JsonPropertyName("supportsEvaluateForHovers")] public bool? SupportsEvaluateForHovers { get; set; }

    /**
     * Available exception filter options for the `setExceptionBreakpoints`
     * request.
     */
    [JsonPropertyName("exceptionBreakpointFilters")] public ExceptionBreakpointsFilter[]? ExceptionBreakpointFilters { get; set; }

    /**
     * The debug adapter supports stepping back via the `stepBack` and
     * `reverseContinue` requests.
     */
    [JsonPropertyName("supportsStepBack")] public bool? SupportsStepBack { get; set; }

    /**
     * The debug adapter supports setting a variable to a value.
     */
    [JsonPropertyName("supportsSetVariable")] public bool? SupportsSetVariable { get; set; }

    /**
     * The debug adapter supports restarting a frame.
     */
    [JsonPropertyName("supportsRestartFrame")] public bool? SupportsRestartFrame { get; set; }

    /**
     * The debug adapter supports the `gotoTargets` request.
     */
    [JsonPropertyName("supportsGotoTargetsRequest")] public bool? SupportsGotoTargetsRequest { get; set; }

    /**
     * The debug adapter supports the `stepInTargets` request.
     */
    [JsonPropertyName("supportsStepInTargetsRequest")] public bool? SupportsStepInTargetsRequest { get; set; }

    /**
     * The debug adapter supports the `completions` request.
     */
    [JsonPropertyName("supportsCompletionsRequest")] public bool? SupportsCompletionsRequest { get; set; }

    /**
     * The set of characters that should trigger completion in a REPL. If not
     * specified, the UI should assume the `.` character.
     */
    [JsonPropertyName("completionTriggerCharacters")] public string[]? CompletionTriggerCharacters { get; set; }

    /**
     * The debug adapter supports the `modules` request.
     */
    [JsonPropertyName("supportsModulesRequest")] public bool? SupportsModulesRequest { get; set; }

    /*
    /**
     * The set of additional module information exposed by the debug adapter.
     */
    //[JsonPropertyName("additionalModuleColumns")] public ColumnDescriptor[]? AdditionalModuleColumns { get; set; }

    /*
    /**
     * Checksum algorithms supported by the debug adapter.
     */
    //[JsonPropertyName("supportedChecksumAlgorithms")] public ChecksumAlgorithm[]? SupportedChecksumAlgorithms { get; set; }

    /**
     * The debug adapter supports the `restart` request. In this case a client
     * should not implement `restart` by terminating and relaunching the adapter
     * but by calling the `restart` request.
     */
    [JsonPropertyName("supportsRestartRequest")] public bool? SupportsRestartRequest { get; set; }

    /**
     * The debug adapter supports `exceptionOptions` on the
     * `setExceptionBreakpoints` request.
     */
    [JsonPropertyName("supportsExceptionOptions")] public bool? SupportsExceptionOptions { get; set; }

    /**
     * The debug adapter supports a `format` attribute on the `stackTrace`,
     * `variables`, and `evaluate` requests.
     */
    [JsonPropertyName("supportsValueFormattingOptions")] public bool? SupportsValueFormattingOptions { get; set; }

    /**
     * The debug adapter supports the `exceptionInfo` request.
     */
    [JsonPropertyName("supportsExceptionInfoRequest")] public bool? SupportsExceptionInfoRequest { get; set; }

    /**
     * The debug adapter supports the `terminateDebuggee` attribute on the
     * `disconnect` request.
     */
    [JsonPropertyName("supportTerminateDebuggee")] public bool? SupportTerminateDebuggee { get; set; }

    /**
     * The debug adapter supports the `suspendDebuggee` attribute on the
     * `disconnect` request.
     */
    [JsonPropertyName("supportSuspendDebuggee")] public bool? SupportSuspendDebuggee { get; set; }

    /**
     * The debug adapter supports the delayed loading of parts of the stack, which
     * requires that both the `startFrame` and `levels` arguments and the
     * `totalFrames` result of the `stackTrace` request are supported.
     */
    [JsonPropertyName("supportsDelayedStackTraceLoading")] public bool? SupportsDelayedStackTraceLoading { get; set; }

    /**
     * The debug adapter supports the `loadedSources` request.
     */
    [JsonPropertyName("supportsLoadedSourcesRequest")] public bool? SupportsLoadedSourcesRequest { get; set; }

    /**
     * The debug adapter supports log points by interpreting the `logMessage`
     * attribute of the `SourceBreakpoint`.
     */
    [JsonPropertyName("supportsLogPoints")] public bool? SupportsLogPoints { get; set; }

    /**
     * The debug adapter supports the `terminateThreads` request.
     */
    [JsonPropertyName("supportsTerminateThreadsRequest")] public bool? SupportsTerminateThreadsRequest { get; set; }

    /**
     * The debug adapter supports the `setExpression` request.
     */
    [JsonPropertyName("supportsSetExpression")] public bool? SupportsSetExpression { get; set; }

    /**
     * The debug adapter supports the `terminate` request.
     */
    [JsonPropertyName("supportsTerminateRequest")] public bool? SupportsTerminateRequest { get; set; }

    /**
     * The debug adapter supports data breakpoints.
     */
    [JsonPropertyName("supportsDataBreakpoints")] public bool? SupportsDataBreakpoints { get; set; }

    /**
     * The debug adapter supports the `readMemory` request.
     */
    [JsonPropertyName("supportsReadMemoryRequest")] public bool? SupportsReadMemoryRequest { get; set; }

    /**
     * The debug adapter supports the `writeMemory` request.
     */
    [JsonPropertyName("supportsWriteMemoryRequest")] public bool? SupportsWriteMemoryRequest { get; set; }

    /**
     * The debug adapter supports the `disassemble` request.
     */
    [JsonPropertyName("supportsDisassembleRequest")] public bool? SupportsDisassembleRequest { get; set; }

    /**
     * The debug adapter supports the `cancel` request.
     */
    [JsonPropertyName("supportsCancelRequest")] public bool? SupportsCancelRequest { get; set; }

    /**
     * The debug adapter supports the `breakpointLocations` request.
     */
    [JsonPropertyName("supportsBreakpointLocationsRequest")] public bool? SupportsBreakpointLocationsRequest { get; set; }

    /**
     * The debug adapter supports the `clipboard` context value in the `evaluate`
     * request.
     */
    [JsonPropertyName("supportsClipboardContext")] public bool? SupportsClipboardContext { get; set; }

    /**
     * The debug adapter supports stepping granularities (argument `granularity`)
     * for the stepping requests.
     */
    [JsonPropertyName("supportsSteppingGranularity")] public bool? SupportsSteppingGranularity { get; set; }

    /**
     * The debug adapter supports adding breakpoints based on instruction
     * references.
     */
    [JsonPropertyName("supportsInstructionBreakpoints")] public bool? SupportsInstructionBreakpoints { get; set; }

    /**
     * The debug adapter supports `filterOptions` as an argument on the
     * `setExceptionBreakpoints` request.
     */
    [JsonPropertyName("supportsExceptionFilterOptions")] public bool? SupportsExceptionFilterOptions { get; set; }

    /**
     * The debug adapter supports the `singleThread` property on the execution
     * requests (`continue`, `next`, `stepIn`, `stepOut`, `reverseContinue`,
     * `stepBack`).
     */
    [JsonPropertyName("supportsSingleThreadExecutionRequests")] public bool? SupportsSingleThreadExecutionRequests { get; set; }
}
