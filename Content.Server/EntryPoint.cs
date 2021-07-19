using Content.Server.Dream;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server {
    class EntryPoint : GameServer {
        private IDreamManager _dreamManager;

        public override void Init() {
            IoCManager.Resolve<IComponentFactory>().DoAutoRegistrations();

            ServerContentIoC.Register();
            IoCManager.BuildGraph();
        }

        public override void PostInit() {
            _dreamManager = IoCManager.Resolve<IDreamManager>();
            _dreamManager.Initialize();
        }

        protected override void Dispose(bool disposing) {
            _dreamManager.Shutdown();
        }
    }
}
