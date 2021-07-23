using Content.Server.Dream;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server {
    public class EntryPoint : GameServer {
        private IDreamManager _dreamManager;

        public override void Init() {
            IoCManager.Resolve<IComponentFactory>().DoAutoRegistrations();

            ServerContentIoC.Register();

            // This needs to happen after all IoC registrations, but before IoC.BuildGraph();
            foreach (var callback in TestingCallbacks)
            {
                var cast = (ServerModuleTestingCallbacks) callback;
                cast.ServerBeforeIoC?.Invoke();
            }

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
