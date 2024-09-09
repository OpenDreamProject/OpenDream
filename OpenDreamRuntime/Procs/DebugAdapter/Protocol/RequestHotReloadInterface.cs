﻿using JetBrains.Annotations;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

[UsedImplicitly]
public sealed class RequestHotReloadInterface : Request {
    public void Respond(DebugAdapterClient client) {
        client.SendMessage(Response.NewSuccess(this));
    }
}
