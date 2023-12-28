using System.Diagnostics.CodeAnalysis;
using OpenDreamShared.Rendering;
using Robust.Shared.Timing;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace OpenDreamClient.Rendering;

internal sealed class ClientImagesSystem : SharedClientImagesSystem {
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ClientAppearanceSystem _appearanceSystem = default!;

    private readonly Dictionary<Vector3, List<int>> _turfClientImages = new();
    private readonly Dictionary<EntityUid, List<int>> _amClientImages = new();
    private readonly Dictionary<int, DreamIcon> _idToIcon = new();

    public override void Initialize() {
        SubscribeNetworkEvent<AddClientImageEvent>(OnAddClientImage);
        SubscribeNetworkEvent<RemoveClientImageEvent>(OnRemoveClientImage);
    }

    public override void Shutdown() {
        _turfClientImages.Clear();
        _amClientImages.Clear();
        _idToIcon.Clear();
    }

    public bool TryGetClientImages(EntityUid entity, Vector3? tileCoords, [NotNullWhen(true)] out List<DreamIcon>? result){
        result = null;
        List<int>? resultIDs;
        if(entity == EntityUid.Invalid && tileCoords is not null) {
            if(!_turfClientImages.TryGetValue(tileCoords.Value, out resultIDs))
                return false;
        } else {
            if(!_amClientImages.TryGetValue(entity, out resultIDs))
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
            if(!_turfClientImages.TryGetValue(e.TurfCoords, out var iconList))
                iconList = new List<int>();
            if(!_idToIcon.ContainsKey(e.ImageAppearance)){
                DreamIcon icon = new DreamIcon(_gameTiming, _appearanceSystem, e.ImageAppearance);
                _idToIcon[e.ImageAppearance] = icon;
            }
            iconList.Add(e.ImageAppearance);
            _turfClientImages[e.TurfCoords] = iconList;
        } else {
            if(!_amClientImages.TryGetValue(ent, out var iconList))
                iconList = new List<int>();
            if(!_idToIcon.ContainsKey(e.ImageAppearance)){
                DreamIcon icon = new DreamIcon(_gameTiming, _appearanceSystem, e.ImageAppearance);
                _idToIcon[e.ImageAppearance] = icon;
            }
            iconList.Add(e.ImageAppearance);
            _amClientImages[ent] = iconList;
        }

    }

    private void OnRemoveClientImage(RemoveClientImageEvent e) {
        EntityUid ent = _entityManager.GetEntity(e.AttachedEntity);
        if(ent == EntityUid.Invalid) {
                if(!_turfClientImages.TryGetValue(e.TurfCoords, out var iconList))
                    return;
                iconList.Remove(e.ImageAppearance);
                _turfClientImages[e.TurfCoords] = iconList;
                _idToIcon.Remove(e.ImageAppearance);
        } else {
            if(!_amClientImages.TryGetValue(ent, out var iconList))
                return;
            iconList.Remove(e.ImageAppearance);
            _amClientImages[ent] = iconList;
            _idToIcon.Remove(e.ImageAppearance);
        }
    }
}
