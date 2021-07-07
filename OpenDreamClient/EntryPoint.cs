using System.Globalization;
using JetBrains.Annotations;
using OpenDreamClient.States;
using Robust.Client;
using Robust.Client.CEF;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Timing;

namespace OpenDreamClient
{
    [UsedImplicitly]
    public class EntryPoint : GameClient
    {
        public override void PreInit()
        {
            IoCManager.Resolve<IClyde>().SetWindowTitle("OpenDream");
            IoCManager.Resolve<IUserInterfaceManager>().Stylesheet = DreamStylesheet.Make();
        }

        public override void Init()
        {
            ClientOpenDreamIoC.Register();
            IoCManager.BuildGraph();

            IoCManager.Resolve<CefManager>().Initialize();
            IoCManager.Resolve<DreamUserInterfaceStateManager>().Initialize();
        }

        public override void PostInit()
        {
            IoCManager.Resolve<IBaseClient>().StartSinglePlayer();
            IoCManager.Resolve<OpenDream>().ConnectToServer("127.0.0.1", 25566);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            IoCManager.Resolve<CefManager>().Shutdown();
        }

        public override void Update(ModUpdateLevel level, FrameEventArgs frameEventArgs)
        {
            if (level != ModUpdateLevel.PreEngine)
                return;

            IoCManager.Resolve<OpenDream>().Update(frameEventArgs.DeltaSeconds);
            IoCManager.Resolve<CefManager>().Update();
        }
    }
}
