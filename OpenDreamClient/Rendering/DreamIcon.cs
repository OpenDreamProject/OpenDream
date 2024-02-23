using OpenDreamClient.Resources;
using OpenDreamClient.Resources.ResourceTypes;
using OpenDreamShared.Dream;
using OpenDreamShared.Resources;
using Robust.Client.Graphics;
using Robust.Shared.Timing;
using System.Linq;

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
    private List<AppearanceAnimation>? _appearanceAnimations;
    private Box2? _cachedAABB;

    public DreamIcon(IGameTiming gameTiming, ClientAppearanceSystem appearanceSystem, int appearanceId,
        AtomDirection? parentDir = null) : this(gameTiming, appearanceSystem) {
        SetAppearance(appearanceId, parentDir);
    }

    public void SetAppearance(int? appearanceId, AtomDirection? parentDir = null) {
        // End any animations that are currently happening
        // Note that this isn't faithful to the original behavior
        EndAppearanceAnimation(null);

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

    //three things to do here, chained animations, loops and parallel animations
    public void StartAppearanceAnimation(IconAppearance endingAppearance, TimeSpan duration, AnimationEasing easing, int loops, AnimationFlags flags, int delay, bool chainAnim) {
        _appearance = CalculateAnimatedAppearance(); //Animation starts from the current animated appearance
        DateTime start = DateTime.Now;
        if(!chainAnim)
            EndAppearanceAnimation(null);
        else
            if(_appearanceAnimations != null && _appearanceAnimations.Count > 0)
                if((flags & AnimationFlags.ANIMATION_PARALLEL) != 0)
                    start = _appearanceAnimations[^1].Start; //either that's also a parallel, or its one that this should be parallel with
                else
                    start = _appearanceAnimations[^1].Start + _appearanceAnimations[^1].Duration; //if it's not parallel, it's chained

        _appearanceAnimations ??= new List<AppearanceAnimation>();
        _appearanceAnimations.Add(new AppearanceAnimation(start, duration, endingAppearance, easing, loops, flags, delay));
    }

    /// <summary>
    /// Ends the target appearance animation. If appearanceAnimation is null, ends all animations.
    /// </summary>
    /// <param name="appearanceAnimation"></param>
    private void EndAppearanceAnimation(AppearanceAnimation? appearanceAnimation) {
        if (appearanceAnimation == null){
            if(_appearanceAnimations != null && _appearanceAnimations.Count > 0) {
                Appearance = _appearanceAnimations[^1].EndAppearance;
                _appearanceAnimations.Clear();
            }
            return;
        }
        if (_appearanceAnimations != null && _appearanceAnimations.Contains(appearanceAnimation!.Value)) {
            _appearance = appearanceAnimation!.Value.EndAppearance;
            _appearanceAnimations.Remove(appearanceAnimation!.Value);
        }
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
        if (_appearanceAnimations == null || _appearance == null)
            return _appearance;
        IconAppearance appearance = new IconAppearance(_appearance);
        List<AppearanceAnimation>? toRemove = null;
        for(int i = 0; i < _appearanceAnimations.Count; i++) {
            AppearanceAnimation animation = _appearanceAnimations[i];
            //if it's not the first one, and it's not parallel, break
            if((animation.flags & AnimationFlags.ANIMATION_PARALLEL) == 0 && i != 0)
                break;


            float timeFactor = Math.Clamp((float)(DateTime.Now - animation.Start).Ticks / animation.Duration.Ticks, 0.0f, 1.0f);
            float factor = 0;
            if((animation.Easing & AnimationEasing.Ease_In) != 0)
                timeFactor = 0.5f+timeFactor/2.0f;
            if((animation.Easing & AnimationEasing.Ease_Out) != 0)
                timeFactor = timeFactor/2.0f;

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
                case AnimationEasing.Bounce: //https://stackoverflow.com/questions/25249829/bouncing-ease-equation-in-c-sharp great match for byond behaviour
                    float bounce = timeFactor*2.75f;
                    if(bounce<1)
                        factor = (float)Math.Pow(bounce, 2);
                    else if(bounce<2) {
                        bounce -= 1.5f;
                        factor = (float)Math.Pow(bounce, 2)+ 0.75f;
                    } else if(bounce<2.5) {
                        bounce -= 2.25f;
                        factor = (float)Math.Pow(bounce, 2) + 0.9375f;
                    } else {
                        bounce -= 2.625f;
                        factor = (float)Math.Pow(bounce, 2) + 0.984375f;
                    }
                    break;
                case AnimationEasing.Elastic: //http://www.java2s.com/example/csharp/system/easing-equation-function-for-an-elastic-exponentially-decaying-sine-w.html with d=1, s=pi/2, c=2, b = -1
                    factor = (float) (Math.Pow(2, -10 * timeFactor) * Math.Sin((timeFactor - MathF.PI/2) * (2*Math.PI/0.3)) + 1);
                    break;
                case AnimationEasing.Back: //https://learn.microsoft.com/en-us/dotnet/api/system.windows.media.animation.backease?view=windowsdesktop-8.0
                    factor = (float)Math.Pow(timeFactor, 3) - timeFactor * MathF.Sin(timeFactor * MathF.PI);
                    break;
                case AnimationEasing.Quad:
                    factor = (float) (1 - Math.Pow(1-timeFactor,2));
                    break;
                case AnimationEasing.Jump:
                    factor = (timeFactor < 1) ? 0 : 1;
                    break;
            }

            IconAppearance endAppearance = animation.EndAppearance;

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

            if (endAppearance.Color != _appearance.Color) {
                appearance.Color = Color.FromSrgb(new Color(
                    Math.Clamp(((1-factor) * _appearance.Color.R) + (factor * endAppearance.Color.R), 0, 1),
                    Math.Clamp(((1-factor) * _appearance.Color.G) + (factor * endAppearance.Color.G), 0, 1),
                    Math.Clamp(((1-factor) * _appearance.Color.B) + (factor * endAppearance.Color.B), 0, 1),
                    Math.Clamp(((1-factor) * _appearance.Color.A) + (factor * endAppearance.Color.A), 0, 1)
                ));
            }

            if (!endAppearance.ColorMatrix.Equals(_appearance.ColorMatrix)){
                appearance.ColorMatrix = new ColorMatrix(
                    ((1-factor) * _appearance.ColorMatrix.c11) + (factor * endAppearance.ColorMatrix.c11),
                    ((1-factor) * _appearance.ColorMatrix.c12) + (factor * endAppearance.ColorMatrix.c12),
                    ((1-factor) * _appearance.ColorMatrix.c13) + (factor * endAppearance.ColorMatrix.c13),
                    ((1-factor) * _appearance.ColorMatrix.c14) + (factor * endAppearance.ColorMatrix.c14),
                    ((1-factor) * _appearance.ColorMatrix.c21) + (factor * endAppearance.ColorMatrix.c21),
                    ((1-factor) * _appearance.ColorMatrix.c22) + (factor * endAppearance.ColorMatrix.c22),
                    ((1-factor) * _appearance.ColorMatrix.c23) + (factor * endAppearance.ColorMatrix.c23),
                    ((1-factor) * _appearance.ColorMatrix.c24) + (factor * endAppearance.ColorMatrix.c24),
                    ((1-factor) * _appearance.ColorMatrix.c31) + (factor * endAppearance.ColorMatrix.c31),
                    ((1-factor) * _appearance.ColorMatrix.c32) + (factor * endAppearance.ColorMatrix.c32),
                    ((1-factor) * _appearance.ColorMatrix.c33) + (factor * endAppearance.ColorMatrix.c33),
                    ((1-factor) * _appearance.ColorMatrix.c34) + (factor * endAppearance.ColorMatrix.c34),
                    ((1-factor) * _appearance.ColorMatrix.c41) + (factor * endAppearance.ColorMatrix.c41),
                    ((1-factor) * _appearance.ColorMatrix.c42) + (factor * endAppearance.ColorMatrix.c42),
                    ((1-factor) * _appearance.ColorMatrix.c43) + (factor * endAppearance.ColorMatrix.c43),
                    ((1-factor) * _appearance.ColorMatrix.c44) + (factor * endAppearance.ColorMatrix.c44),
                    ((1-factor) * _appearance.ColorMatrix.c51) + (factor * endAppearance.ColorMatrix.c51),
                    ((1-factor) * _appearance.ColorMatrix.c52) + (factor * endAppearance.ColorMatrix.c52),
                    ((1-factor) * _appearance.ColorMatrix.c53) + (factor * endAppearance.ColorMatrix.c53),
                    ((1-factor) * _appearance.ColorMatrix.c54) + (factor * endAppearance.ColorMatrix.c54)
                );
            }


            if (endAppearance.GlideSize != _appearance.GlideSize) {
                appearance.GlideSize = ((1-factor) * _appearance.GlideSize) + (factor * endAppearance.GlideSize);
            }

            /* TODO infraluminosity
            if (endAppearance.InfraLuminosity != _appearance.InfraLuminosity) {
                appearance.InfraLuminosity = ((1-factor) * _appearance.InfraLuminosity) + (factor * endAppearance.InfraLuminosity);
            }
            */

            if (endAppearance.Layer != _appearance.Layer) {
                appearance.Layer = ((1-factor) * _appearance.Layer) + (factor * endAppearance.Layer);
            }

            /* TODO luminosity
            if (endAppearance.Luminosity != _appearance.Luminosity) {
                appearance.Luminosity = ((1-factor) * _appearance.Luminosity) + (factor * endAppearance.Luminosity);
            }
            */

            /* TODO maptext
            if (endAppearance.MapTextWidth != _appearance.MapTextWidth) {
                appearance.MapTextWidth = (ushort)Math.Clamp(((1-factor) * _appearance.MapTextWidth) + (factor * endAppearance.MapTextWidth), 0, 65535);
            }

            if (endAppearance.MapTextHeight != _appearance.MapTextHeight) {
                appearance.MapTextHeight = (ushort)Math.Clamp(((1-factor) * _appearance.MapTextHeight) + (factor * endAppearance.MapTextHeight), 0, 65535);
            }

            if (endAppearance.MapTextX != _appearance.MapTextX) {
                appearance.MapTextX = (short)Math.Clamp(((1-factor) * _appearance.MapTextX) + (factor * endAppearance.MapTextX), -32768, 32767);
            }

            if (endAppearance.MapTextY != _appearance.MapTextY) {
                appearance.MapTextY = (short)Math.Clamp(((1-factor) * _appearance.MapTextY) + (factor * endAppearance.MapTextY), -32768, 32767);
            }
            */

            if (endAppearance.PixelOffset != _appearance.PixelOffset) {
                Vector2 startingOffset = appearance.PixelOffset;
                Vector2 newPixelOffset = Vector2.Lerp(startingOffset, endAppearance.PixelOffset, factor);

                appearance.PixelOffset = (Vector2i)newPixelOffset;
            }

            if (!endAppearance.Transform.SequenceEqual(_appearance.Transform)) {
                appearance.Transform[0] = (1.0f-factor)*_appearance.Transform[0] + (factor * endAppearance.Transform[0]);
                appearance.Transform[1] = (1.0f-factor)*_appearance.Transform[1] + (factor * endAppearance.Transform[1]);
                appearance.Transform[2] = (1.0f-factor)*_appearance.Transform[2] + (factor * endAppearance.Transform[2]);
                appearance.Transform[3] = (1.0f-factor)*_appearance.Transform[3] + (factor * endAppearance.Transform[3]);
                appearance.Transform[4] = (1.0f-factor)*_appearance.Transform[4] + (factor * endAppearance.Transform[4]);
                appearance.Transform[5] = (1.0f-factor)*_appearance.Transform[5] + (factor * endAppearance.Transform[5]);
            }

            if (timeFactor >= 1f) {
                if (animation.loops > 0)
                {
                    var tempAnimation = _appearanceAnimations[i];
                    tempAnimation.loops--;
                    _appearanceAnimations[i] = tempAnimation;
                }
                if (animation.loops == 0) {
                    toRemove ??= new();
                    toRemove!.Add(animation);
                }
            }
        }
        if(toRemove != null)
            foreach (AppearanceAnimation animation in toRemove!) {
                EndAppearanceAnimation(animation);
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

    private struct AppearanceAnimation(DateTime start, TimeSpan duration, IconAppearance endAppearance, AnimationEasing easing, int loops, AnimationFlags flags, int delay) {
        public readonly DateTime Start = start;
        public readonly TimeSpan Duration = duration;
        public readonly IconAppearance EndAppearance = endAppearance;
        public readonly AnimationEasing Easing = easing;
        public int loops = loops;
        public readonly AnimationFlags flags = flags;
        public int delay = delay;

    }
}
