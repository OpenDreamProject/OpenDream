using System.Diagnostics.CodeAnalysis;
using OpenDreamShared.Rendering;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace OpenDreamClient.Rendering;
sealed class ClientImagesSystem : SharedClientImagesSystem {
    private readonly Dictionary<Vector3, List<NetEntity>> TurfClientImages = new();
    private readonly Dictionary<EntityUid, List<NetEntity>> AMClientImages = new();
    private readonly Dictionary<int, DreamIcon> _idToIcon = new();
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

    public bool TryGetClientImages(EntityUid entity, Vector3? tileCoords, [NotNullWhen(true)] out List<NetEntity>? result){
        if(entity == EntityUid.Invalid && tileCoords is not null) {
            if(!TurfClientImages.TryGetValue(tileCoords.Value, out result))
                return false;
        } else {
            if(!AMClientImages.TryGetValue(entity, out result))
                return false;
        }
        return result.Count > 0;
    }

    private void OnAddClientImage(AddClientImageEvent e) {
        EntityUid ent = _entityManager.GetEntity(e.AttachedEntity);
        if(ent == EntityUid.Invalid) {
            if(!TurfClientImages.TryGetValue(e.TurfCoords, out var iconList))
                iconList = new List<NetEntity>();
            iconList.Add(e.ImageEntity);
            TurfClientImages[e.TurfCoords] = iconList;
        } else {
            if(!AMClientImages.TryGetValue(ent, out var iconList))
                iconList = new List<NetEntity>();
            iconList.Add(e.ImageEntity);
            AMClientImages[ent] = iconList;
        }

    }

    private void OnRemoveClientImage(RemoveClientImageEvent e) {
        EntityUid ent = _entityManager.GetEntity(e.AttachedEntity);
        if(ent == EntityUid.Invalid) {
            if(!TurfClientImages.TryGetValue(e.TurfCoords, out var iconList))
                return;
            iconList.Remove(e.ImageEntity);
            if(iconList.Count == 0)
                TurfClientImages.Remove(e.TurfCoords);
            else
                TurfClientImages[e.TurfCoords] = iconList;
        } else {
            if(!AMClientImages.TryGetValue(ent, out var iconList))
                return;
            iconList.Remove(e.ImageEntity);
            if(iconList.Count == 0)
                AMClientImages.Remove(ent);
            else
                AMClientImages[ent] = iconList;
        }
    }
}
