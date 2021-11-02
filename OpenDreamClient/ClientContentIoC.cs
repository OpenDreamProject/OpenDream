using OpenDreamClient.Input;
using OpenDreamClient.Interface;
using OpenDreamClient.Resources;
using OpenDreamClient.States;
using Robust.Client.WebView;
using Robust.Shared.IoC;

namespace OpenDreamClient {
    internal static class ClientContentIoC {
        public static void Register() {
            IoCManager.Register<IDreamInterfaceManager, DreamInterfaceManager>();
            IoCManager.Register<IDreamMacroManager, DreamMacroManager>();
            IoCManager.Register<IDreamResourceManager, DreamResourceManager>();
            IoCManager.Register<DreamUserInterfaceStateManager>();
        }
    }
}
