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
        ResPath CreateCacheFile(string filename, string data);
        ResPath CreateCacheFile(string filename, byte[] data);

        /// <param name="resourceId">Integer ID of the resource, as assigned by the server.</param>
        /// <param name="onLoadCallback">
        /// Callback to run when this resource is done loading.
        /// Note that if the resource is immediately available,
        /// this callback is immediately invoked before this function returns.
        /// </param>
        /// <typeparam name="T">The type of resource to load as.</typeparam>
        void LoadResourceAsync<T>(int resourceId, Action<T> onLoadCallback) where T : DreamResource;
        ResPath GetCacheFilePath(string filename);
    }

    internal sealed class DreamResourceManager : IDreamResourceManager {
        private readonly Dictionary<int, LoadingResourceEntry> _loadingResources = new();
        private readonly Dictionary<int, DreamResource> _resourceCache = new();

        [Dependency] private readonly IResourceManager _resourceManager = default!;
        [Dependency] private readonly IClientNetManager _netManager = default!;
        [Dependency] private readonly IDynamicTypeFactory _typeFactory = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        private ResPath _cacheDirectory = default!;

        private ISawmill _sawmill = default!;

        public void Initialize() {
            _sawmill = Logger.GetSawmill("opendream.res");
            InitCacheDirectory();

            _netManager.RegisterNetMessage<MsgBrowseResource>(RxBrowseResource);
            _netManager.RegisterNetMessage<MsgRequestResource>();
            _netManager.RegisterNetMessage<MsgResource>(RxResource);
            _netManager.RegisterNetMessage<MsgNotifyResourceUpdate>(RxResourceUpdateNotification);
        }

        public void Shutdown() {
            _resourceManager.UserData.Delete(_cacheDirectory);
        }

        private void InitCacheDirectory() {
            var random = new Random();
            while (true) {
                _cacheDirectory = new ResPath($"/OpenDream/Cache/{random.Next()}");
                if (!_resourceManager.UserData.Exists(_cacheDirectory))
                    break;
            }

            _sawmill.Debug($"Cache directory is {_cacheDirectory}");
            _resourceManager.UserData.CreateDir(_cacheDirectory);
        }

        private void RxBrowseResource(MsgBrowseResource message) {
            CreateCacheFile(message.Filename, message.Data);
        }

        private void RxResource(MsgResource message) {
            if (_loadingResources.ContainsKey(message.ResourceId)) {
                LoadingResourceEntry entry = _loadingResources[message.ResourceId];
                DreamResource resource = LoadResourceFromData(
                    entry.ResourceType,
                    message.ResourceId,
                    message.ResourceData);

                _resourceCache[message.ResourceId] = resource;
                foreach (Action<DreamResource> callback in entry.LoadCallbacks) {
                    try {
                        callback.Invoke(resource);
                    } catch (Exception e) {
                        _sawmill.Fatal($"Exception while calling resource load callback: {e.Message}");
                    }
                }

                _loadingResources.Remove(message.ResourceId);
            } else {
                throw new Exception($"Received unexpected resource packet for resource id {message.ResourceId}");
            }
        }

        private void RxResourceUpdateNotification(MsgNotifyResourceUpdate message) {
            if (!_loadingResources.ContainsKey(message.ResourceId) && _resourceCache.TryGetValue(message.ResourceId, out var cached)) { //either we're already requesting it, or we don't have it so don't need to update
                _sawmill.Debug($"Resource id {message.ResourceId} was updated, reloading");
                _loadingResources[message.ResourceId] = new LoadingResourceEntry(cached.GetType());
                var msg = new MsgRequestResource() { ResourceId = message.ResourceId };
                _netManager.ClientSendMessage(msg);
            }
        }

        public void LoadResourceAsync<T>(int resourceId, Action<T> onLoadCallback) where T:DreamResource {
            DreamResource? resource = GetCachedResource(resourceId);

            if (resource != null) {
                onLoadCallback.Invoke((T)resource);
                return;
            }

            // Check if file exists in local Robust resources.
            if (_resourceManager.TryContentFileRead($"/Rsc/{resourceId}", out var stream)) {
                byte[] data;
                using (stream) {
                    data = stream.CopyToArray();
                }

                _sawmill.Verbose($"File existed locally, skipping server request: {resourceId}");

                resource = LoadResourceFromData(typeof(T), resourceId, data);

                onLoadCallback((T)resource);
                return;
            }

            // File does not exist locally. Send a request to the server.
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
        }

        private DreamResource LoadResourceFromData(Type resourceType, int resourceId, byte[] data) {
            var resource = (DreamResource) _typeFactory.CreateInstance(resourceType,
                new object[] {resourceId, data});

            _resourceCache[resourceId] = resource;
            return resource;
        }

        public ResPath GetCacheFilePath(string filename)
        {
            return _cacheDirectory / new ResPath(filename).ToRelativePath();
        }

        public ResPath CreateCacheFile(string filename, string data)
        {
            // in BYOND when filename is a path everything except the filename at the end gets ignored - meaning all resource files end up directly in the cache folder
            var path = _cacheDirectory / new ResPath(filename).Filename;
            _resourceManager.UserData.WriteAllText(path, data);
            return new ResPath(filename);
        }

        public ResPath CreateCacheFile(string filename, byte[] data)
        {
            // in BYOND when filename is a path everything except the filename at the end gets ignored - meaning all resource files end up directly in the cache folder
            var path = _cacheDirectory / new ResPath(filename).Filename;
            _resourceManager.UserData.WriteAllBytes(path, data);
            return new ResPath(filename);
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
