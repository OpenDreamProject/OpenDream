using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

[Virtual]
public class Request : ProtocolMessage {
    [JsonPropertyName("command")] public string Command { get; set; }

    public Request() : base("request") { }

    public static Request? DeserializeRequest(JsonDocument json) {
        Request? request = json.Deserialize<Request>();
        if (request == null)
            return null;

        switch (request.Command) {
            case "initialize": return json.Deserialize<RequestInitialize>();
            case "launch": return json.Deserialize<RequestLaunch>();
            case "disconnect": return json.Deserialize<RequestDisconnect>();
            case "setBreakpoints": return json.Deserialize<RequestSetBreakpoints>();
            default: throw new InvalidOperationException($"Unrecognized \"command\" value: {request.Command}");
        }
    }

    private sealed class ErrorResponse : Response {
        public ErrorResponse(Request respondingTo, string error) : base(respondingTo, false) {
            Body = new Dictionary<string, object?> {
                {"error", new Message(error, showUser: true)}
            };
        }
    }

    public void RespondError(DebugAdapterClient client, string error) {
        client.SendMessage(new ErrorResponse(this, error));
    }
}
