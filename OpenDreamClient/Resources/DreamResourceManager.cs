using OpenDreamShared.Net.Packets;
using OpenDreamClient.Resources.ResourceTypes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;

namespace OpenDreamClient.Resources {
    class DreamResourceManager {
        private class LoadingResourceEntry {
            public Type ResourceType;
            public List<Action<Resource>> LoadCallbacks;

            public LoadingResourceEntry(Type resourceType) {
                ResourceType = resourceType;
                LoadCallbacks = new List<Action<Resource>>();
            }
        }

        private Dictionary<string, LoadingResourceEntry> _loadingResources = new Dictionary<string, LoadingResourceEntry>();
        private Dictionary<string, Resource> _resourceCache = new Dictionary<string, Resource>();

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

        private Resource GetCachedResource(string resourcePath) {
            if (_resourceCache.ContainsKey(resourcePath)) {
                return _resourceCache[resourcePath];
            } else {
                return null;
            }
        }
    }
}
