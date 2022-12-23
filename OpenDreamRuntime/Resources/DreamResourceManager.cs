using System.Diagnostics.CodeAnalysis;
using System.IO;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.MetaObjects;
using OpenDreamShared.Network.Messages;
using OpenDreamShared.Resources;
using Robust.Shared.Network;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace OpenDreamRuntime.Resources {
    public sealed class DreamResourceManager {
        [Dependency] private readonly IDreamObjectTree _objectTree = default!;
        [Dependency] private readonly IServerNetManager _netManager = default!;

        public string RootPath { get; private set; }

        private readonly List<DreamResource> _resourceCache = new();
        private readonly Dictionary<string, int> _resourcePathToId = new();
        private readonly Dictionary<DreamResource, Image<Rgba32>> _imageCache = new();

        private ISawmill _sawmill;

        public void Initialize() {
            _sawmill = Logger.GetSawmill("opendream.res");
            _netManager.RegisterNetMessage<MsgRequestResource>(RxRequestResource);
            _netManager.RegisterNetMessage<MsgResource>();

            _resourceCache.Clear();
            _resourcePathToId.Clear();
            _imageCache.Clear();

            // An empty resource path is the console
            _resourceCache.Add(new ConsoleOutputResource());
            _resourcePathToId.Add(String.Empty, 0);
        }

        public void SetDirectory(string directory) {
            RootPath = directory;
            // Used to ensure external DLL calls see a consistent current directory.
            Directory.SetCurrentDirectory(RootPath);

            _sawmill.Debug($"Resource root path set to {RootPath}");
        }

        public bool DoesFileExist(string resourcePath) {
            return File.Exists(Path.Combine(RootPath, resourcePath));
        }

        public DreamResource LoadResource(string resourcePath) {
            DreamResource resource;

            if (_resourcePathToId.TryGetValue(resourcePath, out int resourceId)) {
                resource = _resourceCache[resourceId];
            } else {
                var filePath = Path.Combine(RootPath, resourcePath);
                resourceId = _resourceCache.Count;

                // Create a new type of resource based on its extension
                switch (Path.GetExtension(resourcePath)) {
                    case ".dmi":
                        resource = new IconResource(resourceId, filePath, resourcePath);
                        break;
                    case ".png":
                    case ".jpg":
                    case ".rsi": // RT-specific, not in BYOND
                    case ".gif":
                    case ".bmp":
                        // TODO implement other icon file types
                        goto default;

                    default:
                        resource = new DreamResource(resourceId, filePath, resourcePath);
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

        public Image<Rgba32> LoadImage(DreamResource resource) {
            if (_imageCache.TryGetValue(resource, out var image))
                return image;

            image = Image.Load<Rgba32>(resource.ResourceData);
            _imageCache.Add(resource, image);
            return image;
        }

        public bool TryLoadIcon(DreamValue value, [NotNullWhen(true)] out IconResource? icon) {
            if (value.TryGetValueAsDreamObjectOfType(_objectTree.Icon, out var iconObj)) {
                DreamIcon dreamIcon = DreamMetaObjectIcon.ObjectToDreamIcon[iconObj];

                icon = dreamIcon.GenerateDMI();
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
        public IconResource CreateIconResource(byte[] data, Image<Rgba32> texture, DMIParser.ParsedDMIDescription dmi) {
            int resourceId = _resourceCache.Count;
            IconResource resource = new IconResource(resourceId, data, texture, dmi);

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
                File.Delete(Path.Combine(RootPath, filePath));
            } catch (Exception) {
                return false;
            }

            return true;
        }

        public bool DeleteDirectory(string directoryPath) {
            try {
                Directory.Delete(Path.Combine(RootPath, directoryPath), true);
            } catch (Exception) {
                return false;
            }

            return true;
        }

        public bool SaveTextToFile(string filePath, string text) {
            try {
                File.WriteAllText(Path.Combine(RootPath, filePath), text);
            } catch (Exception) {
                return false;
            }

            return true;
        }

        public bool CopyFile(string sourceFilePath, string destinationFilePath) {
            try {
                var dest = Path.Combine(RootPath, destinationFilePath);
                Directory.CreateDirectory(Path.GetDirectoryName(dest));
                File.Copy(Path.Combine(RootPath, sourceFilePath), dest);
            } catch (Exception) {
                return false;
            }

            return true;
        }

        public string[] EnumerateListing(string path) {
            string directory = Path.Combine(RootPath, Path.GetDirectoryName(path));
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
