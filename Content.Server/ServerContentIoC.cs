using Content.Server.Dream;
using Robust.Shared.IoC;

namespace Content.Server {
    internal static class ServerContentIoC {
        public static void Register() {
            IoCManager.Register<IDreamManager, DreamManager>();
            IoCManager.Register<IAtomManager, AtomManager>();
            IoCManager.Register<IDreamMapManager, DreamMapManager>();
        }
    }
}
