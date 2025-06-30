using JetBrains.Annotations;
using OpenDreamClient.Interface;
using OpenDreamShared.Rendering;
using Robust.Client.Graphics;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace OpenDreamClient.Rendering;

[UsedImplicitly]
public sealed class ClientDreamParticlesSystem : SharedDreamParticlesSystem
{
    [Dependency] private readonly ParticlesManager _particlesManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ClientAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly IDreamInterfaceManager _dreamInterfaceManager = default!;
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
        Func<Texture?> textureFunc;
        if(component.TextureList is null || component.TextureList.Length == 0)
            textureFunc = () => Texture.White;
        else{
            List<DreamIcon> icons = new(component.TextureList.Length);
            foreach(var appearance in component.TextureList){
                DreamIcon icon = new DreamIcon(RenderTargetPool, _dreamInterfaceManager,  _gameTiming, _clyde, _appearanceSystem);
                icon.SetAppearance(appearance.MustGetId());
                icons.Add(icon);
            }
            textureFunc = () => random.Pick(icons).GetTexture(null!, null!, defaultRenderMetaData, null, null); //oh god, so hacky
        }
        var result = new ParticleSystemArgs(textureFunc, new Vector2i(component.Width, component.Height), (uint)component.Count, component.Spawning);
        result.Lifespan = GetGeneratorFloat(component.LifespanLow, component.LifespanHigh, component.LifespanDist);
        result.Fadein = GetGeneratorFloat(component.FadeInLow, component.FadeInHigh, component.FadeInDist);
        result.Fadeout = GetGeneratorFloat(component.FadeOutLow, component.FadeOutHigh, component.FadeOutDist);
        if(component.Gradient.Length > 0)
            result.Color = (float lifetime) => {
                var colorIndex = (int)(lifetime * component.Gradient.Length);
                colorIndex = Math.Clamp(colorIndex, 0, component.Gradient.Length - 1);
                return component.Gradient[colorIndex];
            };
        else
            result.Color = (float lifetime) => Color.White;
        result.Acceleration = (float _ , Vector3 velocity) => GetGeneratorVector3(component.AccelerationLow, component.AccelerationHigh, component.AccelerationType, component.AccelerationDist)() + GetGeneratorVector3(component.DriftLow, component.DriftHigh, component.DriftType, component.DriftDist)() - velocity*GetGeneratorVector3(component.FrictionLow, component.FrictionHigh, component.FrictionType, component.FrictionDist)();
        result.SpawnPosition = GetGeneratorVector3(component.SpawnPositionLow, component.SpawnPositionHigh, component.SpawnPositionType, component.SpawnPositionDist);
        result.SpawnVelocity = GetGeneratorVector3(component.SpawnVelocityLow, component.SpawnVelocityHigh, component.SpawnVelocityType, component.SpawnVelocityDist);
        result.Transform = (float lifetime) => {
            var scale = GetGeneratorVector2(component.ScaleLow, component.ScaleHigh, component.ScaleType, component.ScaleDist)();
            var rotation = GetGeneratorFloat(component.RotationLow, component.RotationHigh, component.RotationDist)();
            var growth = GetGeneratorVector2(component.GrowthLow, component.GrowthHigh, component.GrowthType, component.GrowthDist)();
            var spin = GetGeneratorFloat(component.SpinLow, component.SpinHigh, component.SpinDist)();
            return Matrix3x2.CreateScale(scale.X + growth.X, scale.Y + growth.Y) *
                Matrix3x2.CreateRotation(rotation + spin);
        };
        result.BaseTransform = Matrix3x2.Identity;

        return result;
    }

    private Func<float> GetGeneratorFloat(float low, float high, GeneratorDistribution distribution){
        switch (distribution) {
            case GeneratorDistribution.Constant:
                return () => high;
            case GeneratorDistribution.Uniform:
                return () => random.NextFloat(low, high);
            case GeneratorDistribution.Normal:
                return () => (float) Math.Clamp(random.NextGaussian((low+high)/2, (high-low)/6), low, high);
            case GeneratorDistribution.Linear:
                return () => MathF.Sqrt(random.NextFloat(0, 1)) * (high - low) + low;
            case GeneratorDistribution.Square:
                return () => MathF.Cbrt(random.NextFloat(0, 1)) * (high - low) + low;
            default:
                throw new NotImplementedException();
        }
    }

    private Func<Vector2> GetGeneratorVector2(Vector2 low, Vector2 high, GeneratorOutputType type, GeneratorDistribution distribution){
        switch (type) {
            case GeneratorOutputType.Num:
                return () => new Vector2(GetGeneratorFloat(low.X, high.X, distribution)(), GetGeneratorFloat(low.Y, high.Y, distribution)());
            case GeneratorOutputType.Vector:
                return () => Vector2.Lerp(low, high, GetGeneratorFloat(0,1,distribution)());
            case GeneratorOutputType.Box:
                return () => new Vector2(GetGeneratorFloat(low.X, high.X, distribution)(), GetGeneratorFloat(low.Y, high.Y, distribution)());
            case GeneratorOutputType.Circle:
            case GeneratorOutputType.Sphere:
            case GeneratorOutputType.Square:
            case GeneratorOutputType.Cube:
            default:
                throw new NotImplementedException("Unimplemented generator output type");
        }
    }

    private Func<Vector3> GetGeneratorVector3(Vector3 low, Vector3 high, GeneratorOutputType type, GeneratorDistribution distribution){
        switch (type) {
            case GeneratorOutputType.Num:
                return () => new Vector3(GetGeneratorFloat(low.X, high.X, distribution)(), GetGeneratorFloat(low.Y, high.Y, distribution)(), GetGeneratorFloat(low.Z, high.Z, distribution)());
            case GeneratorOutputType.Vector:
                return () => Vector3.Lerp(low, high, GetGeneratorFloat(0,1,distribution)());
            case GeneratorOutputType.Box:
                return () => new Vector3(GetGeneratorFloat(low.X, high.X, distribution)(), GetGeneratorFloat(low.Y, high.Y, distribution)(), GetGeneratorFloat(low.Z, high.Z, distribution)());
            case GeneratorOutputType.Circle:
            case GeneratorOutputType.Sphere:
            case GeneratorOutputType.Square:
            case GeneratorOutputType.Cube:
            default:
                throw new NotImplementedException("Unimplemented generator output type");
        }
    }
}


