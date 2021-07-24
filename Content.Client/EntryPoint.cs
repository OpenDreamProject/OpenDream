using Content.Client.Rendering;
using Robust.Client;
using Robust.Client.Graphics;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client {
    public class EntryPoint : GameClient {
        public override void Init() {
            IComponentFactory componentFactory = IoCManager.Resolve<IComponentFactory>();
            componentFactory.DoAutoRegistrations();

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

            IoCManager.Resolve<IBaseClient>().ConnectToServer("127.0.0.1", 25566);
            IoCManager.Resolve<IOverlayManager>().AddOverlay(new DreamMapOverlay());
        }
    }
}
