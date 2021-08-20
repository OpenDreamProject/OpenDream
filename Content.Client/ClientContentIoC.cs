using Content.Client.Input;
using Content.Client.Interface;
using Robust.Shared.IoC;

namespace Content.Client {
    internal static class ClientContentIoC {
        public static void Register() {
            IoCManager.Register<IDreamInterfaceManager, DreamInterfaceManager>();
            IoCManager.Register<DreamInterfaceManager, DreamInterfaceManager>();
            IoCManager.Register<IDreamMacroManager, DreamMacroManager>();
        }
    }
}
