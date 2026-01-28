using Robust.Client;

namespace OpenDreamClient;

internal static class Program {
    [STAThread]
    public static void Main(string[] args) {
        ContentStart.StartLibrary(args, new GameControllerOptions {
            Sandboxing = true
        });
    }
}
