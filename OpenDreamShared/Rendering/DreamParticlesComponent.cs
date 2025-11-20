using System.Numerics;
using OpenDreamShared.Dream;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace OpenDreamShared.Rendering;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class DreamParticlesComponent : Component {
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public int Width;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public int Height;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public int Count;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public float Spawning;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector3 Bound1;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector3 Bound2;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector3 Gravity;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Color[] Gradient = [];
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Matrix3x2 Transform;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public ImmutableAppearance[] TextureList = [];
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public IGeneratorNum? Lifespan;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public IGeneratorNum? FadeIn;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public IGeneratorNum? FadeOut;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public IGeneratorVector? SpawnPosition;

	//Starting velocity of the particles
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public IGeneratorVector? SpawnVelocity;

	//Acceleration applied to the particles per second
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public IGeneratorVector? Friction;

	//Scaling applied to the particles in (x,y)
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public IGeneratorVector Scale = new GeneratorNum(1);

	//Rotation applied to the particles in degrees
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public IGeneratorNum? Rotation;

	//Increase in scale per second
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public IGeneratorVector? Growth;

	//Change in rotation per second
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public IGeneratorNum? Spin;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public IGeneratorVector? Drift;
}
