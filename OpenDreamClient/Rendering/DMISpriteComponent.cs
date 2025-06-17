using OpenDreamShared.Dream;
using OpenDreamShared.Rendering;

namespace OpenDreamClient.Rendering;

[RegisterComponent]
internal sealed partial class DMISpriteComponent : SharedDMISpriteComponent {
    [ViewVariables] public DreamIcon Icon { get; set; }
    [ViewVariables] public ScreenLocation? ScreenLocation { get; set; }
}
