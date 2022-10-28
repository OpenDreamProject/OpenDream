namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

public sealed class ODReadyEvent : Event {
    public ODReadyEvent(int port) : base("$opendream/ready") {
        Body = new Dictionary<string, object?> {
            {"gamePort", port},
        };
    }
}
