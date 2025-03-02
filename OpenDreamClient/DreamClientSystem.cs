using OpenDreamClient.Interface;
using OpenDreamClient.Rendering;
using OpenDreamShared.Dream;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace OpenDreamClient;

internal sealed class DreamClientSystem : EntitySystem {
    [Dependency] private readonly IDreamInterfaceManager _interfaceManager = default!;
    [Dependency] private readonly ClientAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;

    public override void Initialize() {
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
    }

    public string GetName(ClientObjectReference reference) {
        switch (reference.Type) {
            case ClientObjectReference.RefType.Client:
                return _playerManager.LocalSession?.Name ?? "<unknown>";
            case ClientObjectReference.RefType.Entity:
                var entity = _entityManager.GetEntity(reference.Entity);
                var metadata = _entityManager.GetComponent<MetaDataComponent>(entity);

                return metadata.EntityName;
            case ClientObjectReference.RefType.Turf:
                var mapCoords = new MapCoordinates(reference.TurfX, reference.TurfY, new(reference.TurfZ));
                if (!_mapManager.TryFindGridAt(mapCoords, out _, out var grid))
                    break;
                if (!_mapSystem.TryGetTile(grid, new(reference.TurfX, reference.TurfY), out var tile))
                    break;

                var icon = _appearanceSystem.GetTurfIcon((uint)tile.TypeId);
                return icon.Appearance?.Name ?? "<unknown>";
        }

        return "<unknown>";
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent e) {
        // The active input context gets reset to "common" when a new player is attached
        // So we have to set it again
        _interfaceManager.DefaultWindow?.Macro.SetActive();
    }
}
