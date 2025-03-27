using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenDreamPackaging;
using Robust.Packaging;
using Robust.Packaging.AssetProcessing;
using Robust.Server.ServerStatus;

namespace OpenDreamRuntime;

public sealed class DreamAczProvider(IDependencyCollection dependencies, string rootPath, string[] resources) : IMagicAczProvider, IFullHybridAczProvider {
    /// <summary>
    /// Resources created at runtime rather than coming from disk
    /// </summary>
    private readonly Dictionary<int, byte[]> _extraResources = new();

    public void AddResource(int resourceId, byte[] data) {
        _extraResources.Add(resourceId, data);
    }

    public async Task Package(AssetPass pass, IPackageLogger logger, CancellationToken cancel) {
        var contentDir = DefaultMagicAczProvider.FindContentRootPath(dependencies);

        await DreamPackaging.WriteResources(contentDir, rootPath, resources, pass, logger, cancel);
    }

    public Task Package(AssetPass hybridPackageInput, AssetPass output, IPackageLogger logger, CancellationToken cancel) {
        var clientAssetGraph = new RobustClientAssetGraph();
        var resourceInput = clientAssetGraph.Input;
        output.AddDependency(clientAssetGraph.Output);
        output.AddDependency(hybridPackageInput);

        AssetGraph.CalculateGraph(
            clientAssetGraph.AllPasses.Concat(new[] { hybridPackageInput, output }).ToArray(),
            logger);

        DreamPackaging.WriteRscResources(rootPath, resources, _extraResources, resourceInput);
        resourceInput.InjectFinished();

        return Task.CompletedTask;
    }
}
