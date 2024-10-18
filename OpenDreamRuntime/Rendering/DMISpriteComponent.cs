using OpenDreamShared.Dream;
using OpenDreamShared.Rendering;

namespace OpenDreamRuntime.Rendering;

[RegisterComponent]
public sealed partial class DMISpriteComponent : SharedDMISpriteComponent {
    [ViewVariables]
    [Access(typeof(DMISpriteSystem))]
    public ScreenLocation ScreenLocation;

    //[Access(typeof(DMISpriteSystem))]
    [ViewVariables] public ImmutableIconAppearance? Appearance;
}

