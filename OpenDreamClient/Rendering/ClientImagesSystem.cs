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
            ClientImages[e.AttachedObject] ??= new List<DreamIcon>();
            DreamIcon icon = new DreamIcon(e.ImageAppearance);
            ClientImages[e.AttachedObject].Add(icon);
            _idToIcon[e.ImageAppearance] = icon;
        }

        private void OnRemoveClientImage(RemoveClientImageEvent e) {
            DreamIcon icon = _idToIcon[e.ImageAppearance];
            ClientImages[e.AttachedObject].Remove(icon);
            _idToIcon.Remove(e.ImageAppearance);
        }
    }
}
