using OpenDreamShared.Dream;
using OpenDreamShared.Rendering;

namespace OpenDreamClient.Rendering;

[RegisterComponent]
internal sealed partial class DMISpriteComponent : SharedDMISpriteComponent {
    [ViewVariables] public DreamIcon Icon { get; set; }
    [ViewVariables] public ScreenLocation? ScreenLocation { get; set; }

    /// <summary>
    /// Checks if this sprite should be visible to the player<br/>
    /// Checks the appearance's invisibility, and if a transform is given, whether it's parented to another entity
    /// </summary>
    /// <param name="transform">The entity's transform, the parent check is skipped if this is null</param>
    /// <param name="seeInvisibility">The eye's see_invisibility var</param>
    /// <returns></returns>
    public bool IsVisible(TransformComponent? transform, int seeInvisibility) {
        if (Icon.Appearance?.Invisibility > seeInvisibility) return false;

        if (transform != null) {
            //Only render movables not inside another movable's contents (parented to the grid)
            //TODO: Use RobustToolbox's container system/components?
            if (transform.ParentUid != transform.GridUid)
                return false;
        }

        return true;
    }
}
