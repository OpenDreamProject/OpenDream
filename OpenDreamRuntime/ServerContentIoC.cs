using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Procs.DebugAdapter;
using OpenDreamRuntime.Resources;

namespace OpenDreamRuntime {
    public static class ServerContentIoC {
        public static void Register(bool unitTests = false) {
            IoCManager.Register<DreamManager>();
            IoCManager.Register<DreamObjectTree, DreamObjectTree>();
            IoCManager.Register<AtomManager, AtomManager>();
            IoCManager.Register<IProcScheduler, ProcScheduler>();
            IoCManager.Register<DreamResourceManager, DreamResourceManager>();
            IoCManager.Register<IDreamDebugManager, DreamDebugManager>();

            #if DEBUG
            IoCManager.Register<LocalHostConGroup>();
            #endif

            if (!unitTests) {
                // Unit tests use their own version
                IoCManager.Register<IDreamMapManager, DreamMapManager>();
            }
        }
    }
}
