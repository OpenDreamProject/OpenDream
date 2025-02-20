
using System.Numerics;
using OpenDreamShared.Dream;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using System;
using Robust.Shared.ViewVariables;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace OpenDreamShared.Rendering;

[RegisterComponent, NetworkedComponent]
public sealed partial class DreamParticlesComponent : Component {
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
    [ViewVariables(VVAccess.ReadWrite)] public float LifespanHigh;
    [ViewVariables(VVAccess.ReadWrite)] public float LifespanLow;
    [ViewVariables(VVAccess.ReadWrite)] public ParticlePropertyType LifespanType;
    [ViewVariables(VVAccess.ReadWrite)] public int FadeInHigh;
    [ViewVariables(VVAccess.ReadWrite)] public int FadeInLow;
    [ViewVariables(VVAccess.ReadWrite)] public ParticlePropertyType FadeInType;
    [ViewVariables(VVAccess.ReadWrite)] public int FadeOutHigh;
    [ViewVariables(VVAccess.ReadWrite)] public int FadeOutLow;
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
    [ViewVariables(VVAccess.ReadWrite)] public float RotationHigh;
    [ViewVariables(VVAccess.ReadWrite)] public float RotationLow;
    [ViewVariables(VVAccess.ReadWrite)] public ParticlePropertyType RotationType;
	//Increase in scale per second
    [ViewVariables(VVAccess.ReadWrite)] public Vector2 GrowthHigh;
    [ViewVariables(VVAccess.ReadWrite)] public Vector2 GrowthLow;
    [ViewVariables(VVAccess.ReadWrite)] public ParticlePropertyType GrowthType;
	//Change in rotation per second
    [ViewVariables(VVAccess.ReadWrite)] public float SpinHigh;
    [ViewVariables(VVAccess.ReadWrite)] public float SpinLow;
    [ViewVariables(VVAccess.ReadWrite)] public ParticlePropertyType SpinType;
    [ViewVariables(VVAccess.ReadWrite)] public Vector3 DriftHigh;
    [ViewVariables(VVAccess.ReadWrite)] public Vector3 DriftLow;
    [ViewVariables(VVAccess.ReadWrite)] public ParticlePropertyType DriftType;
    [Serializable, NetSerializable]
    public sealed class DreamParticlesComponentState : ComponentState
    {
        public int Width;
        public int Height;
        public int Count;
        public float Spawning;
        public Vector3 Bound1;
        public Vector3 Bound2;
        public Vector3 Gravity;
        public Color[] Gradient = [];
        public Matrix3x2 Transform;
        public ImmutableAppearance[] TextureList = [];
        public float LifespanHigh;
        public float LifespanLow;
        public ParticlePropertyType LifespanType;
        public int FadeInHigh;
        public int FadeInLow;
        public ParticlePropertyType FadeInType;
        public int FadeOutHigh;
        public int FadeOutLow;
        public ParticlePropertyType FadeOutType;

        public Vector3 SpawnPositionHigh;
        public Vector3 SpawnPositionLow;
        public ParticlePropertyType SpawnPositionType;
        //Starting velocity of the particles
        public Vector3 SpawnVelocityHigh;
        public Vector3 SpawnVelocityLow;
        public ParticlePropertyType SpawnVelocityType;
        //Acceleration applied to the particles per second
        public Vector3 AccelerationHigh;
        public Vector3 AccelerationLow;
        public ParticlePropertyType AccelerationType;
        public Vector3 FrictionHigh;
        public Vector3 FrictionLow;
        public ParticlePropertyType FrictionType;
        //Scaling applied to the particles in (x,y)
        public Vector2 ScaleHigh;
        public Vector2 ScaleLow;
        public ParticlePropertyType ScaleType;
        //Rotation applied to the particles in degrees
        public float RotationHigh;
        public float RotationLow;
        public ParticlePropertyType RotationType;
        //Increase in scale per second
        public Vector2 GrowthHigh;
        public Vector2 GrowthLow;
        public ParticlePropertyType GrowthType;
        //Change in rotation per second
        public float SpinHigh;
        public float SpinLow;
        public ParticlePropertyType SpinType;
        public Vector3 DriftHigh;
        public Vector3 DriftLow;
        public ParticlePropertyType DriftType;
    }
}
