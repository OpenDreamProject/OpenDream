using Robust.Client;

namespace OpenDreamClient;

internal static class Program {
    public static void Main(string[] args) {
        ContentStart.StartLibrary(args, new GameControllerOptions {
            Sandboxing = true
        });




        Console.WriteLine("abc");


        Console.WriteLine("def");
    }
}
