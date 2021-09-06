using Content.Client.Audio;
using Content.Client.Input;
using Content.Client.Interface;
using Content.Client.Resources;
using Robust.Shared.IoC;

namespace Content.Client {
    internal static class ClientContentIoC {
        public static void Register() {
            IoCManager.Register<IDreamInterfaceManager, DreamInterfaceManager>();
            IoCManager.Register<DreamInterfaceManager, DreamInterfaceManager>();
            IoCManager.Register<IDreamMacroManager, DreamMacroManager>();
            IoCManager.Register<IDreamResourceManager, DreamResourceManager>();
            IoCManager.Register<IDreamSoundEngine, DreamSoundEngine>();
        }
    }
}
