using OpenDreamRuntime;
using System;
using System.IO;
using System.Net;

namespace OpenDreamServer {
    class Program {
        static void Main(string[] args) {
            if (args.Length < 1 || Path.GetExtension(args[0]) != ".json") {
                Console.WriteLine("You must compile your game using DMCompiler, and supply its output as an argument");

                return;
            }

            var server = new Server(IPAddress.Any.ToString(), 25566);
            var runtime = new DreamRuntime(server, args[0]);
            runtime.Run();
        }
    }
}
