using System.Globalization;
using JetBrains.Annotations;
using OpenDreamClient.Renderer;
using OpenDreamClient.States;
using Robust.Client;
using Robust.Client.CEF;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace OpenDreamClient
{
    [UsedImplicitly]
    public class EntryPoint : GameClient
    {
        public override void PreInit()
        {
            IoCManager.Resolve<IComponentFactory>().DoAutoRegistrations();

            IoCManager.Resolve<IClyde>().SetWindowTitle("OpenDream");
            IoCManager.Resolve<IUserInterfaceManager>().Stylesheet = DreamStylesheet.Make();
        }

        public override void Init()
        {
            ClientOpenDreamIoC.Register();
            IoCManager.BuildGraph();

            //IoCManager.Resolve<CefManager>().Initialize();
            IoCManager.Resolve<DreamUserInterfaceStateManager>().Initialize();
        }

        public override void PostInit()
        {
            IoCManager.Resolve<ILightManager>().Enabled = false;

            IoCManager.Resolve<IBaseClient>().StartSinglePlayer();

            var map = IoCManager.Resolve<IMapManager>().CreateMap();
            var dummyEye = IoCManager.Resolve<IEntityManager>().SpawnEntity(null, new MapCoordinates(Vector2.Zero, map));
            dummyEye.AddComponent<EyeComponent>().Current = true;

            IoCManager.Resolve<OpenDream>().ConnectToServer("127.0.0.1", 25566);
            IoCManager.Resolve<IOverlayManager>().AddOverlay(new DreamOverlay());
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            //IoCManager.Resolve<CefManager>().Shutdown();
        }

        public override void Update(ModUpdateLevel level, FrameEventArgs frameEventArgs)
        {
            if (level != ModUpdateLevel.PreEngine)
                return;

            IoCManager.Resolve<OpenDream>().Update(frameEventArgs.DeltaSeconds);
            //IoCManager.Resolve<CefManager>().Update();
        }
    }
}
