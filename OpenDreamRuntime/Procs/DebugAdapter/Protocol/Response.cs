using System.Text.Json.Serialization;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

[Virtual]
public class Response : ProtocolMessage {
    [JsonPropertyName("request_seq")] public int RequestMessageId { get; set; }
    [JsonPropertyName("success")] public bool Success { get; set; }
    [JsonPropertyName("command")] public string Command { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
    [JsonPropertyName("body")] public object? Body { get; set; }

    protected Response(Request respondingTo, bool success, string? message = null) : base("response") {
        RequestMessageId = respondingTo.Seq;
        Success = success;
        Command = respondingTo.Command;
        Message = message;
    }
}
