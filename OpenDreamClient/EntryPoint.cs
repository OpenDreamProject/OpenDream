using System.Globalization;
using OpenDreamClient.Interface;
using OpenDreamClient.Rendering;
using OpenDreamClient.Resources;
using OpenDreamClient.States;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace OpenDreamClient {
    public class EntryPoint : GameClient
    {
        [Dependency]
        private readonly IDreamInterfaceManager _dreamInterface = default!;
        [Dependency]
        private readonly IDreamResourceManager _dreamResource = default!;

        public override void Init() {
            IComponentFactory componentFactory = IoCManager.Resolve<IComponentFactory>();
            componentFactory.DoAutoRegistrations();

            ClientContentIoC.Register();

            // This needs to happen after all IoC registrations, but before IoC.BuildGraph();
            foreach (var callback in TestingCallbacks)
            {
                var cast = (ClientModuleTestingCallbacks) callback;
                cast.ClientBeforeIoC?.Invoke();
            }

            IoCManager.BuildGraph();
            IoCManager.InjectDependencies(this);

            IoCManager.Resolve<DreamUserInterfaceStateManager>().Initialize();

            componentFactory.GenerateNetIds();

            _dreamResource.Initialize();

            // Load localization. Needed for some engine texts, such as the ones in Robust ViewVariables.
            IoCManager.Resolve<ILocalizationManager>().LoadCulture(new CultureInfo("en-US"));

            IoCManager.Resolve<IClyde>().SetWindowTitle("OpenDream");
            IoCManager.Resolve<IUserInterfaceManager>().Stylesheet = DreamStylesheet.Make();

            // Game logic runs all single threaded, disable prediction to reduce CPU load and lag.
            var cfg = IoCManager.Resolve<IConfigurationManager>();
            cfg.SetCVar(CVars.NetPredict, false);
        }

        public override void PostInit() {
            IoCManager.Resolve<ILightManager>().Enabled = false;

            IoCManager.Resolve<IOverlayManager>().AddOverlay(new DreamMapOverlay());
            _dreamInterface.Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            _dreamResource.Shutdown();
        }

        public override void Update(ModUpdateLevel level, FrameEventArgs frameEventArgs)
        {
            if (level == ModUpdateLevel.FramePostEngine)
            {
                _dreamInterface.FrameUpdate(frameEventArgs);
            }
        }
    }
}
