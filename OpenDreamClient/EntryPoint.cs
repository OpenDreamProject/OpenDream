using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;

namespace OpenDreamClient
{
    [UsedImplicitly]
    public class EntryPoint : GameClient
    {
        public override void PreInit()
        {
            IoCManager.Resolve<IClyde>().SetWindowTitle("OpenDream");
        }

        public override void Init()
        {
            ClientOpenDreamIoC.Register();

            IoCManager.BuildGraph();
        }

        public override void PostInit()
        {

        }
    }
}
