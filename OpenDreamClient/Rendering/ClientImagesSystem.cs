using System.Diagnostics.CodeAnalysis;
using OpenDreamShared.Rendering;

namespace OpenDreamClient.Rendering;
internal sealed class ClientImagesSystem : SharedClientImagesSystem {
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private readonly Dictionary<Vector3, List<NetEntity>> _turfClientImages = new();
    private readonly Dictionary<EntityUid, List<NetEntity>> _amClientImages = new();

    public override void Initialize() {
        SubscribeNetworkEvent<AddClientImageEvent>(OnAddClientImage);
        SubscribeNetworkEvent<RemoveClientImageEvent>(OnRemoveClientImage);
    }

    public override void Shutdown() {
        _turfClientImages.Clear();
        _amClientImages.Clear();
    }

    public bool TryGetClientImages(EntityUid entity, Vector3? tileCoords, [NotNullWhen(true)] out List<NetEntity>? result){
        if(entity == EntityUid.Invalid && tileCoords is not null) {
            if(!_turfClientImages.TryGetValue(tileCoords.Value, out result))
                return false;
        } else {
            if(!_amClientImages.TryGetValue(entity, out result))
                return false;
        }
        return result.Count > 0;
    }

    private void OnAddClientImage(AddClientImageEvent e) {
        EntityUid ent = _entityManager.GetEntity(e.AttachedEntity);
        if(ent == EntityUid.Invalid) {
            if(!_turfClientImages.TryGetValue(e.TurfCoords, out var iconList))
                iconList = new List<NetEntity>();
            iconList.Add(e.ImageEntity);
            _turfClientImages[e.TurfCoords] = iconList;
        } else {
            if(!_amClientImages.TryGetValue(ent, out var iconList))
                iconList = new List<NetEntity>();
            iconList.Add(e.ImageEntity);
            _amClientImages[ent] = iconList;
        }

    }

    private void OnRemoveClientImage(RemoveClientImageEvent e) {
        EntityUid ent = _entityManager.GetEntity(e.AttachedEntity);
        if(ent == EntityUid.Invalid) {
            if(!_turfClientImages.TryGetValue(e.TurfCoords, out var iconList))
                return;
            iconList.Remove(e.ImageEntity);
            if(iconList.Count == 0)
                _turfClientImages.Remove(e.TurfCoords);

        } else {
            if(!_amClientImages.TryGetValue(ent, out var iconList))
                return;
            iconList.Remove(e.ImageEntity);
            if(iconList.Count == 0)
                _amClientImages.Remove(ent);

        }
    }
}
