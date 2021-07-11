using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Resources;
using OpenDreamShared;
using Robust.Shared.IoC;

namespace OpenDreamRuntime
{
    public static class RuntimeOpenDreamIoC
    {
        public static void Register()
        {
            SharedOpenDreamIoC.Register();
            IoCManager.Register<DreamRuntime>();
            IoCManager.Register<DreamObjectTree>();
            IoCManager.Register<DreamStateManager>();
            IoCManager.Register<DreamResourceManager>();
        }
    }
}
