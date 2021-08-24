using System;
using Content.Shared.Network.Messages;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Client
{
    public interface IDreamResourceManager
    {
        void Initialize();
        void Shutdown();
    }

    internal sealed class DreamResourceManager : IDreamResourceManager
    {
        [Dependency] private readonly IResourceManager _resourceManager = default!;
        [Dependency] private readonly IClientNetManager _netManager = default!;

        private ResourcePath _cacheDirectory;

        public void Initialize()
        {
            InitCacheDirectory();

            _netManager.RegisterNetMessage<MsgBrowseResource>(RxBrowseResource);
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
    }
}
