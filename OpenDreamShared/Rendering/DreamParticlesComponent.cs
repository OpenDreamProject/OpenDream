
using System.Numerics;
using OpenDreamShared.Dream;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace OpenDreamShared.Rendering;

[NetworkedComponent]
public abstract partial class SharedDreamParticlesComponent : Component {
    [ViewVariables(VVAccess.ReadWrite)] public int Width = 640;
    [ViewVariables(VVAccess.ReadWrite)] public int Height = 480;
    [ViewVariables(VVAccess.ReadWrite)] public int Count = 1000;
    [ViewVariables(VVAccess.ReadWrite)] public float Spawning = 100;
    [ViewVariables(VVAccess.ReadWrite)] public Vector3 Bound1;
    [ViewVariables(VVAccess.ReadWrite)] public Vector3 Bound2;
    [ViewVariables(VVAccess.ReadWrite)] public Vector3 Gravity;
    [ViewVariables(VVAccess.ReadWrite)] public Color[] Gradient = [];
    [ViewVariables(VVAccess.ReadWrite)] public Matrix3x2 Transform;
    [ViewVariables(VVAccess.ReadWrite)] public ImmutableAppearance[] TextureList = [];
    [ViewVariables(VVAccess.ReadWrite)] public float LifespanHigh;
    [ViewVariables(VVAccess.ReadWrite)] public float LifespanLow;
    [ViewVariables(VVAccess.ReadWrite)] public ParticlePropertyType LifespanType;
    [ViewVariables(VVAccess.ReadWrite)] public int FadeInHigh = 0;
    [ViewVariables(VVAccess.ReadWrite)] public int FadeInLow = 0;
    [ViewVariables(VVAccess.ReadWrite)] public ParticlePropertyType FadeInType;
    [ViewVariables(VVAccess.ReadWrite)] public int FadeOutHigh = 0;
    [ViewVariables(VVAccess.ReadWrite)] public int FadeOutLow = 0;
    [ViewVariables(VVAccess.ReadWrite)] public ParticlePropertyType FadeOutType;

    [ViewVariables(VVAccess.ReadWrite)] public Vector3 SpawnPositionHigh;
    [ViewVariables(VVAccess.ReadWrite)] public Vector3 SpawnPositionLow;
    [ViewVariables(VVAccess.ReadWrite)] public ParticlePropertyType SpawnPositionType;
	//Starting velocity of the particles
    [ViewVariables(VVAccess.ReadWrite)] public Vector3 SpawnVelocityHigh;
    [ViewVariables(VVAccess.ReadWrite)] public Vector3 SpawnVelocityLow;
    [ViewVariables(VVAccess.ReadWrite)] public ParticlePropertyType SpawnVelocityType;
	//Acceleration applied to the particles per second
    [ViewVariables(VVAccess.ReadWrite)] public Vector3 AccelerationHigh;
    [ViewVariables(VVAccess.ReadWrite)] public Vector3 AccelerationLow;
    [ViewVariables(VVAccess.ReadWrite)] public ParticlePropertyType AccelerationType;
    [ViewVariables(VVAccess.ReadWrite)] public Vector3 FrictionHigh;
    [ViewVariables(VVAccess.ReadWrite)] public Vector3 FrictionLow;
    [ViewVariables(VVAccess.ReadWrite)] public ParticlePropertyType FrictionType;
	//Scaling applied to the particles in (x,y)
    [ViewVariables(VVAccess.ReadWrite)] public Vector2 ScaleHigh;
    [ViewVariables(VVAccess.ReadWrite)] public Vector2 ScaleLow;
    [ViewVariables(VVAccess.ReadWrite)] public ParticlePropertyType ScaleType;
	//Rotation applied to the particles in degrees
    [ViewVariables(VVAccess.ReadWrite)] public float RotationHigh = 0;
    [ViewVariables(VVAccess.ReadWrite)] public float RotationLow = 0;
    [ViewVariables(VVAccess.ReadWrite)] public ParticlePropertyType RotationType;
	//Increase in scale per second
    [ViewVariables(VVAccess.ReadWrite)] public Vector2 GrowthHigh;
    [ViewVariables(VVAccess.ReadWrite)] public Vector2 GrowthLow;
    [ViewVariables(VVAccess.ReadWrite)] public ParticlePropertyType GrowthType;
	//Change in rotation per second
    [ViewVariables(VVAccess.ReadWrite)] public float SpinHigh = 0;
    [ViewVariables(VVAccess.ReadWrite)] public float SpinLow = 0;
    [ViewVariables(VVAccess.ReadWrite)] public ParticlePropertyType SpinType;
}
