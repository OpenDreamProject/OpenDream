using JetBrains.Annotations;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace OpenDreamRuntime
{
    [UsedImplicitly]
    public class EntryPoint : GameServer
    {
        public override void PreInit()
        {
            IoCManager.Resolve<IComponentFactory>().DoAutoRegistrations();
        }

        public override void Init()
        {
            RuntimeOpenDreamIoC.Register();
            IoCManager.BuildGraph();
        }

        public override void PostInit()
        {

        }
    }
}
