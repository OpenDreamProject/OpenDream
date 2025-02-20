
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using static OpenDreamShared.Rendering.DreamParticlesComponent;

namespace OpenDreamShared.Rendering;

public abstract class SharedDreamParticlesSystem : EntitySystem {
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DreamParticlesComponent, ComponentGetState>(GetCompState);
    }
    private void GetCompState(Entity<DreamParticlesComponent> ent, ref ComponentGetState args)
    {
        args.State = new DreamParticlesComponentState
        {
            Width = ent.Comp.Width,
            Height = ent.Comp.Height,
            Count = ent.Comp.Count,
            Spawning = ent.Comp.Spawning,
            Bound1 = ent.Comp.Bound1,
            Bound2 = ent.Comp.Bound2,
            Gravity = ent.Comp.Gravity,
            Gradient = ent.Comp.Gradient,
            Transform = ent.Comp.Transform,
            TextureList = ent.Comp.TextureList,
            LifespanHigh = ent.Comp.LifespanHigh,
            LifespanLow = ent.Comp.LifespanLow,
            LifespanType = ent.Comp.LifespanType,
            FadeInHigh = ent.Comp.FadeInHigh,
            FadeInLow = ent.Comp.FadeInLow,
            FadeInType = ent.Comp.FadeInType,
            FadeOutHigh = ent.Comp.FadeOutHigh,
            FadeOutLow = ent.Comp.FadeOutLow,
            FadeOutType = ent.Comp.FadeOutType,
            SpawnPositionHigh = ent.Comp.SpawnPositionHigh,
            SpawnPositionLow = ent.Comp.SpawnPositionLow,
            SpawnPositionType = ent.Comp.SpawnPositionType,
            SpawnVelocityHigh = ent.Comp.SpawnVelocityHigh,
            SpawnVelocityLow = ent.Comp.SpawnVelocityLow,
            SpawnVelocityType = ent.Comp.SpawnVelocityType,
            AccelerationHigh = ent.Comp.AccelerationHigh,
            AccelerationLow = ent.Comp.AccelerationLow,
            AccelerationType = ent.Comp.AccelerationType,
            FrictionHigh = ent.Comp.FrictionHigh,
            FrictionLow = ent.Comp.FrictionLow,
            FrictionType = ent.Comp.FrictionType,
            ScaleHigh = ent.Comp.ScaleHigh,
            ScaleLow = ent.Comp.ScaleLow,
            ScaleType = ent.Comp.ScaleType,
            RotationHigh = ent.Comp.RotationHigh,
            RotationLow = ent.Comp.RotationLow,
            RotationType = ent.Comp.RotationType,
            GrowthHigh = ent.Comp.GrowthHigh,
            GrowthLow = ent.Comp.GrowthLow,
            GrowthType = ent.Comp.GrowthType,
            SpinHigh = ent.Comp.SpinHigh,
            SpinLow = ent.Comp.SpinLow,
            SpinType = ent.Comp.SpinType,
            DriftHigh = ent.Comp.DriftHigh,
            DriftLow = ent.Comp.DriftLow,
            DriftType = ent.Comp.DriftType
        };
    }
}
