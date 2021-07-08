using OpenDreamClient.Audio;
using OpenDreamClient.Dream;
using OpenDreamClient.Resources;
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

            IoCManager.Register<OpenDream>();
            IoCManager.Register<DreamSoundEngine>();
            IoCManager.Register<DreamStateManager>();
            IoCManager.Register<DreamResourceManager>();
            IoCManager.Register<CefManager>();
            IoCManager.Register<DreamUserInterfaceStateManager>();
        }
    }
}
