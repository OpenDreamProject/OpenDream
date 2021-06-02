using OpenDreamShared.Net.Packets;
using OpenDreamClient.Resources.ResourceTypes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;

namespace OpenDreamClient.Resources {
    class DreamResourceManager {
        private struct LoadingResourceEntry {
            public Type ResourceType;
            public List<Action<Resource>> LoadCallbacks;

            public LoadingResourceEntry(Type resourceType) {
                ResourceType = resourceType;
                LoadCallbacks = new List<Action<Resource>>();
            }
        }

        private Dictionary<string, LoadingResourceEntry> _loadingResources = new();
        private Dictionary<string, Resource> _resourceCache = new();
        private DirectoryInfo _cacheDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "opendreamcache"));

        public DreamResourceManager(OpenDream openDream) {
            if (_cacheDirectory.Exists) {
                foreach (FileInfo file in _cacheDirectory.EnumerateFiles()) {
                    file.Delete();
                }

                _cacheDirectory.Delete();
            }

            _cacheDirectory.Create();
        }

        public void LoadResourceAsync<T>(string resourcePath, Action<T> onLoadCallback) where T:Resource {
            Resource resource = GetCachedResource(resourcePath);

            if (resource == null) {
                if (!_loadingResources.ContainsKey(resourcePath)) {
                    _loadingResources[resourcePath] = new LoadingResourceEntry(typeof(T));
                    Program.OpenDream.Connection.SendPacket(new PacketRequestResource(resourcePath));
                    Task.Delay(5000).ContinueWith(r => {
                        if (_loadingResources.ContainsKey(resourcePath)) {
                            Console.WriteLine("Resource '" + resourcePath + "' was requested, but is still not recieved 5 seconds later.");
                        }
                    });
                }

                _loadingResources[resourcePath].LoadCallbacks.Add((Resource resource) => {
                    onLoadCallback.Invoke((T)resource);
                });
            } else {
                onLoadCallback.Invoke((T)resource);
            }
        }

        public FileInfo CreateCacheFile(string filename, string data) {
            string cacheFilePath = Path.Combine(_cacheDirectory.FullName, filename);

            File.WriteAllText(cacheFilePath, data);
            return new FileInfo(cacheFilePath);
        }

        public FileInfo CreateCacheFile(string filename, byte[] data) {
            string cacheFilePath = Path.Combine(_cacheDirectory.FullName, filename);

            File.WriteAllBytes(cacheFilePath, data);
            return new FileInfo(cacheFilePath);
        }

        public void HandlePacketResource(PacketResource pResource) {
            if (_loadingResources.ContainsKey(pResource.ResourcePath)) {
                LoadingResourceEntry entry = _loadingResources[pResource.ResourcePath];
                Resource resource = (Resource)Activator.CreateInstance(entry.ResourceType, new object[] { pResource.ResourcePath, pResource.ResourceData });

                _resourceCache[pResource.ResourcePath] = resource;
                foreach (Action<Resource> callback in entry.LoadCallbacks) {
                    try {
                        callback.Invoke(resource);
                    } catch (Exception e) {
                        Console.WriteLine("Exception while calling resource load callback: " + e.Message);
                    }
                }

                _loadingResources.Remove(pResource.ResourcePath);
            } else {
                throw new Exception("Received unexpected resource packet for '" +  pResource.ResourcePath + "'");
            }
        }

        public void HandlePacketBrowseResource(PacketBrowseResource pBrowseResource) {
            CreateCacheFile(pBrowseResource.Filename, pBrowseResource.Data);
        }

        private Resource GetCachedResource(string resourcePath) {
            if (_resourceCache.ContainsKey(resourcePath)) {
                return _resourceCache[resourcePath];
            } else {
                return null;
            }
        }
    }
}
