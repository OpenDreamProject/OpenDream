#nullable enable
using OpenDreamShared;

namespace OpenDreamClient
{
    public sealed class ClientModuleTestingCallbacks : SharedModuleTestingCallbacks
    {
        public Action? ClientBeforeIoC { get; set; }
    }
}
