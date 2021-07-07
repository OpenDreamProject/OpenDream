using Robust.Client;
using Robust.Shared.IoC;

namespace OpenDreamClient {
    public static class Program
    {
        // TODO Robust: Make all of these usages use IoCManager or dependencies.
        internal static OpenDream OpenDream => IoCManager.Resolve<OpenDream>();

        static void Main(string[] args)
        {
            // TODO ROBUST: This is temporary, it should use ContentStart.Start() in the future.
            ContentStart.StartLibrary(args, new GameControllerOptions()
            {
                Sandboxing = false,
                UserDataDirectoryName = "OpenDream",
                ContentModulePrefix = "OpenDream",
                ContentBuildDirectory = "OpenDreamClient",
            });
        }
    }
}
