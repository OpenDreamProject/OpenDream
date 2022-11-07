using System.Text.Json.Serialization;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

public sealed class Checksum {
    [JsonPropertyName("algorithm")] public string Algorithm { get; set; }
    [JsonPropertyName("checksum")] public string ChecksumValue { get; set; }
}
