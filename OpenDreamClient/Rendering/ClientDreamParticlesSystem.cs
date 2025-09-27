using JetBrains.Annotations;
using OpenDreamClient.Interface;
using OpenDreamShared.Rendering;
using OpenDreamClient.Rendering.Particles;
using Robust.Client.Graphics;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace OpenDreamClient.Rendering;

[UsedImplicitly]
public sealed class ClientDreamParticlesSystem : SharedDreamParticlesSystem {
    [Dependency] private readonly ParticlesManager _particlesManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ClientAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly IDreamInterfaceManager _dreamInterfaceManager = default!;
    [Dependency] private readonly IClyde _clyde = default!;

    private RenderTargetPool _renderTargetPool = default!;
    private readonly Random _random = new();

    //used for icon GetTexture(), never needs anything but default settings
    private readonly RendererMetaData _defaultRenderMetaData = new();

    public override void Initialize() {
        base.Initialize();
        SubscribeLocalEvent<DreamParticlesComponent, AfterAutoHandleStateEvent>(OnDreamParticlesComponentChange);
        SubscribeLocalEvent<DreamParticlesComponent, ComponentRemove>(HandleComponentRemove);
        _renderTargetPool = new(_clyde);
    }

    private void OnDreamParticlesComponentChange(EntityUid uid, DreamParticlesComponent component, ref AfterAutoHandleStateEvent args) {
        if (_particlesManager.TryGetParticleSystem(uid, out var system))
            system.UpdateSystem(GetParticleSystemArgs(component));
        else
            _particlesManager.CreateParticleSystem(uid, GetParticleSystemArgs(component));
    }

    private void HandleComponentRemove(EntityUid uid, DreamParticlesComponent component, ref ComponentRemove args) {
        _particlesManager.DestroyParticleSystem(uid);
    }

