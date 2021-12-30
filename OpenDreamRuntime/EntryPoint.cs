using OpenDreamRuntime.Input;
using Robust.Server.ServerStatus;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace OpenDreamRuntime {
    public class EntryPoint : GameServer {
        [Dependency]
        private IDreamManager _dreamManager;
        private DreamCommandSystem _commandSystem;

        public override void Init() {
            IoCManager.Resolve<IStatusHost>().SetAczInfo(
                "Content.Client", new []{"OpenDreamClient", "OpenDreamShared"});

            IComponentFactory componentFactory = IoCManager.Resolve<IComponentFactory>();
            componentFactory.DoAutoRegistrations();

            ServerContentIoC.Register();

            // This needs to happen after all IoC registrations, but before IoC.BuildGraph();
            foreach (var callback in TestingCallbacks)
            {
                var cast = (ServerModuleTestingCallbacks) callback;
                cast.ServerBeforeIoC?.Invoke();
            }

            IoCManager.BuildGraph();
            IoCManager.InjectDependencies(this);
            componentFactory.GenerateNetIds();

            // Disable since disabling prediction causes timing errors otherwise.
            var cfg = IoCManager.Resolve<IConfigurationManager>();
            cfg.SetCVar(CVars.NetLogLateMsg, false);
        }

        public override void PostInit() {
            _commandSystem = EntitySystem.Get<DreamCommandSystem>();
            _dreamManager.Initialize();
        }

        protected override void Dispose(bool disposing) {
            _dreamManager.Shutdown();
        }

        public override void Update(ModUpdateLevel level, FrameEventArgs frameEventArgs) {
            if (level == ModUpdateLevel.PreEngine)
            {
                _commandSystem.RunRepeatingCommands();
                _dreamManager.Update();
            }
        }
    }
}
