using System.Globalization;
using OpenDreamClient.Audio;
using OpenDreamClient.Interface;
using OpenDreamClient.Rendering;
using OpenDreamClient.Resources;
using OpenDreamClient.States;
using OpenDreamShared;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.WebView;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Timing;

namespace OpenDreamClient {
    public sealed class EntryPoint : GameClient {
        [Dependency]
        private readonly IDreamInterfaceManager _dreamInterface = default!;
        [Dependency]
        private readonly IDreamResourceManager _dreamResource = default!;

        private const string UserAgent =
            "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.2; WOW64; Trident/7.0; .NET4.0C; .NET4.0E; .NET CLR 2.0.50727; .NET CLR 3.0.30729; .NET CLR 3.5.30729) ";

        public override void PreInit()
        {
            var config = IoCManager.Resolve<IConfigurationManager>();
            if (config.GetCVar(OpenDreamCVars.SpoofIEUserAgent))
            {
                config.OverrideDefault(WCVars.WebUserAgentOverride, UserAgent);
            }
        }

        public override void Init() {
            IoCManager.Resolve<IConfigurationManager>().OverrideDefault(CVars.NetPredict, false);

            IComponentFactory componentFactory = IoCManager.Resolve<IComponentFactory>();
            componentFactory.DoAutoRegistrations();

            ClientContentIoC.Register();

            // This needs to happen after all IoC registrations, but before IoC.BuildGraph();
            foreach (var callback in TestingCallbacks) {
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
            ILightManager lightManager = IoCManager.Resolve<ILightManager>();
            lightManager.Enabled = true;
            lightManager.DrawLighting = false;
            lightManager.DrawShadows = true;

            IOverlayManager overlayManager = IoCManager.Resolve<IOverlayManager>();
            overlayManager.AddOverlay(new DreamViewOverlay());
            overlayManager.AddOverlay(new DreamScreenOverlay());

            _dreamInterface.Initialize();
            IoCManager.Resolve<IDreamSoundEngine>().Initialize();
        }

        protected override void Dispose(bool disposing) {
            _dreamResource.Shutdown();
        }

        public override void Update(ModUpdateLevel level, FrameEventArgs frameEventArgs) {
            if (level == ModUpdateLevel.FramePostEngine) {
                _dreamInterface.FrameUpdate(frameEventArgs);
            }
        }
    }
}
