using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

[Virtual]
public class Request : ProtocolMessage {
    [JsonPropertyName("command")] public required string Command { get; set; }

    protected Request() : base("request") { }

    public static Request? DeserializeRequest(JsonDocument json) {
        Request? request = json.Deserialize<Request>();
        if (request == null)
            return null;

        return request.Command switch {
            "initialize" => json.Deserialize<RequestInitialize>(),
            "launch" => json.Deserialize<RequestLaunch>(),
            "disconnect" => json.Deserialize<RequestDisconnect>(),
            "setBreakpoints" => json.Deserialize<RequestSetBreakpoints>(),
            "setFunctionBreakpoints" => json.Deserialize<RequestSetFunctionBreakpoints>(),
            "setExceptionBreakpoints" => json.Deserialize<RequestSetExceptionBreakpoints>(),
            "configurationDone" => json.Deserialize<RequestConfigurationDone>(),
            "threads" => json.Deserialize<RequestThreads>(),
            "continue" => json.Deserialize<RequestContinue>(),
            "pause" => json.Deserialize<RequestPause>(),
            "stackTrace" => json.Deserialize<RequestStackTrace>(),
            "scopes" => json.Deserialize<RequestScopes>(),
            "variables" => json.Deserialize<RequestVariables>(),
            "exceptionInfo" => json.Deserialize<RequestExceptionInfo>(),
            "next" => json.Deserialize<RequestNext>(),
            "stepIn" => json.Deserialize<RequestStepIn>(),
            "stepOut" => json.Deserialize<RequestStepOut>(),
            "disassemble" => json.Deserialize<RequestDisassemble>(),
            // Caller will fail to recognize it and can respond with `success: false`.
            _ => request,
        };
    }

    public void RespondError(DebugAdapterClient client, string errorText) {
        client.SendMessage(Response.NewError(this, new { error = new Message(errorText, showUser: true) }, errorText));
    }
}
