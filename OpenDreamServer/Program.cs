using Robust.Server;

namespace OpenDreamServer;

internal static class Program {
    private static void Main(string[] args) {
        ContentStart.StartLibrary(args, new ServerOptions {
            ContentModulePrefix = "OpenDream"
        });
    }
}
