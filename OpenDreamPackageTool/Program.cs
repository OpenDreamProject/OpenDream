using System.Diagnostics.CodeAnalysis;

namespace OpenDreamPackageTool;

public static class Program {
    public class Options {
        public string OutputDir = "release/";
        public bool SkipBuild;
        public string BuildConfiguration = "Release";
    }

    public class ServerOptions : Options {
        public string? Platform;
        public bool HybridAcz;
        public bool InPlatformSubDir = true;
        public bool TgsEngineBuild;
    }

    public class ClientOptions : Options;

    public class TgsOptions : Options {
        // Avoid adding arguments for TGS, to give us more flexibility while keeping compatibility
    }

    public static readonly string[] SharedIgnoredResources = [
        ".gitignore",
        ".directory",
        ".DS_Store"
    ];

    private static readonly IReadOnlySet<string> ValidBuildConfigurations = new HashSet<string> {
        "Release",
        "Debug",
        "Tools"
    };

    public static int Main(string[] args) {
        if (!TryParseArgs(args, out var options))
            return 1;

        if (!File.Exists("OpenDream.sln")) {
            Console.Error.WriteLine(
                "You must run this tool from the root of the OpenDream repo. OpenDream.sln was not found.");
            return 1;
        }

        switch (options) {
            case ServerOptions serverOptions:
                ServerPackaging.Package(serverOptions);
                break;
            case ClientOptions clientOptions:
                ClientPackaging.Package(clientOptions);
                break;
            case TgsOptions tgsOptions:
                TgsPackaging.Package(tgsOptions);
                break;
        }

        return 0;
    }

    public static void CopyDirectory(string src, string dest, string[]? skip = null) {
        skip ??= [];

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
                    case "--configuration":
                        if (i + 1 >= args.Length) {
                            Console.Error.WriteLine("No configuration given");
                            return false;
                        }

                        serverOptions.BuildConfiguration = args[++i];
                        if (!ValidBuildConfigurations.Contains(serverOptions.BuildConfiguration)) {
                            Console.Error.WriteLine($"Invalid configuration '{serverOptions.BuildConfiguration}'");
                            return false;
                        }

                        break;
                    case "--platform":
                    case "-p":
                        if (i + 1 >= args.Length) {
                            Console.Error.WriteLine("No platform given");
                            return false;
                        }

                        serverOptions.Platform = args[++i];
                        break;
                    case "--hybrid-acz":
                        serverOptions.HybridAcz = true;
                        break;
                    default:
                        Console.Error.WriteLine($"Invalid argument '{arg}'");
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
                    case "--configuration":
                        if (i + 1 >= args.Length) {
                            Console.Error.WriteLine("No configuration given");
                            return false;
                        }

                        clientOptions.BuildConfiguration = args[++i];
                        if (!ValidBuildConfigurations.Contains(clientOptions.BuildConfiguration)) {
                            Console.Error.WriteLine($"Invalid configuration '{clientOptions.BuildConfiguration}'");
                            return false;
                        }

                        break;
                    default:
                        Console.Error.WriteLine($"Invalid argument '{arg}'");
                        return false;
                }
            }

            return true;
        } else if (args.Contains("--tgs")) {
            var tgsOptions = new TgsOptions();

            options = tgsOptions;
            for (int i = 0; i < args.Length; i++) {
                var arg = args[i];

                switch (arg) {
                    case "--tgs":
                        break;
                    case "--output":
                    case "-o":
                        tgsOptions.OutputDir = args[++i];
                        break;
                    case "--skip-build":
                        tgsOptions.SkipBuild = true;
                        break;
                    case "--configuration":
                        if (i + 1 >= args.Length) {
                            Console.Error.WriteLine("No configuration given");
                            return false;
                        }

                        tgsOptions.BuildConfiguration = args[++i];
                        if (!ValidBuildConfigurations.Contains(tgsOptions.BuildConfiguration)) {
                            Console.Error.WriteLine($"Invalid configuration '{tgsOptions.BuildConfiguration}'");
                            return false;
                        }

                        break;
                    case "--tools":
                        tgsOptions.BuildConfiguration = "Debug";
                        break;
                    default:
                        Console.Error.WriteLine($"Invalid argument '{arg}'");
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
