using OpenDreamClient.Resources;
using OpenDreamClient.Resources.ResourceTypes;
using OpenDreamShared.Dream;
using OpenDreamShared.Resources;
using Robust.Client.Graphics;
using Robust.Shared.Timing;

namespace OpenDreamClient.Rendering;

internal sealed class DreamIcon(IGameTiming gameTiming, ClientAppearanceSystem appearanceSystem) {
    public delegate void SizeChangedEventHandler();

    public List<DreamIcon> Overlays { get; } = new();
    public List<DreamIcon> Underlays { get; } = new();
    public event SizeChangedEventHandler? SizeChanged;
    public DMIResource? DMI {
        get => _dmi;
        private set {
            _dmi = value;
            CheckSizeChange();
        }
    }
    private DMIResource? _dmi;

    public int AnimationFrame {
        get {
            UpdateAnimation();
            return _animationFrame;
        }
    }

    [ViewVariables]
    public IconAppearance? Appearance {
        get => CalculateAnimatedAppearance();
        private set {
            _appearance = value;
            UpdateIcon();
        }
    }
    private IconAppearance? _appearance;

    public AtlasTexture? CurrentFrame => (Appearance == null || DMI == null)
        ? null
        : DMI.GetState(Appearance.IconState)?.GetFrames(Appearance.Direction)[AnimationFrame];

    private int _animationFrame;
    private TimeSpan _animationFrameTime = gameTiming.CurTime;
    private AppearanceAnimation? _appearanceAnimation;
    private Box2? _cachedAABB;

    public DreamIcon(IGameTiming gameTiming, ClientAppearanceSystem appearanceSystem, int appearanceId,
        AtomDirection? parentDir = null) : this(gameTiming, appearanceSystem) {
        SetAppearance(appearanceId, parentDir);
    }

    public void SetAppearance(int? appearanceId, AtomDirection? parentDir = null) {
        // End any animations that are currently happening
        // Note that this isn't faithful to the original behavior
        EndAppearanceAnimation();

        if (appearanceId == null) {
            Appearance = null;
            return;
        }

        appearanceSystem.LoadAppearance(appearanceId.Value, appearance => {
            if (parentDir != null && appearance.InheritsDirection) {
                appearance = new IconAppearance(appearance) {
                    Direction = parentDir.Value
                };
            }

            Appearance = appearance;
        });
    }

    public void StartAppearanceAnimation(IconAppearance endingAppearance, TimeSpan duration, AnimationEasing easing) {
        _appearance = CalculateAnimatedAppearance(); //Animation starts from the current animated appearance
        _appearanceAnimation = new AppearanceAnimation(DateTime.Now, duration, endingAppearance, easing);
    }

    public void EndAppearanceAnimation() {
        if (_appearanceAnimation != null)
            _appearance = _appearanceAnimation.Value.EndAppearance;

        _appearanceAnimation = null;
    }

    public void GetWorldAABB(Vector2 worldPos, ref Box2? aabb) {
        if (DMI != null && Appearance != null) {
            Vector2 size = DMI.IconSize / (float)EyeManager.PixelsPerMeter;
            Vector2 pixelOffset = Appearance.PixelOffset / (float)EyeManager.PixelsPerMeter;

            worldPos += pixelOffset;

            Box2 thisAABB = Box2.CenteredAround(worldPos, size);
            aabb = aabb?.Union(thisAABB) ?? thisAABB;
        }

        foreach (DreamIcon underlay in Underlays) {
            underlay.GetWorldAABB(worldPos, ref aabb);
        }

        foreach (DreamIcon overlay in Overlays) {
            overlay.GetWorldAABB(worldPos, ref aabb);
        }
    }

    private void UpdateAnimation() {
        if(DMI == null || Appearance == null)
            return;
        DMIParser.ParsedDMIState? dmiState = DMI.Description.GetStateOrDefault(Appearance.IconState);
        if(dmiState == null)
            return;
        DMIParser.ParsedDMIFrame[] frames = dmiState.GetFrames(Appearance.Direction);

        if (_animationFrame == frames.Length - 1 && !dmiState.Loop) return;

        TimeSpan elapsedTime = gameTiming.CurTime.Subtract(_animationFrameTime);
        while (elapsedTime >= frames[_animationFrame].Delay) {
            elapsedTime -= frames[_animationFrame].Delay;
            _animationFrameTime += frames[_animationFrame].Delay;
            _animationFrame++;

            if (_animationFrame >= frames.Length) _animationFrame -= frames.Length;
        }
    }

