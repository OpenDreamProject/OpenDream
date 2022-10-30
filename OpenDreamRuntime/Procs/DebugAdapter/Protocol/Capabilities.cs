using System.Text.Json.Serialization;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

public sealed class Capabilities {
    [JsonPropertyName("supportsFunctionBreakpoints")] public bool? SupportsFunctionBreakpoints { get; set; }
    [JsonPropertyName("supportsConfigurationDoneRequest")] public bool? SupportsConfigurationDoneRequest { get; set; }
    [JsonPropertyName("supportsExceptionInfoRequest")] public bool? SupportsExceptionInfoRequest { get; set; }
}
