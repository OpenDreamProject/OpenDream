using OpenDreamShared.Rendering;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace OpenDreamShared.Rendering;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class DreamParticlesComponent : SharedDreamParticlesComponent {}
