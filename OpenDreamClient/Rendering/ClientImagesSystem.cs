using OpenDreamShared.Dream;
using OpenDreamShared.Rendering;

namespace OpenDreamClient.Rendering {
    sealed class ClientImagesSystem : SharedClientImagesSystem {
        private readonly Dictionary<EntityUid, List<DreamIcon>> ClientImages = new();
        private readonly Dictionary<uint, DreamIcon> _idToIcon = new();

        [Dependency] private IEntityManager _entityManager = default!;

        public override void Initialize() {
            SubscribeNetworkEvent<AddClientImageEvent>(OnAddClientImage);
            SubscribeNetworkEvent<RemoveClientImageEvent>(OnRemoveClientImage);
        }

        public override void Shutdown() {
            ClientImages.Clear();
        }

        public Dictionary<EntityUid, List<DreamIcon>> GetClientImages(){
            return ClientImages;
        }

        private void OnAddClientImage(AddClientImageEvent e) {
            if(!ClientImages.TryGetValue(e.AttachedObject, out var iconList))
                iconList = new List<DreamIcon>();
            DreamIcon icon = new DreamIcon(e.ImageAppearance);
            iconList.Add(icon);
            ClientImages[e.AttachedObject] = iconList;
            _idToIcon[e.ImageAppearance] = icon;
        }

        private void OnRemoveClientImage(RemoveClientImageEvent e) {
            if(!_idToIcon.TryGetValue(e.ImageAppearance, out DreamIcon icon))
                return;
            if(!ClientImages.TryGetValue(e.AttachedObject, out var iconList))
                return;
            iconList.Remove(icon);
            ClientImages[e.AttachedObject] = iconList;
            _idToIcon.Remove(e.ImageAppearance);
        }
    }
}
