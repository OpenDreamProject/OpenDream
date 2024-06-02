using Robust.Client;

namespace OpenDreamClient;

internal static class Program {
    public static void Main(string[] args) {
        ContentStart.StartLibrary(args, new GameControllerOptions {
            Sandboxing = true
        });




        var test = "abc";


        var test2 = "def";
    }
}
