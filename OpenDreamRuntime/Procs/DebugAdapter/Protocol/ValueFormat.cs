using System.Text.Json.Serialization;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

[Virtual]
public class ValueFormat {
    /**
     * Display the value in hex.
     */
    [JsonPropertyName("hex")] public bool? Hex { get; set; }
}
