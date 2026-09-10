using JetBrains.Annotations;
using OpenDreamClient.Interface;
using OpenDreamShared.Rendering;
using OpenDreamClient.Rendering.Particles;
using Robust.Client.Graphics;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace OpenDreamClient.Rendering;

[UsedImplicitly]
public sealed partial class ClientDreamParticlesSystem : SharedDreamParticlesSystem {
    [Dependency] private ParticlesManager _particlesManager = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private ClientAppearanceSystem _appearanceSystem = default!;
    [Dependency] private IDreamInterfaceManager _dreamInterfaceManager = default!;
    [Dependency] private IClyde _clyde = default!;
    [Dependency] private IRobustRandom _random = default!;

    private RenderTargetPool _renderTargetPool = default!;

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

    private ParticleSystemArgs GetParticleSystemArgs(DreamParticlesComponent comp) {
        DreamParticlesComponent.ParticleData data = comp.Data;
        Func<Texture?> textureFunc;
        if (data.TextureList.Length == 0)
            textureFunc = () => Texture.White;
        else {
            List<DreamIcon> icons = new(data.TextureList.Length);
            foreach (var appearance in data.TextureList) {
                DreamIcon icon = new(_renderTargetPool, _dreamInterfaceManager, _gameTiming, _clyde, _appearanceSystem);
                icon.SetAppearance(appearance.MustGetId());
                icons.Add(icon);
            }

            //oh god, so hacky
            textureFunc = () => _random.Pick(icons).GetTexture(null!, null!, _defaultRenderMetaData, null, null);
        }

        var perTick = (1f / 10f); // "Tick" refers to a BYOND standard tick of 0.1s. --DM Reference
        var result = new ParticleSystemArgs(textureFunc, new Vector2i(data.Width, data.Height), (uint)data.Count, data.Spawning / perTick) {
            Lifespan = () => (data.Lifespan?.Generate(_random) ?? 1f) * perTick,
            Fadein = () => (data.FadeIn?.Generate(_random) ?? 0f) * perTick,
            Fadeout = () => (data.FadeOut?.Generate(_random) ?? 0f) * perTick,
            Color = data.Gradient.Length > 0
                ? lifetime => {
                    var colorIndex = (int)(lifetime * data.Gradient.Length);
                    colorIndex = Math.Clamp(colorIndex, 0, data.Gradient.Length - 1);
                    return data.Gradient[colorIndex];
                }
                : _ => Color.White,
            Acceleration = (_, velocity) => { // TODO: Acceleration needs to only update every tick
                var drift = (data.Drift?.GenerateVector3(_random) ?? Vector3.Zero);
                var friction = (data.Friction?.GenerateVector3(_random) ?? Vector3.Zero); // TODO: Only calculated once per particle

                return drift - (velocity * friction);
            },
            SpawnPosition = () => data.SpawnPosition?.GenerateVector3(_random) ?? Vector3.Zero,
            SpawnVelocity = () => data.SpawnVelocity?.GenerateVector3(_random) ?? Vector3.Zero,
            Transform = _ => { // TODO: Needs to only be performed every tick
                var scale = data.Scale.GenerateVector2(_random);
                var rotation = data.Rotation?.Generate(_random) ?? 0f;
                var growth = data.Growth?.GenerateVector2(_random) ?? Vector2.Zero;
                var spin = data.Spin?.Generate(_random) ?? 0f;
                return Matrix3x2.CreateScale(scale.X + growth.X, scale.Y + growth.Y) *
                       Matrix3x2.CreateRotation(rotation + spin);
            },
            BaseTransform = Matrix3x2.Identity
        };

        return result;
    }
}
