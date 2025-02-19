
using OpenDreamShared.Rendering;
using Robust.Client.Graphics;

namespace OpenDreamClient.Rendering;

[RegisterComponent]
public sealed partial class DreamParticlesComponent : SharedDreamParticlesComponent {
    public ParticleSystem? particlesSystem;
}
