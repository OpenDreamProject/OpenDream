using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace OpenDreamPackaging;

public static class Program {
    public class Options {
        public string OutputDir = "release/";
        public bool SkipBuild;
    }

    public class ServerOptions : Options {
        public PlatformReg? Platform;
        public bool HybridAcz;
    }

    public class ClientOptions : Options {

    }

    public static readonly string[] SharedIgnoredResources = {
        ".gitignore",
        ".directory",
        ".DS_Store"
    };

    public static int Main(string[] args) {
        if (!TryParseArgs(args, out var options))
            return 1;

        switch (options) {
            case ServerOptions serverOptions:
                ServerPackaging.Package(serverOptions);
                break;
            case ClientOptions clientOptions:
                ClientPackaging.Package(clientOptions);
                break;
        }

        return 0;
    }

    public static void RunSubProcess(ProcessStartInfo startInfo) {
        using Process process = new();

        process.StartInfo = startInfo;
        process.OutputDataReceived += ProcessOutputDataReceived;
        process.Start();
        process.WaitForExit();
        if (process.ExitCode != 0) {
            Environment.Exit(process.ExitCode);
        }
    }

    public static void CopyDirectory(string src, string dest, string[]? skip = null) {
        skip ??= Array.Empty<string>();

        var srcDir = new DirectoryInfo(src);
        if (!srcDir.Exists)
            throw new Exception($"Source directory not found: {src}");

        Directory.CreateDirectory(dest);

        foreach (var file in srcDir.EnumerateFiles()) {
            if (skip.Contains(file.Name))
                continue;

            file.CopyTo(Path.Combine(dest, file.Name));
        }

        foreach (var subDir in srcDir.EnumerateDirectories()) {
            if (skip.Contains(subDir.Name))
                continue;

            CopyDirectory(subDir.FullName, Path.Combine(dest, subDir.Name));
        }
    }

    private static void ProcessOutputDataReceived(object sender, DataReceivedEventArgs args) {
        if (args.Data == null)
            return;

        Console.Write(args.Data);
    }

    private static bool TryParseArgs(string[] args, [NotNullWhen(true)] out Options? options) {
        if (args.Contains("--server")) {
            var serverOptions = new ServerOptions();

            options = serverOptions;
            for (int i = 0; i < args.Length; i++) {
                var arg = args[i];

                switch (arg) {
                    case "--server":
                        break;
                    case "--output":
                    case "-o":
                        serverOptions.OutputDir = args[++i];
                        break;
                    case "--skip-build":
                        serverOptions.SkipBuild = true;
                        break;
                    case "--platform":
                    case "-p":
                        var platformRId = args[++i];

                        serverOptions.Platform =
                            ServerPackaging.Platforms.First(p => p.RId == platformRId);
                        if (serverOptions.Platform == null) {
                            Console.Error.WriteLine($"Invalid platform '{platformRId}'");
                            return false;
                        }

                        break;
                    case "--hybrid-acz":
                        serverOptions.HybridAcz = true;
                        break;
                    default:
                        Console.Error.WriteLine($"Unknown argument '{arg}'");
                        return false;
                }
            }

            return true;
        } else if (args.Contains("--client")) {
            var clientOptions = new ClientOptions();

            options = clientOptions;
            for (int i = 0; i < args.Length; i++) {
                var arg = args[i];

                switch (arg) {
                    case "--client":
                        break;
                    case "--output":
                    case "-o":
                        clientOptions.OutputDir = args[++i];
                        break;
                    case "--skip-build":
                        clientOptions.SkipBuild = true;
                        break;
                    default:
                        Console.Error.WriteLine($"Unknown argument '{arg}'");
                        return false;
                }
            }

            return true;
        }

        options = null;
        Console.Error.WriteLine("One of '--server' or '--client' must be given");
        return false;
    }
}
