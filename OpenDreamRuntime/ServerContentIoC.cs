using OpenDreamRuntime.Resources;
using Robust.Shared.IoC;

namespace OpenDreamRuntime {
    public static class ServerContentIoC {
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
