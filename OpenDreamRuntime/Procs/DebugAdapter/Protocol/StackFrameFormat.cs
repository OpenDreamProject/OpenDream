using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

[UsedImplicitly]
public sealed class StackFrameFormat : ValueFormat {
    /**
     * Displays parameters for the stack frame.
     */
    [JsonPropertyName("parameters")] public bool? Parameters { get; set; }

    /**
     * Displays the types of parameters for the stack frame.
     */
    [JsonPropertyName("parameterTypes")] public bool? ParameterTypes { get; set; }

    /**
     * Displays the names of parameters for the stack frame.
     */
    [JsonPropertyName("parameterNames")] public bool? ParameterNames { get; set; }

    /**
     * Displays the values of parameters for the stack frame.
     */
    [JsonPropertyName("parameterValues")] public bool? ParameterValues { get; set; }

    /**
     * Displays the line number of the stack frame.
     */
    [JsonPropertyName("line")] public bool? Line { get; set; }

    /**
     * Displays the module of the stack frame.
     */
    [JsonPropertyName("module")] public bool? Module { get; set; }

    /**
     * Includes all stack frames, including those the debug adapter might
     * otherwise hide.
     */
    [JsonPropertyName("includeAll")] public bool? IncludeAll { get; set; }
}
