using OpenDreamRuntime.Input;
using OpenDreamShared;
using Robust.Server.ServerStatus;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Timing;

namespace OpenDreamRuntime {
    public sealed class EntryPoint : GameServer {
        [Dependency] private readonly IDreamManager _dreamManager = default!;
        [Dependency] private readonly IConfigurationManager _configManager = default!;

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

            var cfg = IoCManager.Resolve<IConfigurationManager>();
            cfg.OverrideDefault(CVars.NetLogLateMsg, false); // Disable since disabling prediction causes timing errors otherwise.
            cfg.OverrideDefault(CVars.GameAutoPauseEmpty, false); // TODO: world.sleep_offline can control this
        }

        public override void PostInit() {
            _commandSystem = EntitySystem.Get<DreamCommandSystem>();
            _dreamManager.Initialize(_configManager.GetCVar<string>(OpenDreamCVars.JsonPath));
        }

        protected override void Dispose(bool disposing) {
            _dreamManager.Shutdown();
        }

        public override void Update(ModUpdateLevel level, FrameEventArgs frameEventArgs) {
            if (level == ModUpdateLevel.PostEngine)
            {
                _commandSystem.RunRepeatingCommands();
                _dreamManager.Update();
            }
        }
    }
}
