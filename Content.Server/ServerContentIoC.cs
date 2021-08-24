using Content.Server.Dream;
using Content.Server.Dream.Resources;
using Robust.Shared.IoC;

namespace Content.Server {
    internal static class ServerContentIoC {
        public static void Register() {
            IoCManager.Register<IDreamManager, DreamManager>();
            IoCManager.Register<IAtomManager, AtomManager>();
            IoCManager.Register<IDreamMapManager, DreamMapManager>();
            IoCManager.Register<DreamResourceManager, DreamResourceManager>();

            #if DEBUG
            IoCManager.Register<LocalHostConGroup>();
            #endif
        }
    }
}
