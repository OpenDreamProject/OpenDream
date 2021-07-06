using OpenDreamClient.States;
using OpenDreamShared;
using Robust.Client.CEF;
using Robust.Shared.IoC;

namespace OpenDreamClient
{
    public static class ClientOpenDreamIoC
    {
        public static void Register()
        {
            SharedOpenDreamIoC.Register();

            IoCManager.Register<CefManager>();
            IoCManager.Register<DreamUserInterfaceStateManager>();
        }
    }
}
