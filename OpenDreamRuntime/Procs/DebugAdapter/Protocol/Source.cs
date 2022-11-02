using System.Text.Json.Serialization;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

public sealed class Source {
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("path")] public string? Path { get; set; }
    [JsonPropertyName("sourceReference")] public int? SourceReference { get; set; }
    [JsonPropertyName("presentationHint")] public string? PresentationHint { get; set; }
    [JsonPropertyName("origin")] public string? Origin { get; set; }
    [JsonPropertyName("sources")] public Source[]? Sources { get; set; }
    [JsonPropertyName("adapterData")] public object? AdapterData { get; set; }
    [JsonPropertyName("checksums")] public Checksum[]? Checksums { get; set; }

    public Source(string name, string path) {
        Name = name;
        Path = path;
    }
}
