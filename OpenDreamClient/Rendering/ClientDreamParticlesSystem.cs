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
        SubscribeLocalEvent<DreamParticlesComponent, AfterAutoHandleStateEvent>(OnDreamParticlesComponentChange);
        SubscribeLocalEvent<DreamParticlesComponent, ComponentRemove>(HandleComponentRemove);
        RenderTargetPool = new(_clyde);
    }

    private void OnDreamParticlesComponentChange(EntityUid uid, DreamParticlesComponent component, ref AfterAutoHandleStateEvent args)
    {
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
        result.Acceleration = (float _ , Vector3 velocity) => GetGeneratorVector3(component.AccelerationLow, component.AccelerationHigh, component.AccelerationType)() + GetGeneratorVector3(component.DriftLow, component.DriftHigh, component.DriftType)() - velocity*GetGeneratorVector3(component.FrictionLow, component.FrictionHigh, component.FrictionType)();
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
        result.BaseTransform = Matrix3x2.Identity;

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
