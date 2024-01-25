using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

[UsedImplicitly]
public sealed class Checksum {
    [JsonPropertyName("algorithm")] public required string Algorithm { get; set; }
    [JsonPropertyName("checksum")] public required string ChecksumValue { get; set; }
}
