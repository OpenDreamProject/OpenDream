#nullable enable
using System;
using OpenDreamShared;

namespace OpenDreamRuntime
{
    public sealed class ServerModuleTestingCallbacks : SharedModuleTestingCallbacks
    {
        public Action? ServerBeforeIoC { get; set; }
    }
}
