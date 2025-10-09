using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using OpenDreamRuntime.Objects.Types;
using OpenDreamShared.Network.Messages;
using OpenDreamShared.Resources;
using Robust.Server.ServerStatus;
using Robust.Shared.Network;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace OpenDreamRuntime.Resources;

public sealed class DreamResourceManager {
    [Dependency] private readonly IServerNetManager _netManager = default!;
    [Dependency] private readonly IStatusHost _statusHost = default!;
    [Dependency] private readonly IDependencyCollection _dependencyCollection = default!;
    [Dependency] private readonly ISerializationManager _serializationManager = default!;
    public string RootPath { get; private set; } = default!;
    public DreamResource? InterfaceFile { get; private set; }

    private DreamAczProvider _aczProvider = default!;
    private readonly List<DreamResource> _resourceCache = new();
    private readonly Dictionary<string, int> _resourcePathToId = new();
    private readonly Dictionary<string, IconResource> _md5ToGeneratedIcon = new();

    private ISawmill _sawmill = default!;

    public void PreInitialize() {
        _sawmill = Logger.GetSawmill("opendream.res");
        _netManager.RegisterNetMessage<MsgRequestResource>(RxRequestResource);
        _netManager.RegisterNetMessage<MsgResource>();
        _netManager.RegisterNetMessage<MsgNotifyResourceUpdate>();
    }

    public void Initialize(string rootPath, string[] resources, string? interfaceFile) {
        _resourceCache.Clear();
        _resourcePathToId.Clear();

        // An empty resource path is the console
        _resourceCache.Add(new ConsoleOutputResource());
        _resourcePathToId.Add(string.Empty, 0);

        RootPath = rootPath;

        // Used to ensure external DLL calls see a consistent current directory.
        Directory.SetCurrentDirectory(RootPath);

        _sawmill.Debug($"Resource root path set to {RootPath}");

        // Immediately build list of resources from rsc.
        for (var i = 0; i < resources.Length; i++) {
            var resource = resources[i];
            var loaded = LoadResource(resource);
            // Resource IDs must be consistent with the ordering, or else packaged resources will mismatch.
            // First resource is the hardcoded console resource
            DebugTools.Assert(loaded.Id == i + 1, "Resource IDs not consistent!");
        }

        _aczProvider = new DreamAczProvider(_dependencyCollection, rootPath, resources);
        _statusHost.SetMagicAczProvider(_aczProvider);
        _statusHost.SetFullHybridAczProvider(_aczProvider);

        if (!string.IsNullOrWhiteSpace(interfaceFile)) {
            if (DoesFileExist(interfaceFile))
                InterfaceFile = LoadResource(interfaceFile);
            else
                throw new FileNotFoundException("Interface DMF not found at " + Path.Join(rootPath, interfaceFile));
        }
    }

    public bool DoesFileExist(string resourcePath) {
        return File.Exists(resourcePath);
    }

    public DreamResource LoadResource(string resourcePath) {
        DreamResource resource;
        int resourceId;

        DreamResource GetResource() {
            // Create a new type of resource based on its extension
            switch (Path.GetExtension(resourcePath)) {
                case ".dmf":
                    resource = new DMFResource(resourceId, resourcePath, resourcePath, _serializationManager);
                    break;
                case ".dmi":
                case ".png":
                    resource = new IconResource(resourceId, resourcePath, resourcePath);
                    break;
                case ".jpg":
                case ".rsi": // RT-specific, not in BYOND
                case ".gif":
                case ".bmp":
                    // TODO implement other icon file types
                    goto default;

                default:
                    resource = new DreamResource(resourceId, resourcePath, resourcePath);
                    break;
            }

            return resource;
        }

        if (_resourcePathToId.TryGetValue(resourcePath, out resourceId)) {
            resource = _resourceCache[resourceId];
        } else {
            resourceId = _resourceCache.Count;
            resource = GetResource();
            _resourceCache.Add(resource);
            _resourcePathToId.Add(resourcePath, resourceId);
        }

        return resource;
    }

    public bool TryLoadResource(int resourceId, [NotNullWhen(true)] out DreamResource? resource) {
        if (resourceId >= 0 && resourceId < _resourceCache.Count) {
            resource = _resourceCache[resourceId];
            return true;
        }

        resource = null;
        return false;
    }

    public bool TryLoadIcon(DreamValue value, [NotNullWhen(true)] out IconResource? icon) {
        if (value.TryGetValueAsDreamObject<DreamObjectIcon>(out var iconObj)) {
            icon = iconObj.Icon.GenerateDMI();
            return true;
        }

        DreamResource? resource;

        if (value.TryGetValueAsString(out var resourcePath)) {
            resource = LoadResource(resourcePath);
        } else {
            value.TryGetValueAsDreamResource(out resource);
        }

        if (resource is IconResource iconResource) {
            icon = iconResource;
            return true;
        }

        icon = null;
        return false;
    }

