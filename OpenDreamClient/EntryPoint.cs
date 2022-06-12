using System.Globalization;
using OpenDreamClient.Audio;
using OpenDreamClient.Interface;
using OpenDreamClient.Rendering;
using OpenDreamClient.Resources;
using OpenDreamClient.States;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Timing;

namespace OpenDreamClient {
    public sealed class EntryPoint : GameClient
    {
        [Dependency]
        private readonly IDreamInterfaceManager _dreamInterface = default!;
        [Dependency]
        private readonly IDreamResourceManager _dreamResource = default!;

        public override void Init() {
            IoCManager.Resolve<IConfigurationManager>().OverrideDefault(CVars.NetPredict, false);

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
        }

        public override void PostInit() {
            IoCManager.Resolve<ILightManager>().Enabled = false;

            IoCManager.Resolve<IOverlayManager>().AddOverlay(new DreamViewOverlay());

            _dreamInterface.Initialize();
            IoCManager.Resolve<IDreamSoundEngine>().Initialize();
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
