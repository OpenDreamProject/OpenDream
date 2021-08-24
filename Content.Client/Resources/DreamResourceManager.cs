using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Client.Resources.ResourceTypes;
using Content.Shared.Network.Messages;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Client.Resources
{
    public interface IDreamResourceManager
    {
        void Initialize();
        void Shutdown();
        ResourcePath CreateCacheFile(string filename, string data);
        ResourcePath CreateCacheFile(string filename, byte[] data);
        void LoadResourceAsync<T>(string resourcePath, Action<T> onLoadCallback) where T:DreamResource;
    }

    internal sealed class DreamResourceManager : IDreamResourceManager
    {
        private readonly Dictionary<string, LoadingResourceEntry> _loadingResources = new();
        private readonly Dictionary<string, DreamResource> _resourceCache = new();


        [Dependency] private readonly IResourceManager _resourceManager = default!;
        [Dependency] private readonly IClientNetManager _netManager = default!;
        [Dependency] private readonly IDynamicTypeFactory _typeFactory = default!;

        private ResourcePath _cacheDirectory;

        private ISawmill _sawmill = Logger.GetSawmill("opendream.res");

        public void Initialize()
        {
            InitCacheDirectory();

            _netManager.RegisterNetMessage<MsgBrowseResource>(RxBrowseResource);
            _netManager.RegisterNetMessage<MsgRequestResource>();
            _netManager.RegisterNetMessage<MsgResource>(RxResource);
        }

        public void Shutdown()
        {
            _resourceManager.UserData.Delete(_cacheDirectory);
        }

        private void InitCacheDirectory()
        {
            var random = new Random();
            while (true)
            {
                _cacheDirectory = new ResourcePath($"/OpenDream/Cache/{random.Next()}");
                if (!_resourceManager.UserData.Exists(_cacheDirectory))
                    break;
            }

            Logger.DebugS("opendream.res", $"Cache directory is {_cacheDirectory}");
            _resourceManager.UserData.CreateDir(_cacheDirectory);
        }

        private void RxBrowseResource(MsgBrowseResource message)
        {
            _resourceManager.UserData.WriteAllBytes(_cacheDirectory / message.Filename, message.Data);
        }

        private void RxResource(MsgResource message)
        {
            if (_loadingResources.ContainsKey(message.ResourcePath)) {
                LoadingResourceEntry entry = _loadingResources[message.ResourcePath];
                DreamResource resource = (DreamResource)_typeFactory.CreateInstance(entry.ResourceType, new object[] { message.ResourcePath, message.ResourceData });

                _resourceCache[message.ResourcePath] = resource;
                foreach (Action<DreamResource> callback in entry.LoadCallbacks) {
                    try {
                        callback.Invoke(resource);
                    } catch (Exception e) {
                        Console.WriteLine("Exception while calling resource load callback: " + e.Message);
                    }
                }

                _loadingResources.Remove(message.ResourcePath);
            } else {
                throw new Exception("Received unexpected resource packet for '" +  message.ResourcePath + "'");
            }
        }

        public void LoadResourceAsync<T>(string resourcePath, Action<T> onLoadCallback) where T:DreamResource {
            DreamResource resource = GetCachedResource(resourcePath);

            if (resource == null) {
                if (!_loadingResources.ContainsKey(resourcePath)) {
                    _loadingResources[resourcePath] = new LoadingResourceEntry(typeof(T));
                    var msg = _netManager.CreateNetMessage<MsgRequestResource>();
                    msg.ResourcePath = resourcePath;
                    _netManager.ClientSendMessage(msg);
                    // todo: this is awful and probably thread-unsafe.
                    Task.Delay(5000).ContinueWith(r => {
                        if (_loadingResources.ContainsKey(resourcePath)) {
                            _sawmill.Warning($"Resource '{resourcePath}' was requested, but is still not recieved 5 seconds later.");
                        }
                    });
                }

                _loadingResources[resourcePath].LoadCallbacks.Add((DreamResource resource) => {
                    onLoadCallback.Invoke((T)resource);
                });
            } else {
                onLoadCallback.Invoke((T)resource);
            }
        }


        public ResourcePath CreateCacheFile(string filename, string data)
        {
            var path = _cacheDirectory / filename;
            _resourceManager.UserData.WriteAllText(path, data);
            return path;
        }

        public ResourcePath CreateCacheFile(string filename, byte[] data)
        {
            var path = _cacheDirectory / filename;
            _resourceManager.UserData.WriteAllBytes(path, data);
            return path;
        }

        private DreamResource GetCachedResource(string resourcePath) {
            if (_resourceCache.TryGetValue(resourcePath, out var cached)) {
                return cached;
            } else {
                return null;
            }
        }

        private struct LoadingResourceEntry {
            public Type ResourceType;
            public List<Action<DreamResource>> LoadCallbacks;

            public LoadingResourceEntry(Type resourceType) {
                ResourceType = resourceType;
                LoadCallbacks = new List<Action<DreamResource>>();
            }
        }

    }
}