    private IconAppearance? CalculateAnimatedAppearance() {
        if (_appearanceAnimation == null || _appearance == null)
            return _appearance;

        AppearanceAnimation animation = _appearanceAnimation.Value;
        IconAppearance appearance = new IconAppearance(_appearance);
        float timeFactor = Math.Clamp((DateTime.Now - animation.Start).Ticks / animation.Duration.Ticks, 0.0f, 1.0f);
        float factor = 0;
        switch (animation.Easing) {
            case AnimationEasing.Linear:
                factor = timeFactor;
                break;
            case AnimationEasing.Sine:
                factor = (float)Math.Sin(timeFactor * MathF.PI / 2);
                break;
            case AnimationEasing.Circular:
                factor = (float)Math.Sqrt(1 - Math.Pow(1 - timeFactor, 2));
                break;
            case AnimationEasing.Cubic:
                factor = (float)(1 - Math.Pow(1-timeFactor, 3));
                break;
            case AnimationEasing.Bounce:
                float bounce = timeFactor*2.75f;
                if(bounce<1)
                    factor = bounce*bounce;
                else if(bounce<2) {
                    bounce -= 1.5f;
                    factor = bounce*bounce + 0.75f;
                } else if(bounce<2.5) {
                    bounce -= 2.25f;
                    factor = bounce*bounce + 0.9375f;
                } else {
                    bounce -= 2.625f;
                    factor = bounce*bounce + 0.984375f;
                }
                break;
            case AnimationEasing.Elastic:
                factor = (float)(1.0 - Math.Pow(2, -10 * timeFactor) * Math.Cos(timeFactor*Math.PI/0.15));
                break;
            case AnimationEasing.Back:
                factor = (float)(1 - Math.Pow(1 - timeFactor, 2)*((1.70158+1)*(1-timeFactor) - 1.70158));
                break;
            case AnimationEasing.Quad:
                factor = (float) (1 - Math.Pow(1-timeFactor,2));
                break;
            case AnimationEasing.Jump:
                factor = (timeFactor < 1) ? 0 : 1;
                break;
        }
        IconAppearance endAppearance = animation.EndAppearance;

        if (endAppearance.PixelOffset != _appearance.PixelOffset) {
            Vector2 startingOffset = appearance.PixelOffset;
            Vector2 newPixelOffset = Vector2.Lerp(startingOffset, endAppearance.PixelOffset, factor);

            appearance.PixelOffset = (Vector2i)newPixelOffset;
        }

        //non-smooth animations
        /*
        dir
        icon
        icon_state
        invisibility
        maptext
        suffix
        */

        if (endAppearance.Direction != _appearance.Direction) {
            appearance.Direction = endAppearance.Direction;
        }
        if (endAppearance.Icon != _appearance.Icon) {
            appearance.Icon = endAppearance.Icon;
        }
        if (endAppearance.IconState != _appearance.IconState) {
            appearance.IconState = endAppearance.IconState;
        }
        if (endAppearance.Invisibility != _appearance.Invisibility) {
            appearance.Invisibility = endAppearance.Invisibility;
        }
        /* TODO maptext
        if (endAppearance.MapText != _appearance.MapText) {
            appearance.MapText = endAppearance.MapText;
        }
        */
        /* TODO suffix
        if (endAppearance.Suffix != _appearance.Suffix) {
            appearance.Suffix = endAppearance.Suffix;
        }
        */

        //smooth animation properties
        /*
        alpha
        color
        glide_size
        infra_luminosity
        layer
        maptext_width, maptext_height, maptext_x, maptext_y
        luminosity
        pixel_x, pixel_y, pixel_w, pixel_z
        transform
        */


        if (endAppearance.Alpha != _appearance.Alpha) {
            appearance.Alpha = (byte)Math.Clamp(((1-factor) * _appearance.Alpha) + (factor * endAppearance.Alpha), 0, 255);
        }

        // TODO: Other animatable properties

        if (factor >= 1f) {
            EndAppearanceAnimation();
        }

        return appearance;
    }

    private void UpdateIcon() {
        if (Appearance == null) {
            DMI = null;
            return;
        }

        if (Appearance.Icon == null) {
            DMI = null;
        } else {
            IoCManager.Resolve<IDreamResourceManager>().LoadResourceAsync<DMIResource>(Appearance.Icon.Value, dmi => {
                if (dmi.Id != Appearance.Icon) return; //Icon changed while resource was loading

                DMI = dmi;
                _animationFrame = 0;
                _animationFrameTime = gameTiming.CurTime;
            });
        }

        Overlays.Clear();
        foreach (int overlayId in Appearance.Overlays) {
            DreamIcon overlay = new DreamIcon(gameTiming, appearanceSystem, overlayId, Appearance.Direction);
            overlay.SizeChanged += CheckSizeChange;

            Overlays.Add(overlay);
        }

        Underlays.Clear();
        foreach (int underlayId in Appearance.Underlays) {
            DreamIcon underlay = new DreamIcon(gameTiming, appearanceSystem, underlayId, Appearance.Direction);
            underlay.SizeChanged += CheckSizeChange;

            Underlays.Add(underlay);
        }
    }

    private void CheckSizeChange() {
        Box2? aabb = null;
        GetWorldAABB(Vector2.Zero, ref aabb);

        if (aabb != _cachedAABB) {
            _cachedAABB = aabb;
            SizeChanged?.Invoke();
        }
    }

    private struct AppearanceAnimation(DateTime start, TimeSpan duration, IconAppearance endAppearance, AnimationEasing easing) {
        public readonly DateTime Start = start;
        public readonly TimeSpan Duration = duration;
        public readonly IconAppearance EndAppearance = endAppearance;
        public readonly AnimationEasing Easing = easing;
    }
}
