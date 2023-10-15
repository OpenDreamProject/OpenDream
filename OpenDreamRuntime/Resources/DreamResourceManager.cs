using System.Diagnostics.CodeAnalysis;
using System.IO;
using OpenDreamRuntime.Objects.Types;
using OpenDreamShared.Network.Messages;
using OpenDreamShared.Resources;
using Robust.Shared.Network;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace OpenDreamRuntime.Resources {
    public sealed class DreamResourceManager {
        [Dependency] private readonly IServerNetManager _netManager = default!;

        public string RootPath { get; private set; }

        private readonly List<DreamResource> _resourceCache = new();
        private readonly Dictionary<string, int> _resourcePathToId = new();

        private ISawmill _sawmill;

        public void PreInitialize() {
            _sawmill = Logger.GetSawmill("opendream.res");
            _netManager.RegisterNetMessage<MsgRequestResource>(RxRequestResource);
            _netManager.RegisterNetMessage<MsgResource>();

            _resourceCache.Clear();
            _resourcePathToId.Clear();

            // An empty resource path is the console
            _resourceCache.Add(new ConsoleOutputResource());
            _resourcePathToId.Add(string.Empty, 0);
        }

        public void Initialize(string rootPath, string[] resources) {
            RootPath = rootPath;
            // Used to ensure external DLL calls see a consistent current directory.
            Directory.SetCurrentDirectory(RootPath);

            _sawmill.Debug($"Resource root path set to {RootPath}");

            // Immediately build list of resources from rsc.
            for (var i = 0; i < resources.Length; i++) {
                var resource = resources[i];
                var loaded = LoadResource(resource);
                // Resource IDs must be consistent with the ordering, or else packaged resources will mismatch.
                DebugTools.Assert(loaded.Id == i + 1, "Resource IDs not consistent!");
            }
        }

        public bool DoesFileExist(string resourcePath) {
            return File.Exists(resourcePath);
        }

        public DreamResource LoadResource(string resourcePath) {
            DreamResource resource;

            if (_resourcePathToId.TryGetValue(resourcePath, out int resourceId)) {
                resource = _resourceCache[resourceId];
            } else {
                resourceId = _resourceCache.Count;

                // Create a new type of resource based on its extension
                switch (Path.GetExtension(resourcePath)) {
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
        /// Dynamically create a new icon resource that clients can use
        /// </summary>
        /// <param name="data">The resource's data</param>
        /// <param name="texture">The image texture</param>
        /// <param name="dmi">The image's DMI information</param>
        public IconResource CreateIconResource(byte[] data, Image<Rgba32> texture, DMIParser.ParsedDMIDescription dmi) {
            int resourceId = _resourceCache.Count;
            IconResource resource = new IconResource(resourceId, data, texture, dmi);

            _resourceCache.Add(resource);
            return resource;
        }

        /// <summary>
        /// Dynamically create a new generic resource that clients can use
        /// </summary>
        /// <param name="data">The resource's data</param>
        public DreamResource CreateResource(byte[] data) {
            int resourceId = _resourceCache.Count;
            DreamResource resource = new DreamResource(resourceId, data);

            _resourceCache.Add(resource);
            return resource;
        }

        /// <summary>
        /// Dynamically create a new icon resource that clients can use
        /// </summary>
        /// <param name="data">The resource's data</param>
        public IconResource CreateIconResource(byte[] data) {
            int resourceId = _resourceCache.Count;
            IconResource resource = new IconResource(resourceId, data);

            _resourceCache.Add(resource);
            return resource;
        }

        public void RxRequestResource(MsgRequestResource pRequestResource) {
            if (TryLoadResource(pRequestResource.ResourceId, out var resource)) {
                var msg = new MsgResource() {
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
                if (dir != null)
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
    }
}