    private ParticleSystemArgs GetParticleSystemArgs(DreamParticlesComponent component) {
        Func<Texture?> textureFunc;
        if (component.TextureList.Length == 0)
            textureFunc = () => Texture.White;
        else {
            List<DreamIcon> icons = new(component.TextureList.Length);
            foreach (var appearance in component.TextureList) {
                DreamIcon icon = new(_renderTargetPool, _dreamInterfaceManager, _gameTiming, _clyde, _appearanceSystem);
                icon.SetAppearance(appearance.MustGetId());
                icons.Add(icon);
            }

            //oh god, so hacky
            textureFunc = () => _random.Pick(icons).GetTexture(null!, null!, _defaultRenderMetaData, null, null);
        }

        var result = new ParticleSystemArgs(textureFunc, new Vector2i(component.Width, component.Height), (uint)component.Count, component.Spawning) {
            Lifespan = GetGeneratorFloat(component.LifespanLow, component.LifespanHigh, component.LifespanDist),
            Fadein = GetGeneratorFloat(component.FadeInLow, component.FadeInHigh, component.FadeInDist),
            Fadeout = GetGeneratorFloat(component.FadeOutLow, component.FadeOutHigh, component.FadeOutDist)
        };

        if (component.Gradient.Length > 0)
            result.Color = lifetime => {
                var colorIndex = (int)(lifetime * component.Gradient.Length);
                colorIndex = Math.Clamp(colorIndex, 0, component.Gradient.Length - 1);
                return component.Gradient[colorIndex];
            };
        else
            result.Color = _ => Color.White;
        result.Acceleration = (_ , velocity) => GetGeneratorVector3(component.AccelerationLow, component.AccelerationHigh, component.AccelerationType, component.AccelerationDist)() + GetGeneratorVector3(component.DriftLow, component.DriftHigh, component.DriftType, component.DriftDist)() - velocity*GetGeneratorVector3(component.FrictionLow, component.FrictionHigh, component.FrictionType, component.FrictionDist)();
        result.SpawnPosition = GetGeneratorVector3(component.SpawnPositionLow, component.SpawnPositionHigh, component.SpawnPositionType, component.SpawnPositionDist);
        result.SpawnVelocity = GetGeneratorVector3(component.SpawnVelocityLow, component.SpawnVelocityHigh, component.SpawnVelocityType, component.SpawnVelocityDist);
        result.Transform = _ => {
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

    private Func<float> GetGeneratorFloat(float low, float high, GeneratorDistribution distribution) {
        switch (distribution) {
            case GeneratorDistribution.Constant:
                return () => high;
            case GeneratorDistribution.Uniform:
                return () => _random.NextFloat(low, high);
            case GeneratorDistribution.Normal:
                return () => (float)Math.Clamp(_random.NextGaussian((low + high) / 2, (high - low) / 6), low, high);
            case GeneratorDistribution.Linear:
                return () => MathF.Sqrt(_random.NextFloat(0, 1)) * (high - low) + low;
            case GeneratorDistribution.Square:
                return () => MathF.Cbrt(_random.NextFloat(0, 1)) * (high - low) + low;
            default:
                throw new NotImplementedException();
        }
    }

    private Func<Vector2> GetGeneratorVector2(Vector2 low, Vector2 high, GeneratorOutputType type, GeneratorDistribution distribution){
        switch (type) {
            case GeneratorOutputType.Num:
                return () => new Vector2(GetGeneratorFloat(low.X, high.X, distribution)(), GetGeneratorFloat(low.Y, high.Y, distribution)());
            case GeneratorOutputType.Vector:
                return () => Vector2.Lerp(low, high, GetGeneratorFloat(0, 1, distribution)());
            case GeneratorOutputType.Box:
                return () => new Vector2(GetGeneratorFloat(low.X, high.X, distribution)(), GetGeneratorFloat(low.Y, high.Y, distribution)());
            case GeneratorOutputType.Circle:
                var theta = _random.NextFloat(0, 360);
                //polar -> cartesian, radius between low and high, angle uniform sample
                return () => new Vector2(MathF.Cos(theta) * GetGeneratorFloat(low.X, high.X, distribution)(), MathF.Sin(theta) * GetGeneratorFloat(low.Y, high.Y, distribution)());
            case GeneratorOutputType.Square:
                return () => {
                    var x = GetGeneratorFloat(-high.X, high.X, distribution)();
                    var y = GetGeneratorFloat(-high.Y, high.Y, distribution)();
                    if (MathF.Abs(x) < low.X)
                        y = _random.NextByte() > 128
                            ? GetGeneratorFloat(-high.Y, -low.Y, distribution)()
                            : GetGeneratorFloat(low.Y, high.Y, distribution)();
                    return new(x, y);
                };
            default:
                throw new NotImplementedException($"Unimplemented generator output type {type}");
        }
    }

    private Func<Vector3> GetGeneratorVector3(Vector3 low, Vector3 high, GeneratorOutputType type, GeneratorDistribution distribution){
        switch (type) {
            case GeneratorOutputType.Num:
                return () => new Vector3(GetGeneratorFloat(low.X, high.X, distribution)(), GetGeneratorFloat(low.Y, high.Y, distribution)(), GetGeneratorFloat(low.Z, high.Z, distribution)());
            case GeneratorOutputType.Vector:
                return () => Vector3.Lerp(low, high, GetGeneratorFloat(0, 1, distribution)());
            case GeneratorOutputType.Box:
                return () => new Vector3(GetGeneratorFloat(low.X, high.X, distribution)(), GetGeneratorFloat(low.Y, high.Y, distribution)(), GetGeneratorFloat(low.Z, high.Z, distribution)());
            case GeneratorOutputType.Sphere:
                var theta = _random.NextFloat(0, 360);
                var phi = _random.NextFloat(0, 180);
                //3d polar -> cartesian, radius between low and high, angle uniform sample
                return () => new Vector3(
                    MathF.Cos(theta) * MathF.Sin(phi) * GetGeneratorFloat(low.X, high.X, distribution)(),
                    MathF.Sin(theta) * MathF.Sin(phi) * GetGeneratorFloat(low.Y, high.Y, distribution)(),
                    MathF.Cos(phi) * GetGeneratorFloat(low.Z, high.Z, distribution)()
                );
            case GeneratorOutputType.Cube:
                return () => {
                    var x = GetGeneratorFloat(-high.X, high.X, distribution)();
                    var y = GetGeneratorFloat(-high.Y, high.Y, distribution)();
                    var z = GetGeneratorFloat(-high.Z, high.Z, distribution)();
                    if (MathF.Abs(x) < low.X)
                        y = _random.NextByte() > 128
                            ? GetGeneratorFloat(-high.Y, -low.Y, distribution)()
                            : GetGeneratorFloat(low.Y, high.Y, distribution)();
                    if (MathF.Abs(y) < low.Y)
                        z = _random.NextByte() > 128
                            ? GetGeneratorFloat(-high.Z, -low.Z, distribution)()
                            : GetGeneratorFloat(low.Z, high.Z, distribution)();
                    return new(x, y, z);
                };
            case GeneratorOutputType.Circle:
            case GeneratorOutputType.Square:
                return () => new Vector3(GetGeneratorVector2(new(low.X, low.Y), new(high.X, high.Y), type, distribution)(), 0);
            default:
                throw new NotImplementedException($"Unimplemented generator output type {type}");
        }
    }
}
