using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Resources;

namespace OpenDreamRuntime {
    public static class ServerContentIoC {
        public static void Register() {
            IoCManager.Register<IDreamManager, DreamManager>();
            IoCManager.Register<IAtomManager, AtomManager>();
            IoCManager.Register<IDreamMapManager, DreamMapManager>();
            IoCManager.Register<IProcScheduler, ProcScheduler>();
            IoCManager.Register<DreamResourceManager, DreamResourceManager>();

            #if DEBUG
            IoCManager.Register<LocalHostConGroup>();
            #endif
        }
    }
}
