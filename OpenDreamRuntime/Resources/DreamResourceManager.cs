using OpenDreamShared.Net.Packets;
using System;
using System.Collections.Generic;
using System.IO;

namespace OpenDreamRuntime.Resources {
    public class DreamResourceManager {
        public string RootPath;

        private Dictionary<string, DreamResource> _resourceCache = new();
        private object _resourceCacheLock = new object();

        public DreamResourceManager(string rootPath) {
            RootPath = rootPath;
        }

        public bool DoesFileExist(string resourcePath) {
            return File.Exists(Path.Combine(RootPath, resourcePath));
        }

        public DreamResource LoadResource(string resourcePath) {
            if (resourcePath == "") return new ConsoleOutputResource(); //An empty resource path is the console

            DreamResource resource = null;

            lock (_resourceCacheLock) {
                _resourceCache.TryGetValue(resourcePath, out resource);
            }

            if (resource == null) {
                resource = new DreamResource(Path.Combine(RootPath, resourcePath), resourcePath);
                lock (_resourceCacheLock) {
                    _resourceCache.Add(resourcePath, resource);
                }
            }

            return resource;
        }

        public void HandleRequestResourcePacket(DreamConnection connection, PacketRequestResource pRequestResource) {
            DreamResource resource = LoadResource(pRequestResource.ResourcePath);

            if (resource.ResourceData != null) {
                connection.SendPacket(new PacketResource(resource.ResourcePath, resource.ResourceData));
            } else {
                Console.WriteLine("User \"" + connection.CKey + "\" requested resource '" + pRequestResource.ResourcePath + "', which doesn't exist");
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

        public string[] GetListing(string path) {
            string[] files;

            if (Path.EndsInDirectorySeparator(path)) {
                files = Directory.GetFiles(RootPath, path, SearchOption.AllDirectories);
            } else {
                string directoryPath = Path.GetDirectoryName(path);

                files = Directory.GetFiles(Path.Combine(RootPath, directoryPath ?? string.Empty), Path.GetFileName(path), SearchOption.AllDirectories);
            }

            return files;
        }
    }
}
