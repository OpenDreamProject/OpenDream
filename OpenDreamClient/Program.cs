using Robust.Client;
using Robust.Shared.Utility;

namespace OpenDreamClient {
    internal static class Program {
        public static void Main(string[] args) {
            ContentStart.StartLibrary(args, new GameControllerOptions() {
                Sandboxing = true,
                DefaultWindowTitle = "OpenDream",
                WindowIconSet = new ResourcePath("/OpenDream/Logo/Icon"),
                SplashLogo = new ResourcePath("/OpenDream/Logo/logo.png"),
                ContentModulePrefix = "OpenDream",
            });
        }
    }
}
