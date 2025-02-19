
using System.Numerics;
using OpenDreamShared.Dream;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace OpenDreamShared.Rendering;

[NetworkedComponent, AutoGenerateComponentState(true)]
public abstract partial class SharedDreamParticlesComponent : Component {
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
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public ParticlePropertyType LifespanType;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public int FadeInHigh;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public int FadeInLow;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public ParticlePropertyType FadeInType;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public int FadeOutHigh;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public int FadeOutLow;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public ParticlePropertyType FadeOutType;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector3 SpawnPositionHigh;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector3 SpawnPositionLow;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public ParticlePropertyType SpawnPositionType;
	//Starting velocity of the particles
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector3 SpawnVelocityHigh;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector3 SpawnVelocityLow;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public ParticlePropertyType SpawnVelocityType;
	//Acceleration applied to the particles per second
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector3 AccelerationHigh;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector3 AccelerationLow;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public ParticlePropertyType AccelerationType;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector3 FrictionHigh;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector3 FrictionLow;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public ParticlePropertyType FrictionType;
	//Scaling applied to the particles in (x,y)
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector2 ScaleHigh;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector2 ScaleLow;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public ParticlePropertyType ScaleType;
	//Rotation applied to the particles in degrees
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public float RotationHigh;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public float RotationLow;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public ParticlePropertyType RotationType;
	//Increase in scale per second
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector2 GrowthHigh;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector2 GrowthLow;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public ParticlePropertyType GrowthType;
	//Change in rotation per second
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public float SpinHigh;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public float SpinLow;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public ParticlePropertyType SpinType;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector3 DriftHigh;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public Vector3 DriftLow;
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public ParticlePropertyType DriftType;
}
