using Robust.Client;
using Robust.Shared.Utility;

namespace Content.Client {
    internal static class Program {
        public static void Main(string[] args) {
            ContentStart.StartLibrary(args, new GameControllerOptions() {
                //Content.Shared makes use of lots of things that client sandboxing is not happy with
                Sandboxing = false,
                DefaultWindowTitle = "OpenDream",
                WindowIconSet = new ResourcePath("/OpenDream/Logo/Icon"),
                SplashLogo = new ResourcePath("/OpenDream/Logo/logo.png"),
            });
        }
    }
}
