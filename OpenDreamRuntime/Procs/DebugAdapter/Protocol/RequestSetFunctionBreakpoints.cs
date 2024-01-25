using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

[UsedImplicitly]
public sealed class RequestSetFunctionBreakpoints : Request {
    [JsonPropertyName("arguments")] public required RequestSetBreakpointsArguments Arguments { get; set; }

    [UsedImplicitly]
    public sealed class RequestSetBreakpointsArguments {
        [JsonPropertyName("breakpoints")] public required FunctionBreakpoint[] Breakpoints { get; set; }
    }

    public void Respond(DebugAdapterClient client, Breakpoint[] breakpoints) {
        client.SendMessage(Response.NewSuccess(this, new { breakpoints = breakpoints }));
    }
}
