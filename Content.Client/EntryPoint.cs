using Content.Client.Interface;
using Content.Client.Rendering;
using Robust.Client;
using Robust.Client.Graphics;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Content.Client {
    public class EntryPoint : GameClient {
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
            componentFactory.GenerateNetIds();

            IoCManager.Resolve<IClyde>().SetWindowTitle("OpenDream");
            //IoCManager.Resolve<IUserInterfaceManager>().Stylesheet = DreamStylesheet.Make();
        }

        public override void PostInit() {
            IoCManager.Resolve<ILightManager>().Enabled = false;

            IoCManager.Resolve<IOverlayManager>().AddOverlay(new DreamMapOverlay());
            IoCManager.Resolve<IDreamInterfaceManager>().LoadDMF(new ResourcePath("/Game/interface.dmf")); //TODO: Don't hardcode interface.dmf
        }
    }
}
