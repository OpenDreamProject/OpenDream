using JetBrains.Annotations;
using OpenDreamShared.Rendering;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace OpenDreamClient.Rendering;

[UsedImplicitly]
public sealed class ClientDreamParticlesSystem : SharedDreamParticlesSystem
{
    [Dependency] private readonly ParticlesManager _particlesManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    private Random random = new();

    public override void Initialize() {
        base.Initialize();
        SubscribeLocalEvent<DynamicParticlesComponent, ComponentChange>(OnDynamicParticlesComponentChange);
        SubscribeLocalEvent<DynamicParticlesComponent, ComponentAdd>(HandleComponentAdd);
        SubscribeLocalEvent<DynamicParticlesComponent, ComponentRemove>(HandleComponentRemove);
    }

    private void OnDynamicParticlesComponentChange(EntityUid uid, DynamicParticlesComponent component, ref ComponentChange args)
    {
        if(_particlesManager.TryGetParticleSystem(uid, out var system))
            system.UpdateSystem(GetParticleSystemArgs(component));
    }

    private void HandleComponentAdd(EntityUid uid, DynamicParticlesComponent component, ref ComponentAdd args)
    {
        component.particlesSystem = _particlesManager.CreateParticleSystem(uid, GetParticleSystemArgs(component));
    }

    private void HandleComponentRemove(EntityUid uid, DynamicParticlesComponent component, ref ComponentRemove args)
    {
        component.particlesSystem = null;
        _particlesManager.DestroyParticleSystem(uid);
    }

    private ParticleSystemArgs GetParticleSystemArgs(DynamicParticlesComponent component){
        Func<Texture> textureFunc;
        if(component.TextureList is null || component.TextureList.Length == 0)
            textureFunc = () => Texture.White;
        else
            textureFunc = () => _resourceCache.GetResource<TextureResource>(new Random().Pick(component.TextureList)); //TODO

        var result = new ParticleSystemArgs(textureFunc, new Vector2i(component.Width, component.Height), (uint)component.Count, component.Spawning);

        GeneratorFloat lifespan = new();
        result.Lifespan = GetGeneratorFloat(component.LifespanLow, component.LifespanHigh, component.LifespanType);
        result.Fadein = GetGeneratorFloat(component.FadeInLow, component.FadeInHigh, component.FadeInType);
        result.Fadeout = GetGeneratorFloat(component.FadeOutLow, component.FadeOutHigh, component.FadeOutType);
        if(component.ColorList.Length > 0)
            result.Color = (float lifetime) => {
                var colorIndex = (int)(lifetime * component.ColorList.Length);
                colorIndex = Math.Clamp(colorIndex, 0, component.ColorList.Length - 1);
                return component.ColorList[colorIndex];
            };
        else
            result.Color = (float lifetime) => System.Drawing.Color.White;
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
