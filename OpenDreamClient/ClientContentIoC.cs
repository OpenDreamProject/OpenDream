using OpenDreamClient.Audio;
using OpenDreamClient.Input;
using OpenDreamClient.Interface;
using OpenDreamClient.Resources;
using OpenDreamClient.States;

namespace OpenDreamClient;

internal static class ClientContentIoC {
    public static void Register() {
        IoCManager.Register<IDreamInterfaceManager, DreamInterfaceManager>();
        IoCManager.Register<IClickMapManager, ClickMapManager>();
        IoCManager.Register<IDreamResourceManager, DreamResourceManager>();
        IoCManager.Register<DreamUserInterfaceStateManager>();
        IoCManager.Register<IDreamSoundEngine, DreamSoundEngine>();
    }
}