    /// <summary>
    /// Dynamically create a new generic resource that clients can use
    /// </summary>
    /// <param name="data">The resource's data</param>
    public DreamResource CreateResource(byte[] data) {
        int resourceId = _resourceCache.Count;
        DreamResource resource = new DreamResource(resourceId, data);

        _resourceCache.Add(resource);
        _aczProvider.AddResource(resourceId, data);
        return resource;
    }

    /// <summary>
    /// Dynamically create a new icon resource that clients can use
    /// </summary>
    /// <param name="data">The resource's data</param>
    /// <param name="texture">The image texture</param>
    /// <param name="dmi">The image's DMI information, assumed to be equal to what's in the data argument</param>
    public IconResource CreateIconResource(byte[] data, Image<Rgba32> texture, DMIParser.ParsedDMIDescription dmi) {
        var resourceId = _resourceCache.Count;
        var md5 = CalculateMd5(data);
        if (_md5ToGeneratedIcon.TryGetValue(md5, out var possibleDuplicate) && possibleDuplicate.ResourceData != null) {
            if (data.SequenceEqual(possibleDuplicate.ResourceData))
                return possibleDuplicate;
        }

        IconResource resource = new IconResource(resourceId, data, texture, dmi);
        _resourceCache.Add(resource);
        _md5ToGeneratedIcon[md5] = resource; // Would override in the case of collisions, but whatever
        _aczProvider.AddResource(resourceId, data);
        return resource;
    }

    /// <summary>
    /// Dynamically create a new icon resource that clients can use
    /// </summary>
    /// <param name="data">The resource's data</param>
    public IconResource CreateIconResource(byte[] data) {
        var resourceId = _resourceCache.Count;
        var md5 = CalculateMd5(data);
        if (_md5ToGeneratedIcon.TryGetValue(md5, out var possibleDuplicate) && possibleDuplicate.ResourceData != null) {
            if (data.SequenceEqual(possibleDuplicate.ResourceData))
                return possibleDuplicate;
        }

        IconResource resource = new IconResource(resourceId, data);
        _resourceCache.Add(resource);
        _md5ToGeneratedIcon[md5] = resource;  // Would override in the case of collisions, but whatever
        _aczProvider.AddResource(resourceId, data);
        return resource;
    }

    public void RxRequestResource(MsgRequestResource pRequestResource) {
        if (TryLoadResource(pRequestResource.ResourceId, out var resource)) {
            var msg = new MsgResource {
                ResourceId = resource.Id, ResourceData = resource.ResourceData
            };

            pRequestResource.MsgChannel.SendMessage(msg);
        } else {
            _sawmill.Warning(
                $"User {pRequestResource.MsgChannel} requested resource with id '{pRequestResource.ResourceId}', which doesn't exist");
        }
    }

    public bool DeleteFile(string filePath) {
        try {
            File.Delete(filePath);
        } catch (Exception) {
            return false;
        }

        return true;
    }

    public bool DeleteDirectory(string directoryPath) {
        try {
            Directory.Delete(directoryPath, true);
        } catch (Exception) {
            return false;
        }

        return true;
    }

    public bool SaveTextToFile(string filePath, string text) {
        try {
            Directory.GetParent(filePath)?.Create();
            File.WriteAllText(filePath, text);
        } catch (Exception) {
            return false;
        }

        return true;
    }

    public bool CopyFile(DreamResource sourceFile, string destinationFilePath) {
        try {
            var dir = Path.GetDirectoryName(destinationFilePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            if (sourceFile.ResourceData == null)
                File.WriteAllText(string.Empty, destinationFilePath);
            else
                File.WriteAllBytes(destinationFilePath, sourceFile.ResourceData);
        } catch (Exception) {
            return false;
        }

        return true;
    }

    public string[] EnumerateListing(string path) {
        string directory = Path.GetDirectoryName(path);
        string searchPattern = Path.GetFileName(path);

        var entries = Directory.GetFileSystemEntries(directory, searchPattern);
        for (var i = 0; i < entries.Length; i++) {
            var relPath = Path.GetRelativePath(directory, entries[i]);
            if (Directory.Exists(entries[i])) relPath += "/";
            entries[i] = relPath;
        }

        return entries;
    }

    private string CalculateMd5(byte[] date) {
        using MD5 md5 = MD5.Create();

        return Encoding.ASCII.GetString(md5.ComputeHash(date));
    }
}
