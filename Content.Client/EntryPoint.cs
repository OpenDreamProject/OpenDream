using System.Globalization;
using Content.Client.Interface;
using Content.Client.Rendering;
using Robust.Client;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client {
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

            componentFactory.GenerateNetIds();

            _dreamResource.Initialize();

            // Load localization. Needed for some engine texts, such as the ones in Robust ViewVariables.
            IoCManager.Resolve<ILocalizationManager>().LoadCulture(new CultureInfo("en-US"));

            IoCManager.Resolve<IClyde>().SetWindowTitle("OpenDream");
            IoCManager.Resolve<IUserInterfaceManager>().Stylesheet = DreamStylesheet.Make();
        }

        public override void PostInit() {
            IoCManager.Resolve<ILightManager>().Enabled = false;

            IoCManager.Resolve<IOverlayManager>().AddOverlay(new DreamMapOverlay());
            _dreamInterface.Initialize();
            _dreamInterface.LoadDMF(new ResourcePath("/Game/interface.dmf")); //TODO: Don't hardcode interface.dmf
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
