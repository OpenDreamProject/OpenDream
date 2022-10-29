namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

public sealed class RequestThreads : Request {
    public void Respond(DebugAdapterClient client, Thread[] threads) {
        client.SendMessage(Response.NewSuccess(this, new { threads }));
    }
}
