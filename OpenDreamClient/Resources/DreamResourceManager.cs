using OpenDreamShared;
using OpenDreamShared.Network.Messages;
using OpenDreamClient.Resources.ResourceTypes;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace OpenDreamClient.Resources {
    public interface IDreamResourceManager {
        void Initialize();
        void Shutdown();
        ResourcePath CreateCacheFile(string filename, string data);
        ResourcePath CreateCacheFile(string filename, byte[] data);
        void LoadResourceAsync<T>(int resourceId, Action<T> onLoadCallback) where T : DreamResource;
        ResourcePath GetCacheFilePath(string filename);
    }

    internal sealed class DreamResourceManager : IDreamResourceManager {
        private readonly Dictionary<int, LoadingResourceEntry> _loadingResources = new();
        private readonly Dictionary<int, DreamResource> _resourceCache = new();

        [Dependency] private readonly IResourceManager _resourceManager = default!;
        [Dependency] private readonly IClientNetManager _netManager = default!;
        [Dependency] private readonly IDynamicTypeFactory _typeFactory = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        private ResourcePath _cacheDirectory = default!;

        private ISawmill _sawmill = default!;

        public void Initialize() {
            _sawmill = Logger.GetSawmill("opendream.res");
            InitCacheDirectory();

            _netManager.RegisterNetMessage<MsgBrowseResource>(RxBrowseResource);
            _netManager.RegisterNetMessage<MsgRequestResource>();
            _netManager.RegisterNetMessage<MsgResource>(RxResource);
        }

        public void Shutdown() {
            _resourceManager.UserData.Delete(_cacheDirectory);
        }

        private void InitCacheDirectory() {
            var random = new Random();
            while (true) {
                _cacheDirectory = new ResourcePath($"/OpenDream/Cache/{random.Next()}");
                if (!_resourceManager.UserData.Exists(_cacheDirectory))
                    break;
            }

            Logger.DebugS("opendream.res", $"Cache directory is {_cacheDirectory}");
            _resourceManager.UserData.CreateDir(_cacheDirectory);
        }

        private void RxBrowseResource(MsgBrowseResource message) {
            CreateCacheFile(message.Filename, message.Data);
        }

        private void RxResource(MsgResource message) {
            if (_loadingResources.ContainsKey(message.ResourceId)) {
                LoadingResourceEntry entry = _loadingResources[message.ResourceId];
                DreamResource resource = (DreamResource) _typeFactory.CreateInstance(entry.ResourceType,
                    new object[] {message.ResourceId, message.ResourceData});

                _resourceCache[message.ResourceId] = resource;
                foreach (Action<DreamResource> callback in entry.LoadCallbacks) {
                    try {
                        callback.Invoke(resource);
                    } catch (Exception e) {
                        Logger.Fatal($"Exception while calling resource load callback: {e.Message}");
                    }
                }

                _loadingResources.Remove(message.ResourceId);
            } else {
                throw new Exception($"Received unexpected resource packet for resource id {message.ResourceId}");
            }
        }

        public void LoadResourceAsync<T>(int resourceId, Action<T> onLoadCallback) where T:DreamResource {
            DreamResource? resource = GetCachedResource(resourceId);

            if (resource == null) {
                if (!_loadingResources.ContainsKey(resourceId)) {
                    _loadingResources[resourceId] = new LoadingResourceEntry(typeof(T));

                    var msg = new MsgRequestResource() { ResourceId = resourceId };
                    _netManager.ClientSendMessage(msg);

                    var timeout = _cfg.GetCVar(OpenDreamCVars.DownloadTimeout);
                    Timer.Spawn(TimeSpan.FromSeconds(timeout), () => {
                        if (_loadingResources.ContainsKey(resourceId)) {
                            _sawmill.Warning(
                                $"Resource id {resourceId} was requested, but is still not received {timeout} seconds later.");
                        }
                    });
                }

                _loadingResources[resourceId].LoadCallbacks.Add(loadedResource => {
                    onLoadCallback.Invoke((T)loadedResource);
                });
            } else {
                onLoadCallback.Invoke((T)resource);
            }
        }

        public ResourcePath GetCacheFilePath(string filename)
        {
            return _cacheDirectory / new ResourcePath(filename).ToRelativePath();
        }

        public ResourcePath CreateCacheFile(string filename, string data)
        {
            // in BYOND when filename is a path everything except the filename at the end gets ignored - meaning all resource files end up directly in the cache folder
            var path = _cacheDirectory / new ResourcePath(filename).Filename;
            _resourceManager.UserData.WriteAllText(path, data);
            return new ResourcePath(filename);
        }

        public ResourcePath CreateCacheFile(string filename, byte[] data)
        {
            // in BYOND when filename is a path everything except the filename at the end gets ignored - meaning all resource files end up directly in the cache folder
            var path = _cacheDirectory / new ResourcePath(filename).Filename;
            _resourceManager.UserData.WriteAllBytes(path, data);
            return new ResourcePath(filename);
        }

        private DreamResource? GetCachedResource(int resourceId) {
            _resourceCache.TryGetValue(resourceId, out var cached);

            return cached;
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
