using System.Diagnostics;
using System.IO.Compression;

namespace OpenDreamPackaging;

public static class ServerPackaging {
    public static readonly PlatformReg[] Platforms = {
        new("win-x64", "Windows", true),
        new("linux-x64", "Linux", true),
        new("linux-arm64", "Linux", true),
        new("osx-x64", "MacOS", true),

        // Non-default platforms (i.e. for Watchdog Git)
        new("win-x86", "Windows", false),
        new("linux-x86", "Linux", false),
        new("linux-arm", "Linux", false),
    };

    private static readonly string[] ServerIgnoredResources = {
        "Textures",
        "Fonts",
        "Audio",
        "Shaders"
    };

    // Assembly names to copy from content.
    // PDBs are included if available, .dll/.pdb appended automatically.
    private static readonly string[] ServerContentAssemblies = {
        "OpenDreamServer",
        "OpenDreamShared",
        "OpenDreamRuntime",
        "Byond.TopicSender",
        "DMCompiler"
    };

    // Extra assemblies to copy on the server, with a startswith
    private static readonly string[] ServerExtraAssemblies = {
        "OpenDreamServer",
        "OpenDreamShared",
        "OpenDreamRuntime",
        "Byond.TopicSender",
        "DMCompiler"
    };

    private static readonly string[] ServerNotExtraAssemblies = {
        "Microsoft.CodeAnalysis"
    };

    private static readonly string[] BinSkipFolders = {
        // Roslyn localization files, screw em.
        "cs",
        "de",
        "es",
        "fr",
        "it",
        "ja",
        "ko",
        "pl",
        "pt-BR",
        "ru",
        "tr",
        "zh-Hans",
        "zh-Hant"
    };

    public static IEnumerable<string> PlatformRIds => Platforms.Select(platform => platform.RId);

    public static IEnumerable<PlatformReg> PlatformsDefault => Platforms.Where(platform => platform.BuildByDefault);

    public static void Package(Program.ServerOptions options) {
        IEnumerable<PlatformReg> platforms = (options.Platform != null) ? new[] {options.Platform} : PlatformsDefault;

        if (!options.InPlatformSubDir && options.Platform == null) {
            Console.Error.WriteLine(
                "Packaging the server without a platform subdirectory requires selecting a single platform");
        }

        if (Directory.Exists(options.OutputDir)) {
            Console.WriteLine($"Cleaning old release packages ({options.OutputDir})...");
            Directory.Delete(options.OutputDir, true);
        }

        Directory.CreateDirectory(options.OutputDir);

        if (options.HybridAcz) {
            // Hybrid ACZ involves a file "Content.Client.zip" in the server executable directory.
            // Rather than hosting the client ZIP on the watchdog or on a separate server,
            // Hybrid ACZ uses the ACZ hosting functionality to host it as part of the status host,
            // which means that features such as automatic UPnP forwarding still work properly.
            ClientPackaging.Package(new Program.ClientOptions {
                OutputDir = options.OutputDir,
                SkipBuild = options.SkipBuild
            });
        }

        foreach (var platform in platforms) {
            BuildPlatform(platform, options);
        }
    }

    private static void BuildPlatform(PlatformReg platform, Program.ServerOptions options) {
        Console.WriteLine($"Building project for {platform.RId}");

        if (!options.SkipBuild) {
            Program.RunSubProcess(new ProcessStartInfo {
                FileName = "dotnet",
                ArgumentList = {
                    "build",
                    "OpenDreamServer/OpenDreamServer.csproj",
                    "-c", "Release",
                    "--nologo",
                    "/v:m",
                    $"/p:TargetOS={platform.TargetOs}",
                    "/t:Rebuild",
                    "/p:FullRelease=True",
                    "/m",
                    $"/p:TgsEngineBuild={(options.TgsEngineBuild ? "True" : "False")}"
                }
            });

            PublishClientServer(platform.RId, platform.TargetOs);
        }

        string releaseDir = options.OutputDir;
        if (options.InPlatformSubDir)
            releaseDir = Path.Combine(releaseDir, $"OpenDream.Server_{platform.RId}");

        Console.WriteLine($"Packaging {platform.RId} server...");
        Directory.CreateDirectory(releaseDir);
        Program.CopyDirectory($"RobustToolbox/bin/Server/{platform.RId}/publish", releaseDir, BinSkipFolders);
        CopyResources(Path.Combine(releaseDir, "Resources"));
        CopyContentAssemblies(Path.Combine(releaseDir, "Resources", "Assemblies"));
        if (options.HybridAcz) {
            // Hybrid ACZ expects "Content.Client.zip" (as it's not OpenDream-specific)
            ZipFile.CreateFromDirectory(Path.Combine(options.OutputDir, "OpenDream.Client"), Path.Combine(releaseDir, "Content.Client.Zip"));
        }
    }

    private static void PublishClientServer(string platformRId, string targetOs) {
        Program.RunSubProcess(new ProcessStartInfo {
            FileName = "dotnet",
            ArgumentList = {
                "publish",
                "RobustToolbox/Robust.Server/Robust.Server.csproj",
                "--runtime", platformRId,
                "--no-self-contained",
                "-c", "Release",
                $"/p:TargetOS={targetOs}",
                "/p:FullRelease=True",
                "/m"
            }
        });
    }

    private static void CopyResources(string dest) {
        // Content repo goes FIRST so that it won't override engine files as that's forbidden.
        var ignoreSet = Program.SharedIgnoredResources.Union(ServerIgnoredResources).ToArray();

        Program.CopyDirectory("Resources", dest, ignoreSet);
        Program.CopyDirectory("RobustToolbox/Resources", dest, ignoreSet);
    }

    private static void CopyContentAssemblies(string dest) {
        List<string> files = new();
        string sourceDir = Path.Combine("bin", "Content.Server");
        string[] baseAssemblies = ServerContentAssemblies;

        // Additional assemblies that need to be copied such as EFCore.
        foreach (var filename in Directory.EnumerateFiles(sourceDir)) {
            if (ServerExtraAssemblies.Any(assembly => filename.StartsWith(assembly)) &&
                !ServerNotExtraAssemblies.Any(assembly => filename.StartsWith(assembly)))
                files.Add(filename);
        }

        // Include content assemblies.
        foreach (var assembly in baseAssemblies) {
            files.Add(assembly + ".dll");

            // If PDB available, include it as well.
            var pdbPath = assembly + ".pdb";
            if (File.Exists(Path.Combine(sourceDir, pdbPath)))
                files.Add(pdbPath);
        }

        // Create assemblies dir if necessary.
        Directory.CreateDirectory(dest);

        foreach (var file in files) {
            File.Copy(Path.Combine(sourceDir, file), Path.Combine(dest, file));
        }
    }
}
