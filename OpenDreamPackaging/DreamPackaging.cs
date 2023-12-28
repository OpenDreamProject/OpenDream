using Robust.Packaging;
using Robust.Packaging.AssetProcessing;

namespace OpenDreamPackaging;

public static class DreamPackaging {
    public static async Task WriteResources(
        string contentDir,
        string dreamRootDir,
        string[] resources,
        AssetPass pass,
        IPackageLogger logger,
        CancellationToken cancel)
    {
        var graph = new RobustClientAssetGraph();
        pass.Dependencies.Add(new AssetPassDependency(graph.Output.Name));

        AssetGraph.CalculateGraph(graph.AllPasses.Append(pass).ToArray(), logger);

        var inputPass = graph.Input;

        await RobustClientPackaging.WriteClientResources(
            contentDir,
            inputPass,
            cancel);

        await RobustClientPackaging.WriteClientResources(contentDir, inputPass, cancel);

        for (var i = 0; i < resources.Length; i++) {
            var resource = resources[i].Replace('\\', Path.DirectorySeparatorChar);
            // The game client only knows a resource ID, so that's what we name the files.
            // (0 is special and taken, so we start counting at 1)
            var path = $"Rsc/{i + 1}";
            var diskPath = Path.Combine(dreamRootDir, resource);

            inputPass.InjectFileFromDisk(path, diskPath);
        }

        inputPass.InjectFinished();
    }
}
