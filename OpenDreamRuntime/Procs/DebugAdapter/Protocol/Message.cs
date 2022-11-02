using System.Text.Json.Serialization;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

public struct Message {
    private static int _idCounter = 0;

    [JsonPropertyName("id")] public int Id { get; set; } = _idCounter++;
    [JsonPropertyName("format")] public string Format { get; set; }
    [JsonPropertyName("showUser")] public bool ShowUser { get; set; }

    public Message(string format, bool showUser = false) {
        Format = format;
        ShowUser = showUser;
    }
}
