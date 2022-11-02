using System.Text.Json.Serialization;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

public sealed class RequestSetFunctionBreakpoints : Request {
    [JsonPropertyName("arguments")] public RequestSetBreakpointsArguments Arguments { get; set; }

    public sealed class RequestSetBreakpointsArguments {
        [JsonPropertyName("breakpoints")] public FunctionBreakpoint[] Breakpoints { get; set; }
    }

    public void Respond(DebugAdapterClient client, Breakpoint[] breakpoints) {
        client.SendMessage(Response.NewSuccess(this, new { breakpoints = breakpoints }));
    }
}
