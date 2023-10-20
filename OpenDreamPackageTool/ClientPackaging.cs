using System.Diagnostics;
using Robust.Packaging.Utility;

namespace OpenDreamPackageTool;

public static class ClientPackaging {
    private static readonly string[] ClientIgnoredResources = {
        "Maps",
        "emotes.xml",
        "Groups",
        "engineCommandPerms.yml",
        "clientCommandPerms.yml"
    };

    private static readonly string[] ClientContentAssemblies = {
        "OpenDreamClient",
        "OpenDreamShared"
    };

    public static void Package(Program.ClientOptions options) {
        Directory.CreateDirectory(options.OutputDir);

        if (!options.SkipBuild)
            WipeBin();

        Build(options);
    }

    private static void WipeBin() {
        Console.WriteLine("Clearing old build artifacts (if any)...");

        Directory.Delete("bin", true);
    }

    private static void Build(Program.ClientOptions options) {
        Console.WriteLine("Building project...");

        if (!options.SkipBuild) {
            ProcessHelpers.RunCheck(new ProcessStartInfo {
                FileName = "dotnet",
                ArgumentList = {
                    "build",
                    "OpenDreamClient/OpenDreamClient.csproj",
                    "-c", "Release",
                    "--nologo",
                    "/v:m",
                    "/t:Rebuild",
                    "/p:FullRelease=True",
                    "/m"
                }
            }).Wait();
        }

        DirectoryInfo releaseDir = new DirectoryInfo(Path.Combine(options.OutputDir, "OpenDreamClient"));

        Console.WriteLine("Packaging client...");
        releaseDir.Create();
        CopyResources(releaseDir.FullName);
        CopyContentAssemblies(Path.Combine(releaseDir.FullName, "Assemblies"));
    }

    private static void CopyResources(string dest) {
        var ignoreSet = Program.SharedIgnoredResources.Union(ClientIgnoredResources).ToArray();

        Program.CopyDirectory("Resources", dest, ignoreSet);
    }

    private static void CopyContentAssemblies(string dest) {
        List<string> files = new();
        string sourceDir = Path.Combine("bin", "Content.Client");
        string[] baseAssemblies = ClientContentAssemblies;

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
