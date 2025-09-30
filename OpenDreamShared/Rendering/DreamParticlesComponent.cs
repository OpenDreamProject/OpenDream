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
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public float LifespanHigh;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public float LifespanLow;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public GeneratorOutputType LifespanType;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public GeneratorDistribution LifespanDist;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public int FadeInHigh;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public int FadeInLow;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public GeneratorOutputType FadeInType;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public GeneratorDistribution FadeInDist;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public int FadeOutHigh;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public int FadeOutLow;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public GeneratorOutputType FadeOutType;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public GeneratorDistribution FadeOutDist;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector3 SpawnPositionHigh;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector3 SpawnPositionLow;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public GeneratorOutputType SpawnPositionType;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public GeneratorDistribution SpawnPositionDist;

	//Starting velocity of the particles
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector3 SpawnVelocityHigh;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector3 SpawnVelocityLow;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public GeneratorOutputType SpawnVelocityType;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public GeneratorDistribution SpawnVelocityDist;

	//Acceleration applied to the particles per second
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector3 AccelerationHigh;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector3 AccelerationLow;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public GeneratorOutputType AccelerationType;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public GeneratorDistribution AccelerationDist;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector3 FrictionHigh;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector3 FrictionLow;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public GeneratorOutputType FrictionType;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public GeneratorDistribution FrictionDist;

	//Scaling applied to the particles in (x,y)
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector2 ScaleHigh = Vector2.One;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector2 ScaleLow = Vector2.One;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public GeneratorOutputType ScaleType;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public GeneratorDistribution ScaleDist;

	//Rotation applied to the particles in degrees
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public float RotationHigh;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public float RotationLow;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public GeneratorOutputType RotationType;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public GeneratorDistribution RotationDist;

	//Increase in scale per second
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector2 GrowthHigh;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector2 GrowthLow;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public GeneratorOutputType GrowthType;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public GeneratorDistribution GrowthDist;

	//Change in rotation per second
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public float SpinHigh;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public float SpinLow;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public GeneratorOutputType SpinType;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public GeneratorDistribution SpinDist;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector3 DriftHigh;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector3 DriftLow;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public GeneratorOutputType DriftType;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public GeneratorDistribution DriftDist;
}

public enum GeneratorOutputType {
    Num,
    Vector,
    Box,
    Color,
    Circle,
    Sphere,
    Square,
    Cube
}

public enum GeneratorDistribution {
    Constant,
    Uniform,
    Normal,
    Linear,
    Square
}
