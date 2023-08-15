using System.Diagnostics.CodeAnalysis;
using System.Linq;
using OpenDreamShared.Dream;
using OpenDreamShared.Rendering;

namespace OpenDreamClient.Rendering;
sealed class ClientImagesSystem : SharedClientImagesSystem {
    private readonly Dictionary<IconAppearance, List<uint>> TurfClientImages = new();
    private readonly Dictionary<EntityUid, List<uint>> AMClientImages = new();
    private readonly Dictionary<uint, DreamIcon> _idToIcon = new();
    [Dependency] private readonly ClientAppearanceSystem _clientAppearanceSystem = default!;
    [Dependency] private IEntityManager _entityManager = default!;

    public override void Initialize() {
        SubscribeNetworkEvent<AddClientImageEvent>(OnAddClientImage);
        SubscribeNetworkEvent<RemoveClientImageEvent>(OnRemoveClientImage);
    }

    public override void Shutdown() {
        TurfClientImages.Clear();
        AMClientImages.Clear();
        _idToIcon.Clear();
    }

    public bool TryGetClientImages(EntityUid entity, IconAppearance appearance, [NotNullWhen(true)] out List<DreamIcon>? result){
        result = null;
        List<uint>? resultIDs;
        if(entity == EntityUid.Invalid) {
            if(!TurfClientImages.TryGetValue(appearance, out resultIDs))
                return false;
        } else {
            if(!AMClientImages.TryGetValue(entity, out resultIDs))
                return false;
        }
        result = new List<DreamIcon>();
        foreach(uint distinctID in resultIDs.Distinct())
            if(_idToIcon.TryGetValue(distinctID, out DreamIcon? icon))
                result.Add(icon);
        return result.Count > 0;
    }

    private void OnAddClientImage(AddClientImageEvent e) {
        if(e.AttachedEntity == EntityUid.Invalid)
            _clientAppearanceSystem.LoadAppearance(e.AttachedAppearance, appearance => {
                if(!TurfClientImages.TryGetValue(appearance, out var iconList))
                    iconList = new List<uint>();
                if(!_idToIcon.ContainsKey(e.ImageAppearance)){
                    DreamIcon icon = new DreamIcon(e.ImageAppearance);
                    _idToIcon[e.ImageAppearance] = icon;
                }
                iconList.Add(e.ImageAppearance);
                TurfClientImages[appearance] = iconList;
            });
        else {
            if(!AMClientImages.TryGetValue(e.AttachedEntity, out var iconList))
                    iconList = new List<uint>();
            if(!_idToIcon.ContainsKey(e.ImageAppearance)){
                DreamIcon icon = new DreamIcon(e.ImageAppearance);
                _idToIcon[e.ImageAppearance] = icon;
            }
            iconList.Add(e.ImageAppearance);
            AMClientImages[e.AttachedEntity] = iconList;
        }

    }

    private void OnRemoveClientImage(RemoveClientImageEvent e) {
        if(e.AttachedEntity == EntityUid.Invalid)
            _clientAppearanceSystem.LoadAppearance(e.AttachedAppearance, appearance => {
                if(!TurfClientImages.TryGetValue(appearance, out var iconList))
                    return;
                iconList.Remove(e.ImageAppearance);
                TurfClientImages[appearance] = iconList;
                _idToIcon.Remove(e.ImageAppearance);
            });
        else {
            if(!AMClientImages.TryGetValue(e.AttachedEntity, out var iconList))
                return;
            iconList.Remove(e.ImageAppearance);
            AMClientImages[e.AttachedEntity] = iconList;
            _idToIcon.Remove(e.ImageAppearance);
        }
    }
}
