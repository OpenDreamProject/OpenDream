using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

[UsedImplicitly]
public sealed class ExceptionDetails {
    /**
     * Message contained in the exception.
     */
    [JsonPropertyName("message")] public string? Message { get; set; }

    /**
     * Short type name of the exception object.
     */
    [JsonPropertyName("typeName")] public string? TypeName { get; set; }

    /**
     * Fully-qualified type name of the exception object.
     */
    [JsonPropertyName("fullTypeName")] public string? FullTypeName { get; set; }

    /**
     * An expression that can be evaluated in the current scope to obtain the
     * exception object.
     */
    [JsonPropertyName("evaluateName")] public string? EvaluateName { get; set; }

    /**
     * Stack trace at the time the exception was thrown.
     */
    [JsonPropertyName("stackTrace")] public string? StackTrace { get; set; }

    /**
     * Details of the exception contained by this exception, if any.
     */
    [JsonPropertyName("innerException")] public ExceptionDetails[]? InnerException { get; set; }
}
