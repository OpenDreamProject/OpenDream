using System.Diagnostics.CodeAnalysis;
using OpenDreamShared.Dream;
using OpenDreamShared.Rendering;

namespace OpenDreamClient.Rendering {
    sealed class ClientImagesSystem : SharedClientImagesSystem {
        private readonly Dictionary<IconAppearance, List<DreamIcon>> TurfClientImages = new();
        private readonly Dictionary<EntityUid, List<DreamIcon>> AMClientImages = new();
        private readonly Dictionary<uint, DreamIcon> _idToIcon = new();
        [Dependency] private readonly ClientAppearanceSystem clientAppearanceSystem = default!;
        [Dependency] private IEntityManager _entityManager = default!;

        public override void Initialize() {
            SubscribeNetworkEvent<AddClientImageEvent>(OnAddClientImage);
            SubscribeNetworkEvent<RemoveClientImageEvent>(OnRemoveClientImage);
        }

        public override void Shutdown() {
            TurfClientImages.Clear();
            AMClientImages.Clear();
        }

        public bool TryGetClientImages(EntityUid entity, IconAppearance appearance, [NotNullWhen(true)] out List<DreamIcon>? result){
            if(entity == EntityUid.Invalid)
                return TurfClientImages.TryGetValue(appearance, out result);
            else
                return AMClientImages.TryGetValue(entity, out result);

        }

        private void OnAddClientImage(AddClientImageEvent e) {
            if(e.AttachedEntity == EntityUid.Invalid)
                clientAppearanceSystem.LoadAppearance(e.AttachedAppearance, appearance => {
                    if(!TurfClientImages.TryGetValue(appearance, out var iconList))
                        iconList = new List<DreamIcon>();
                    DreamIcon icon = new DreamIcon(e.ImageAppearance);
                    iconList.Add(icon);
                    TurfClientImages[appearance] = iconList;
                    _idToIcon[e.ImageAppearance] = icon;
                });
            else {
                if(!AMClientImages.TryGetValue(e.AttachedEntity, out var iconList))
                    iconList = new List<DreamIcon>();
                DreamIcon icon = new DreamIcon(e.ImageAppearance);
                iconList.Add(icon);
                AMClientImages[e.AttachedEntity] = iconList;
                _idToIcon[e.ImageAppearance] = icon;
            }

        }

        private void OnRemoveClientImage(RemoveClientImageEvent e) {
            if(e.AttachedEntity == EntityUid.Invalid)
                clientAppearanceSystem.LoadAppearance(e.AttachedAppearance, appearance => {
                    if(!TurfClientImages.TryGetValue(appearance, out var iconList))
                        iconList = new List<DreamIcon>();
                    if(_idToIcon.TryGetValue(e.ImageAppearance, out DreamIcon icon)) {
                        iconList.Remove(icon);
                        TurfClientImages[appearance] = iconList;
                        _idToIcon.Remove(e.ImageAppearance);
                    }
                });
            else {
                if(!AMClientImages.TryGetValue(e.AttachedEntity, out var iconList))
                    iconList = new List<DreamIcon>();
                if(_idToIcon.TryGetValue(e.ImageAppearance, out DreamIcon icon)) {
                    iconList.Remove(icon);
                    AMClientImages[e.AttachedEntity] = iconList;
                    _idToIcon.Remove(e.ImageAppearance);
                }
            }
        }
    }
}
