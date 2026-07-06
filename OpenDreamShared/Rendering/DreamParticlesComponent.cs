using System;
using System.Numerics;
using OpenDreamShared.Dream;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace OpenDreamShared.Rendering;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, fieldDeltas: true)]
public sealed partial class DreamParticlesComponent : Component {
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField] public ParticleData Data;
    [Serializable, NetSerializable]
    public sealed class ParticleData {
        [ViewVariables(VVAccess.ReadWrite)] public int Width;
        [ViewVariables(VVAccess.ReadWrite)] public int Height;
        [ViewVariables(VVAccess.ReadWrite)] public int Count;
        [ViewVariables(VVAccess.ReadWrite)] public float Spawning;
        [ViewVariables(VVAccess.ReadWrite)] public Vector3 Bound1;
        [ViewVariables(VVAccess.ReadWrite)] public Vector3 Bound2;
        [ViewVariables(VVAccess.ReadWrite)] public Vector3 Gravity;
        [ViewVariables(VVAccess.ReadWrite)] public Color[] Gradient = [];
        [ViewVariables(VVAccess.ReadWrite)] public Matrix3x2 Transform;
        [ViewVariables(VVAccess.ReadWrite)] public ImmutableAppearance[] TextureList = [];
        [ViewVariables(VVAccess.ReadWrite)] public IGeneratorNum? Lifespan;
        [ViewVariables(VVAccess.ReadWrite)] public IGeneratorNum? FadeIn;
        [ViewVariables(VVAccess.ReadWrite)] public IGeneratorNum? FadeOut;

        [ViewVariables(VVAccess.ReadWrite)] public IGeneratorVector? SpawnPosition;

        //Starting velocity of the particles
        [ViewVariables(VVAccess.ReadWrite)] public IGeneratorVector? SpawnVelocity;

        //Acceleration applied to the particles per second
        [ViewVariables(VVAccess.ReadWrite)] public IGeneratorVector? Friction;

        //Scaling applied to the particles in (x,y)
        [ViewVariables(VVAccess.ReadWrite)] public IGeneratorVector Scale = new GeneratorNum(1);

        //Rotation applied to the particles in degrees
        [ViewVariables(VVAccess.ReadWrite)] public IGeneratorNum? Rotation;

        //Increase in scale per second
        [ViewVariables(VVAccess.ReadWrite)] public IGeneratorVector? Growth;

        //Change in rotation per second
        [ViewVariables(VVAccess.ReadWrite)] public IGeneratorNum? Spin;
        [ViewVariables(VVAccess.ReadWrite)] public IGeneratorVector? Drift;
    }
}
