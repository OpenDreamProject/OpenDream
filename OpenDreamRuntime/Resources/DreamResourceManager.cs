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
    public sealed class DreamResourceManager
    {
        [Dependency] private readonly IServerNetManager _netManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IDreamManager _dreamMan = default!;

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

        public bool SufficientTrustLevel(string path, bool dllCheck = false)
        {
            // TODO safe/ultrasafe should prompt the user to allow/deny, then update these throws
            switch (_dreamMan.TrustLevel)
            {
                case TrustLevel.Trusted: return true;
                case TrustLevel.Ultrasafe:
                {
                    if(dllCheck) throw new PropagatingRuntime("Safety violation: Can't call DLLs outside of Trusted");
                    throw new PropagatingRuntime("Safety violation: Can't access files");
                }
                case TrustLevel.Safe:
                {
                    if (dllCheck)
                    {
                        throw new PropagatingRuntime("Safety violation: Can't call DLLs outside of Trusted");
                    }
                    var relPath = Path.GetRelativePath(RootPath, path);
                    if (!relPath.StartsWith('.') && !Path.IsPathRooted(relPath))
                    {
                        return true;
                    }
                    throw new PropagatingRuntime("Safety violation");
                }
                default:
                    throw new PropagatingRuntime("Invalid trust level");
            }
        }

        public bool DoesFileExist(string resourcePath)
        {
            var path = Path.Combine(RootPath, resourcePath);
            if (!SufficientTrustLevel(path)) return false;
            return File.Exists(path);
        }

        public DreamResource LoadResource(string resourcePath, bool ignoreTrustlevel = false) {
            if (resourcePath == "") return new ConsoleOutputResource(); //An empty resource path is the console

            if (!_resourceCache.TryGetValue(resourcePath, out DreamResource resource))
            {
                var path = Path.Combine(RootPath, resourcePath);
                if (!ignoreTrustlevel && !SufficientTrustLevel(path))
                {
                    return null;
                }
                resource = new DreamResource(path, resourcePath);
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

        public bool DeleteFile(string filePath)
        {
            var path = Path.Combine(RootPath, filePath);
            if (!SufficientTrustLevel(path))
            {
                return false;
            }
            try {
                File.Delete(path);
            } catch (Exception) {
                return false;
            }

            return true;
        }

        public bool DeleteDirectory(string directoryPath)
        {
            var path = Path.Combine(RootPath, directoryPath);
            if (!SufficientTrustLevel(path))
            {
                return false;
            }
            try {
                Directory.Delete(path, true);
            } catch (Exception) {
                return false;
            }

            return true;
        }

        public bool SaveTextToFile(string filePath, string text)
        {
            var path = Path.Combine(RootPath, filePath);
            if (!SufficientTrustLevel(path))
            {
                return false;
            }
            try {
                File.WriteAllText(path, text);
            } catch (Exception) {
                return false;
            }

            return true;
        }

        public bool CopyFile(string sourceFilePath, string destinationFilePath)
        {
            var src = Path.Combine(RootPath, sourceFilePath);
            var dest = Path.Combine(RootPath, destinationFilePath);
            if (!SufficientTrustLevel(src) || !SufficientTrustLevel(dest))
            {
                return false;
            }
            try {
                File.Copy(src, dest);
            } catch (Exception) {
                return false;
            }

            return true;
        }

        public string[] EnumerateListing(string path)
        {
            string directory = Path.Combine(RootPath, Path.GetDirectoryName(path));
            if (!SufficientTrustLevel(directory))
            {
                return Array.Empty<string>();
            }
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
