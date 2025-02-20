using JetBrains.Annotations;
using OpenDreamShared.Rendering;
using Pidgin;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameStates;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using static OpenDreamShared.Rendering.DreamParticlesComponent;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace OpenDreamClient.Rendering;

[UsedImplicitly]
public sealed class ClientDreamParticlesSystem : SharedDreamParticlesSystem
{
    [Dependency] private readonly ParticlesManager _particlesManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ClientAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    public RenderTargetPool RenderTargetPool = default!;
    private Random random = new();
    private RendererMetaData defaultRenderMetaData = new(); //used for icon GetTexture(), never needs anything but default settings

    public override void Initialize() {
        base.Initialize();
        SubscribeLocalEvent<DreamParticlesComponent, ComponentHandleState>(OnDreamParticlesComponentChange);
        SubscribeLocalEvent<DreamParticlesComponent, ComponentRemove>(HandleComponentRemove);
        RenderTargetPool = new(_clyde);
    }

    private void OnDreamParticlesComponentChange(EntityUid uid, DreamParticlesComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not DreamParticlesComponentState state)
                return;
        component.Width = state.Width;
        component.Width = state.Width;
        component.Height = state.Height;
        component.Count = state.Count;
        component.Spawning = state.Spawning;
        component.Bound1 = state.Bound1;
        component.Bound2 = state.Bound2;
        component.Gravity = state.Gravity;
        component.Gradient = state.Gradient;
        component.Transform = state.Transform;
        component.TextureList = state.TextureList;
        component.LifespanHigh = state.LifespanHigh;
        component.LifespanLow = state.LifespanLow;
        component.LifespanType = state.LifespanType;
        component.FadeInHigh = state.FadeInHigh;
        component.FadeInLow = state.FadeInLow;
        component.FadeInType = state.FadeInType;
        component.FadeOutHigh = state.FadeOutHigh;
        component.FadeOutLow = state.FadeOutLow;
        component.FadeOutType = state.FadeOutType;
        component.SpawnPositionHigh = state.SpawnPositionHigh;
        component.SpawnPositionLow = state.SpawnPositionLow;
        component.SpawnPositionType = state.SpawnPositionType;
        component.SpawnVelocityHigh = state.SpawnVelocityHigh;
        component.SpawnVelocityLow = state.SpawnVelocityLow;
        component.SpawnVelocityType = state.SpawnVelocityType;
        component.AccelerationHigh = state.AccelerationHigh;
        component.AccelerationLow = state.AccelerationLow;
        component.AccelerationType = state.AccelerationType;
        component.FrictionHigh = state.FrictionHigh;
        component.FrictionLow = state.FrictionLow;
        component.FrictionType = state.FrictionType;
        component.ScaleHigh = state.ScaleHigh;
        component.ScaleLow = state.ScaleLow;
        component.ScaleType = state.ScaleType;
        component.RotationHigh = state.RotationHigh;
        component.RotationLow = state.RotationLow;
        component.RotationType = state.RotationType;
        component.GrowthHigh = state.GrowthHigh;
        component.GrowthLow = state.GrowthLow;
        component.GrowthType = state.GrowthType;
        component.SpinHigh = state.SpinHigh;
        component.SpinLow = state.SpinLow;
        component.SpinType = state.SpinType;
        component.DriftHigh = state.DriftHigh;
        component.DriftLow = state.DriftLow;
        component.DriftType = state.DriftType;
        if(_particlesManager.TryGetParticleSystem(uid, out var system))
            system.UpdateSystem(GetParticleSystemArgs(component));
        else
            _particlesManager.CreateParticleSystem(uid, GetParticleSystemArgs(component));
    }
    private void HandleComponentRemove(EntityUid uid, DreamParticlesComponent component, ref ComponentRemove args)
    {
        _particlesManager.DestroyParticleSystem(uid);
    }

    private ParticleSystemArgs GetParticleSystemArgs(DreamParticlesComponent component){
        Func<Texture> textureFunc;
        if(component.TextureList is null || component.TextureList.Length == 0)
            textureFunc = () => Texture.White;
        else{
            List<DreamIcon> icons = new(component.TextureList.Length);
            foreach(var appearance in component.TextureList){
                DreamIcon icon = new DreamIcon(RenderTargetPool, _gameTiming, _clyde, _appearanceSystem);
                icon.SetAppearance(appearance.MustGetId());
                icons.Add(icon);
            }
            textureFunc = () => random.Pick(icons).GetTexture(null!, null!, defaultRenderMetaData, null) ?? Texture.White; //oh god, so hacky
        }
        var result = new ParticleSystemArgs(textureFunc, new Vector2i(component.Width, component.Height), (uint)component.Count, component.Spawning);
        GeneratorFloat lifespan = new();
        result.Lifespan = GetGeneratorFloat(component.LifespanLow, component.LifespanHigh, component.LifespanType);
        result.Fadein = GetGeneratorFloat(component.FadeInLow, component.FadeInHigh, component.FadeInType);
        result.Fadeout = GetGeneratorFloat(component.FadeOutLow, component.FadeOutHigh, component.FadeOutType);
        if(component.Gradient.Length > 0)
            result.Color = (float lifetime) => {
                var colorIndex = (int)(lifetime * component.Gradient.Length);
                colorIndex = Math.Clamp(colorIndex, 0, component.Gradient.Length - 1);
                return component.Gradient[colorIndex];
            };
        else
            result.Color = (float lifetime) => Color.White;
        result.Acceleration = (float _ ) => GetGeneratorVector3(component.AccelerationLow, component.AccelerationHigh, component.AccelerationType)();
        result.SpawnPosition = GetGeneratorVector3(component.SpawnPositionLow, component.SpawnPositionHigh, component.SpawnPositionType);
        result.SpawnVelocity = GetGeneratorVector3(component.SpawnVelocityLow, component.SpawnVelocityHigh, component.SpawnVelocityType);
        result.Transform = (float lifetime) => {
            var scale = GetGeneratorVector2(component.ScaleLow, component.ScaleHigh, component.ScaleType)();
            var rotation = GetGeneratorFloat(component.RotationLow, component.RotationHigh, component.RotationType)();
            var growth = GetGeneratorVector2(component.GrowthLow, component.GrowthHigh, component.GrowthType)();
            var spin = GetGeneratorFloat(component.SpinLow, component.SpinHigh, component.SpinType)();
            return Matrix3x2.CreateScale(scale.X + growth.X, scale.Y + growth.Y) *
                Matrix3x2.CreateRotation(rotation + spin);
        };

        return result;
    }

    private Func<float> GetGeneratorFloat(float low, float high, ParticlePropertyType type){
        switch (type) {
            case ParticlePropertyType.HighValue:
                return () => high;
            case ParticlePropertyType.RandomUniform:
                return () => random.NextFloat(low, high);
            default:
                throw new NotImplementedException();
        }
    }

    private Func<Vector2> GetGeneratorVector2(Vector2 low, Vector2 high, ParticlePropertyType type){
        switch (type) {
            case ParticlePropertyType.HighValue:
                return () => high;
            case ParticlePropertyType.RandomUniform:
                return () => new Vector2(random.NextFloat(low.X, high.X), random.NextFloat(low.Y, high.Y));
            default:
                throw new NotImplementedException();
        }
    }

    private Func<Vector3> GetGeneratorVector3(Vector3 low, Vector3 high, ParticlePropertyType type){
        switch (type) {
            case ParticlePropertyType.HighValue:
                return () => high;
            case ParticlePropertyType.RandomUniform:
                return () => new Vector3(random.NextFloat(low.X, high.X), random.NextFloat(low.Y, high.Y), random.NextFloat(low.Z, high.Z));
            default:
                throw new NotImplementedException();
        }
    }
}
