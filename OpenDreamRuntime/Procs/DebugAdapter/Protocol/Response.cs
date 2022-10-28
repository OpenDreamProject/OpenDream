using System.Text.Json.Serialization;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

public sealed class Response : ProtocolMessage {
    [JsonPropertyName("request_seq")] public int RequestMessageId { get; set; }
    [JsonPropertyName("success")] public bool Success { get; set; }
    [JsonPropertyName("command")] public string Command { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
    [JsonPropertyName("body")] public object? Body { get; set; }

    private Response(Request respondingTo) : base("response") {
        RequestMessageId = respondingTo.Seq;
        Command = respondingTo.Command;
    }

    public static Response NewSuccess(Request respondingTo, object? body = null) {
        return new Response(respondingTo) { Success = true, Body = body };
    }

    public static Response NewError(Request respondingTo, object? body = null, string? code = null) {
        return new Response(respondingTo) { Success = false, Body = body, Message = code };
    }
}
