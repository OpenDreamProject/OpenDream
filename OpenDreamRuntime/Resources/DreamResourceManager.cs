using System;
using System.Collections.Generic;
using System.IO;
using OpenDreamShared;
using OpenDreamShared.Network.Messages;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;

namespace OpenDreamRuntime.Resources
{
    public class DreamResourceManager
    {
        [Dependency] private readonly IServerNetManager _netManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        public string RootPath { get; private set; }

        private readonly Dictionary<string, DreamResource> _resourceCache = new();

        public void Initialize()
        {
            var fullPath = Path.GetFullPath(_cfg.GetCVar(OpenDreamCVars.JsonPath));
            RootPath = Path.GetDirectoryName(fullPath);

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
                var msg = _netManager.CreateNetMessage<MsgResource>();
                msg.ResourcePath = resource.ResourcePath;
                msg.ResourceData = resource.ResourceData;
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

        public IEnumerable<string> EnumerateListing(string path) {
            string directory = Path.Combine(RootPath, Path.GetDirectoryName(path));
            string searchPattern = Path.GetFileName(path);

            var entries = Directory.EnumerateFileSystemEntries(directory, searchPattern);
            foreach (string entry in entries) {
                yield return Path.GetRelativePath(directory, entry);
            }
        }
    }
}
