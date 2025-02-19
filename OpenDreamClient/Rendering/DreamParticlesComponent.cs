
using OpenDreamShared.Rendering;
using Robust.Client.Graphics;
using Robust.Shared.GameStates;

namespace OpenDreamClient.Rendering;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class DreamParticlesComponent : SharedDreamParticlesComponent {
    public ParticleSystem? particlesSystem;
}
