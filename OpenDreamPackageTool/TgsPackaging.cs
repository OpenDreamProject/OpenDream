using System.Diagnostics;
using Robust.Packaging.Utility;

namespace OpenDreamPackageTool;

public static class TgsPackaging {
    public static void Package(Program.TgsOptions options) {
        if (Directory.Exists(options.OutputDir)) {
            Console.WriteLine($"Cleaning old release packages ({options.OutputDir})...");
            Directory.Delete(options.OutputDir, true);
        }

        // Package the server to <output dir>/bin/server
        ServerPackaging.Package(new Program.ServerOptions {
            OutputDir = Path.Combine(options.OutputDir, "bin", "server"),
            Platform = options.Platform,
            HybridAcz = true, // Force Hybrid ACZ with TGS
            SkipBuild = options.SkipBuild,
            InPlatformSubDir = false,
            TgsEngineBuild = true
        });

        var platform = options.Platform!;
        if (!options.SkipBuild) {
            ProcessHelpers.RunCheck(new ProcessStartInfo {
                FileName = "dotnet",
                ArgumentList = {
                    "build",
                    "DMCompiler/DMCompiler.csproj",
                    "-c", "Release",
                    "--nologo",
                    "/v:m",
                    $"/p:TargetOS={platform.TargetOs}",
                    "/t:Rebuild",
                    "/m"
                }
            }).Wait();

            PublishCompiler(platform.RId, platform.TargetOs);
        }

        // Package the compiler to <output dir>/bin/compiler
        Program.CopyDirectory($"bin/DMCompiler/{platform.RId}/publish", Path.Combine(options.OutputDir, "bin", "compiler"));
    }

    private static void PublishCompiler(string platformRId, string targetOs) {
        ProcessHelpers.RunCheck(new ProcessStartInfo {
            FileName = "dotnet",
            ArgumentList = {
                "publish",
                "DMCompiler/DMCompiler.csproj",
                "--runtime", platformRId,
                "--no-self-contained",
                "-c", "Release",
                $"/p:TargetOS={targetOs}",
                "/m"
            }
        }).Wait();
    }
}
