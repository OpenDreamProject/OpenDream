using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenDreamPackaging;
using Robust.Packaging;
using Robust.Packaging.AssetProcessing;
using Robust.Server.ServerStatus;

namespace OpenDreamRuntime;

public sealed class DreamAczProvider : IMagicAczProvider, IFullHybridAczProvider {
    private readonly IDependencyCollection _dependencies;
    private readonly string _rootPath;
    private readonly string[] _resources;

    public DreamAczProvider(IDependencyCollection dependencies, string rootPath, string[] resources) {
        _dependencies = dependencies;
        _rootPath = rootPath;
        _resources = resources;
    }

    public async Task Package(AssetPass pass, IPackageLogger logger, CancellationToken cancel) {
        var contentDir = DefaultMagicAczProvider.FindContentRootPath(_dependencies);

        await DreamPackaging.WriteResources(contentDir, _rootPath, _resources, pass, logger, cancel);
    }

    public Task Package(AssetPass hybridPackageInput, AssetPass output, IPackageLogger logger, CancellationToken cancel) {
        var clientAssetGraph = new RobustClientAssetGraph();
        var resourceInput = clientAssetGraph.Input;
        output.AddDependency(clientAssetGraph.Output);
        output.AddDependency(hybridPackageInput);

        AssetGraph.CalculateGraph(
            clientAssetGraph.AllPasses.Concat(new[] { hybridPackageInput, output }).ToArray(),
            logger);

        DreamPackaging.WriteRscResources(_rootPath, _resources, resourceInput);
        resourceInput.InjectFinished();

        return Task.CompletedTask;
    }
}
