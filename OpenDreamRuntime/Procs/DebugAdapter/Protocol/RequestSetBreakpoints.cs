using System.Text.Json.Serialization;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

public sealed class RequestSetBreakpoints : Request {
    [JsonPropertyName("arguments")] public RequestSetBreakpointsArguments Arguments { get; set; }

    public sealed class RequestSetBreakpointsArguments {
        [JsonPropertyName("source")] public Source Source { get; set; }
        [JsonPropertyName("breakpoints")] public SourceBreakpoint[]? Breakpoints { get; set; }
        [JsonPropertyName("sourceModified")] public bool SourceModified { get; set; }
    }

    private sealed class SetBreakpointsResponse : Response {
        public SetBreakpointsResponse(Request respondingTo, Breakpoint[] breakpoints) : base(respondingTo, false) {
            Body = new Dictionary<string, object?> {
                {"breakpoints", breakpoints}
            };
        }
    }

    public void Respond(DebugAdapterClient client, Breakpoint[] breakpoints) {
        client.SendMessage(new SetBreakpointsResponse(this, breakpoints));
    }
}
