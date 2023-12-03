using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

[UsedImplicitly]
public sealed class RequestSetBreakpoints : Request {
    [JsonPropertyName("arguments")] public required RequestSetBreakpointsArguments Arguments { get; set; }

    [UsedImplicitly]
    public sealed class RequestSetBreakpointsArguments {
        [JsonPropertyName("source")] public required Source Source { get; set; }
        [JsonPropertyName("breakpoints")] public SourceBreakpoint[]? Breakpoints { get; set; }
        [JsonPropertyName("sourceModified")] public bool SourceModified { get; set; }
    }

    public void Respond(DebugAdapterClient client, Breakpoint[] breakpoints) {
        client.SendMessage(Response.NewSuccess(this, new { breakpoints = breakpoints }));
    }
}
