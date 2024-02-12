using System.Diagnostics;
using System.Runtime.InteropServices;
using Robust.Packaging.Utility;

namespace OpenDreamPackageTool;

/// <summary>
/// Packages the OpenDream server, client (hybrid ACZ), and compiler in a format friendly for TGS
/// See https://github.com/OpenDreamProject/OpenDream/issues/1495
/// </summary>
public static class TgsPackaging {
    public static void Package(Program.TgsOptions options) {
        if (Directory.Exists(options.OutputDir)) {
            Console.WriteLine($"Cleaning old release packages ({options.OutputDir})...");
            Directory.Delete(options.OutputDir, true);
        }

        var platform = DeterminePlatform();

        // Package the server to <output dir>/bin/server
        ServerPackaging.Package(new Program.ServerOptions {
            OutputDir = Path.Combine(options.OutputDir, "bin", "server"),
            Platform = platform.RId,
            HybridAcz = true, // Force Hybrid ACZ with TGS
            SkipBuild = options.SkipBuild,
            InPlatformSubDir = false,
            TgsEngineBuild = true
        });

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
        // TODO: Add a --compiler option to the package tool
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

    /// <summary>
    /// Determine what platform to package for, based on what OS we're currently running on
    /// </summary>
    /// <returns>The platform</returns>
    private static PlatformReg DeterminePlatform() {
        string rId;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            rId = "win";
        } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
            rId = "linux";
        } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            rId = "osx";
        } else {
            throw new NotSupportedException("Your OS is not supported");
        }

        rId += RuntimeInformation.OSArchitecture switch {
            Architecture.X64 => "-x64",
            Architecture.X86 => "-x86",
            Architecture.Arm64 => "-arm64",
            Architecture.Arm => "-arm",
            _ => throw new NotSupportedException(
                $"Your architecture ({RuntimeInformation.OSArchitecture}) is not supported")
        };

        return ServerPackaging.GetPlatform(rId);
    }
}
