using Robust.Client;
using Robust.Shared.IoC;

namespace OpenDreamClient {
    public static class Program
    {
        internal static OpenDream OpenDream => IoCManager.Resolve<OpenDream>();

        static void Main(string[] args)
        {
            ContentStart.Start(args);
        }
    }
}
