using System.IO;
using OpenDreamShared.Network.Messages;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace OpenDreamRuntime.Resources
{
    public sealed class DreamResourceManager
    {
        [Dependency] private readonly IServerNetManager _netManager = default!;

        public string RootPath { get; private set; }

        private readonly Dictionary<string, DreamResource> _resourceCache = new();

        // Terrible and temporary, see DreamManager
        public void Initialize(string jsonPath)
        {
            RootPath = Path.GetDirectoryName(jsonPath);

            Logger.DebugS("opendream.res", $"Resource root path is {RootPath}");

            _netManager.RegisterNetMessage<MsgRequestResource>(RxRequestResource);
            _netManager.RegisterNetMessage<MsgResource>();
        }

        public bool DoesFileExist(string resourcePath) {
            return File.Exists(Path.Combine(RootPath, resourcePath));
        }

        public DreamResource LoadResource(string resourcePath) {
            if (resourcePath == "") return new ConsoleOutputResource(); //An empty resource path is the console

            if (!_resourceCache.TryGetValue(resourcePath, out DreamResource resource)) {
                resource = new DreamResource(Path.Combine(RootPath, resourcePath), resourcePath);
                _resourceCache.Add(resourcePath, resource);
            }

            return resource;
        }

        public void RxRequestResource(MsgRequestResource pRequestResource) {
            DreamResource resource = LoadResource(pRequestResource.ResourcePath);

            if (resource.ResourceData != null)
            {
                var msg = new MsgResource() {
                    ResourcePath = resource.ResourcePath,
                    ResourceData = resource.ResourceData
                };

                pRequestResource.MsgChannel.SendMessage(msg);
            } else {
                Logger.WarningS("opendream.res", $"User {pRequestResource.MsgChannel} requested resource '{pRequestResource.ResourcePath}', which doesn't exist");
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
                File.Copy(Path.Combine(RootPath, sourceFilePath), Path.Combine(RootPath, destinationFilePath));
            } catch (Exception) {
                return false;
            }

            return true;
        }

        public string[] EnumerateListing(string path)
        {
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
