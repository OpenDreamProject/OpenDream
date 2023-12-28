using System.Threading;
using System.Threading.Tasks;
using OpenDreamPackaging;
using Robust.Packaging;
using Robust.Packaging.AssetProcessing;
using Robust.Server.ServerStatus;

namespace OpenDreamRuntime;

public sealed class DreamMagicAczProvider : IMagicAczProvider {
    private readonly IDependencyCollection _dependencies;
    private readonly string _rootPath;
    private readonly string[] _resources;

    public DreamMagicAczProvider(IDependencyCollection dependencies, string rootPath, string[] resources) {
        _dependencies = dependencies;
        _rootPath = rootPath;
        _resources = resources;
    }

    public async Task Package(AssetPass pass, IPackageLogger logger, CancellationToken cancel) {
        var contentDir = DefaultMagicAczProvider.FindContentRootPath(_dependencies);

        await DreamPackaging.WriteResources(contentDir, _rootPath, _resources, pass, logger, cancel);
    }
}
