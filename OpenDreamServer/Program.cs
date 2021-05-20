using OpenDreamVM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

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
