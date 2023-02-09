namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

public sealed class RequestConfigurationDone : Request {
    public void Respond(DebugAdapterClient client) {
        client.SendMessage(Response.NewSuccess(this));
    }
}
