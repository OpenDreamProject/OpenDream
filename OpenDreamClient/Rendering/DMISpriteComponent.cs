using OpenDreamShared.Dream;
using OpenDreamShared.Rendering;

namespace OpenDreamClient.Rendering;

[RegisterComponent]
internal sealed partial class DMISpriteComponent : SharedDMISpriteComponent {
    [ViewVariables] public DreamIcon Icon { get; set; }
    [ViewVariables] public ScreenLocation? ScreenLocation { get; set; }

    [Dependency] private readonly IEntityManager _entityManager = default!;

    public bool IsVisible(bool checkWorld = true, int seeInvis = 0) {
        if (Icon.Appearance?.Invisibility > seeInvis) return false;

        if (checkWorld) {
            //Only render movables not inside another movable's contents (parented to the grid)
            //TODO: Use RobustToolbox's container system/components?
            if (!_entityManager.TryGetComponent<TransformComponent>(Owner, out var transform))
                return false;

            if (transform.ParentUid != transform.GridUid)
                return false;
        }

        return true;
    }
}
