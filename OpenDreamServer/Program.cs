using Robust.Server;

namespace OpenDreamServer {
    class Program {
        static void Main(string[] args) {
            ContentStart.StartLibrary(args, new ServerOptions
            {
                ContentModulePrefix = "OpenDream",
            });
        }
    }
}
