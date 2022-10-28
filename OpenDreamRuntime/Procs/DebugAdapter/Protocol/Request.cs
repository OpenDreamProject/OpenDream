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
            case "configurationDone": return json.Deserialize<RequestConfigurationDone>();
            default: return request;  // Caller will fail to recognize it and can respond with `success: false`;
        }
    }

    public void RespondError(DebugAdapterClient client, string errorText) {
        client.SendMessage(Response.NewError(this, new { error = new Message(errorText, showUser: true) }, errorText));
    }
}
