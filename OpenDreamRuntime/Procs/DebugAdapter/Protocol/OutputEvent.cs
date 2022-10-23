namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

public sealed class OutputEvent : Event {
    public OutputEvent(string category, string output) : base("output") {
        Body = new Dictionary<string, object?> {
            {"category", category},
            {"output", output}
        };
    }
}
