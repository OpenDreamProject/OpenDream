using System.Diagnostics.CodeAnalysis;
using System.Linq;
using OpenDreamShared.Dream;
using OpenDreamShared.Rendering;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace OpenDreamClient.Rendering;
sealed class ClientImagesSystem : SharedClientImagesSystem {
    private readonly Dictionary<Vector3, List<int>> TurfClientImages = new();
    private readonly Dictionary<EntityUid, List<int>> AMClientImages = new();
    private readonly Dictionary<int, DreamIcon> _idToIcon = new();
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

    public bool TryGetClientImages(EntityUid entity, Vector3? tileCoords, [NotNullWhen(true)] out List<DreamIcon>? result){
        result = null;
        List<int>? resultIDs;
        if(entity == EntityUid.Invalid && tileCoords is not null) {
            if(!TurfClientImages.TryGetValue(tileCoords.Value, out resultIDs))
                return false;
        } else {
            if(!AMClientImages.TryGetValue(entity, out resultIDs))
                return false;
        }
        result = new List<DreamIcon>();
        foreach(int distinctID in resultIDs)
            if(_idToIcon.TryGetValue(distinctID, out DreamIcon? icon))
                result.Add(icon);
        return result.Count > 0;
    }

    private void OnAddClientImage(AddClientImageEvent e) {
        EntityUid ent = _entityManager.GetEntity(e.AttachedEntity);
        if(ent == EntityUid.Invalid) {
            if(!TurfClientImages.TryGetValue(e.TurfCoords, out var iconList))
                iconList = new List<int>();
            if(!_idToIcon.ContainsKey(e.ImageAppearance)){
                DreamIcon icon = new DreamIcon(e.ImageAppearance);
                _idToIcon[e.ImageAppearance] = icon;
            }
            iconList.Add(e.ImageAppearance);
            TurfClientImages[e.TurfCoords] = iconList;
        } else {
            if(!AMClientImages.TryGetValue(ent, out var iconList))
                iconList = new List<int>();
            if(!_idToIcon.ContainsKey(e.ImageAppearance)){
                DreamIcon icon = new DreamIcon(e.ImageAppearance);
                _idToIcon[e.ImageAppearance] = icon;
            }
            iconList.Add(e.ImageAppearance);
            AMClientImages[ent] = iconList;
        }

    }

    private void OnRemoveClientImage(RemoveClientImageEvent e) {
        EntityUid ent = _entityManager.GetEntity(e.AttachedEntity);
        if(ent == EntityUid.Invalid) {
                if(!TurfClientImages.TryGetValue(e.TurfCoords, out var iconList))
                    return;
                iconList.Remove(e.ImageAppearance);
                TurfClientImages[e.TurfCoords] = iconList;
                _idToIcon.Remove(e.ImageAppearance);
        } else {
            if(!AMClientImages.TryGetValue(ent, out var iconList))
                return;
            iconList.Remove(e.ImageAppearance);
            AMClientImages[ent] = iconList;
            _idToIcon.Remove(e.ImageAppearance);
        }
    }
}
