using JetBrains.Annotations;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

[UsedImplicitly]
public sealed class RequestThreads : Request {
    public void Respond(DebugAdapterClient client, IEnumerable<Thread> threads) {
        client.SendMessage(Response.NewSuccess(this, new { threads }));
    }
}
