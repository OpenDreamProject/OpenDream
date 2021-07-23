using Robust.Client;

namespace Content.Client {
    internal static class Program {
        public static void Main(string[] args) {
            ContentStart.StartLibrary(args, new GameControllerOptions() {
                //Content.Shared makes use of lots of things that client sandboxing is not happy with
                Sandboxing = false
            });
        }
    }
}
