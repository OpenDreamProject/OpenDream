using System.Diagnostics.CodeAnalysis;
using OpenDreamClient.Resources;
using OpenDreamClient.Resources.ResourceTypes;
using OpenDreamShared.Dream;
using OpenDreamShared.Resources;
using Robust.Client.Graphics;
using Robust.Shared.Timing;
using System.Linq;
using OpenDreamClient.Interface;

namespace OpenDreamClient.Rendering;

internal sealed class DreamIcon(RenderTargetPool renderTargetPool, IDreamInterfaceManager interfaceManager, IGameTiming gameTiming, IClyde clyde, ClientAppearanceSystem appearanceSystem) : IDisposable {
    public delegate void SizeChangedEventHandler();

    public List<DreamIcon> Overlays { get; } = new();
    public List<DreamIcon> Underlays { get; } = new();
    public event SizeChangedEventHandler? SizeChanged;

    public DMIResource? DMI {
        get => _dmi;
        private set {
            _dmi?.OnUpdateCallbacks.Remove(DirtyTexture);
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
    public ImmutableAppearance? Appearance {
        get => CalculateAnimatedAppearance();
        private set {
            if (_appearance?.Equals(value) is true)
                return;

            _appearance = value;
            UpdateIcon();
        }
    }

    private ImmutableAppearance? _appearance;

    //acts as a cache for the mutable appearance, so we don't have to ToMutable() every frame
    private MutableAppearance? _animatedAppearance;
    private AtomDirection _direction;
    private string? _iconState;

    // TODO: We could cache these per-appearance instead of per-atom
    public IRenderTexture? CachedTexture {
        get => _cachedTexture;
        private set {
            if (_cachedTexture != null)
                renderTargetPool.Return(_cachedTexture);
            _cachedTexture = value;
        }
    }

    public Vector2 TextureRenderOffset = Vector2.Zero;
    public Texture? LastRenderedTexture;

    private int _animationFrame;
    private List<AppearanceAnimation>? _appearanceAnimations;
    private int _appearanceAnimationsLoops;
    private Box2? _cachedAABB;
    private bool _textureDirty = true;
    private bool _animationComplete;
    private IRenderTexture? _cachedTexture;

    public DreamIcon(RenderTargetPool renderTargetPool, IDreamInterfaceManager interfaceManager, IGameTiming gameTiming, IClyde clyde, ClientAppearanceSystem appearanceSystem, uint appearanceId,
        AtomDirection? parentDir = null, string? parentIconState = null) : this(renderTargetPool, interfaceManager, gameTiming, clyde, appearanceSystem) {
        SetAppearance(appearanceId, parentDir, parentIconState);
    }

    public void Dispose() {
        CachedTexture = null;
        LastRenderedTexture = null;
        DMI = null; //triggers the removal of the onUpdateCallback
    }

    public Texture? GetTexture(DreamViewOverlay viewOverlay, DrawingHandleWorld handle, RendererMetaData iconMetaData, Texture? textureOverride, ClientAppearanceSystem.Flick? flick) {
        Texture? frame;

        if (textureOverride == null) {
            if (Appearance == null || DMI == null)
                return null;

            var dmi = flick?.Icon ?? DMI;
            var iconState = flick?.IconState ?? _iconState;
            var animationFrame = flick?.GetAnimationFrame(gameTiming) ?? AnimationFrame;
            if (animationFrame == -1) // A flick returns -1 for a finished animation
                animationFrame = AnimationFrame;
            if (CachedTexture != null && !_textureDirty && flick == null)
                return CachedTexture.Texture;

            _textureDirty = false;
            frame = dmi.GetState(iconState)?.GetFrames(_direction)[animationFrame];
        } else {
            frame = textureOverride;
        }

        var canSkipFullRender = Appearance?.Filters.Length is 0 or null &&
                                    iconMetaData.ColorMatrixToApply.Equals(ColorMatrix.Identity) &&
                                    iconMetaData.AlphaToApply.Equals(1.0f);

        if (frame == null) {
            CachedTexture = null;
        } else if (canSkipFullRender) {
            TextureRenderOffset = Vector2.Zero;
            return frame;
        } else {
            if (textureOverride is not null) { //no caching in the presence of overrides
                var texture = FullRenderTexture(viewOverlay, handle, iconMetaData, frame);

                renderTargetPool.ReturnAtEndOfFrame(texture);
                return texture.Texture;
            }

            CachedTexture = FullRenderTexture(viewOverlay, handle, iconMetaData, frame);
        }

        return CachedTexture?.Texture;
    }

    public void SetAppearance(uint? appearanceId, AtomDirection? parentDir = null, string? parentIconState = null) {
        // End any animations that are currently happening
        // Note that this isn't faithful to the original behavior
        EndAppearanceAnimation(null);

        if (appearanceId == null) {
            Appearance = null;
            return;
        }

        appearanceSystem.LoadAppearance(appearanceId.Value, appearance => {
            if (parentDir != null && appearance.InheritsDirection) {
                _direction = parentDir.Value;
            } else {
                _direction = appearance.Direction;
            }
            if (parentIconState != null && appearance.IconState == null) {
                _iconState = parentIconState;
            } else {
                _iconState = appearance.IconState;
            }

            Appearance = appearance;
        });
    }

    //three things to do here, chained animations, loops and parallel animations
    public void StartAppearanceAnimation(ImmutableAppearance endingAppearance, TimeSpan duration, AnimationEasing easing, int loops, AnimationFlags flags, int delay, bool chainAnim) {
        _appearance = CalculateAnimatedAppearance(); //Animation starts from the current animated appearance
        DateTime start = DateTime.Now;
        if(!chainAnim)
            EndAppearanceAnimation(null);
        else
            if(_appearanceAnimations != null && _appearanceAnimations.Count > 0)
                if((flags & AnimationFlags.AnimationParallel) != 0)
                    start = _appearanceAnimations[^1].Start; //either that's also a parallel, or its one that this should be parallel with
                else
                    start = _appearanceAnimations[^1].Start + _appearanceAnimations[^1].Duration; //if it's not parallel, it's chained

        _appearanceAnimations ??= new List<AppearanceAnimation>();
        if(_appearanceAnimations.Count == 0) {//only valid on the first animation
            _appearanceAnimationsLoops = loops;
        }

        for(int i=_appearanceAnimations.Count-1; i>=0; i--) //there can be only one last-in-sequence, and it might not be the last element of the list because it could be added to mid-loop
            if(_appearanceAnimations[i].LastInSequence) {
                var lastAnim =  _appearanceAnimations[i];
                lastAnim.LastInSequence = false;
                _appearanceAnimations[i] = lastAnim;
                break;
            }

        _appearanceAnimations.Add(new AppearanceAnimation(start, duration, endingAppearance, easing, flags, delay, true));
    }

    /// <summary>
    /// Ends the target appearance animation. If appearanceAnimation is null, ends all animations.
    /// </summary>
    /// <param name="appearanceAnimation">Animation to end</param>
    private void EndAppearanceAnimation(AppearanceAnimation? appearanceAnimation) {
        if (appearanceAnimation == null) {
            if (_appearanceAnimations?.Count > 0) {
                Appearance = _appearanceAnimations[^1].EndAppearance;
                _appearanceAnimations.Clear();
            }

            return;
        }

        if (_appearanceAnimations != null && _appearanceAnimations.Contains(appearanceAnimation.Value)) {
            _appearance = appearanceAnimation.Value.EndAppearance;
            _appearanceAnimations.Remove(appearanceAnimation.Value);
        }
    }

    public void GetWorldAABB(Vector2 worldPos, ref Box2? aabb) {
        if (DMI != null && Appearance != null) {
            Vector2 size = DMI.IconSize / (float)interfaceManager.IconSize;
            Vector2 pixelOffset = Appearance.TotalPixelOffset / (float)interfaceManager.IconSize;

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
        if(DMI == null || Appearance == null || _animationComplete)
            return;

        DMIParser.ParsedDMIState? dmiState = DMI.Description.GetStateOrDefault(_iconState);
        if(dmiState == null)
            return;
        DMIParser.ParsedDMIFrame[] frames = dmiState.GetFrames(_direction);

        if (frames.Length <= 1) return;

        var oldFrame = _animationFrame;
        var currentGameTicks = gameTiming.CurTime.Ticks;
        var sequenceDuration = frames.Aggregate(TimeSpan.Zero, (duration, frame) => duration + frame.Delay);
        var durationDiff = new TimeSpan(currentGameTicks % sequenceDuration.Ticks);
        var noLoop = !dmiState.Loop;

        _animationFrame = 0;
        while (durationDiff >= frames[_animationFrame].Delay) {
            durationDiff -= frames[_animationFrame].Delay;

            _animationFrame++;

            if (noLoop && _animationFrame == frames.Length - 1) {
                _animationComplete = true;
                break;
            } else if (_animationFrame == frames.Length)
                _animationFrame = 0;
        }

        if (oldFrame != _animationFrame)
            DirtyTexture();
    }

    private ImmutableAppearance? CalculateAnimatedAppearance() {
        if (_appearanceAnimations == null || _appearanceAnimations.Count == 0 || _appearance == null) {
            _animatedAppearance = null; //null it if _appearanceAnimations is empty
            return _appearance;
        }

        _textureDirty = true; //if we have animations, we need to recalculate the texture
        _animatedAppearance = _appearance.ToMutable();
        List<AppearanceAnimation>? toRemove = null;
        List<AppearanceAnimation>? toReAdd = null;
        for(int i = 0; i < _appearanceAnimations.Count; i++) {
            AppearanceAnimation animation = _appearanceAnimations[i];
            //if it's not the first one, and it's not parallel, break
            if((animation.Flags & AnimationFlags.AnimationParallel) == 0 && i != 0)
                break;

            float timeFactor = Math.Clamp((float)(DateTime.Now - animation.Start).Ticks / animation.Duration.Ticks, 0.0f, 1.0f);
            float factor = 0;
            if((animation.Easing & AnimationEasing.EaseIn) != 0)
                timeFactor /= 2.0f;
            if((animation.Easing & AnimationEasing.EaseOut) != 0)
                timeFactor = 0.5f+timeFactor/2.0f;

            switch (animation.Easing) {
                case AnimationEasing.Linear:
                    factor = timeFactor;
                    break;
                case AnimationEasing.Sine:
                    factor = MathF.Sin(timeFactor * MathF.PI / 2);
                    break;
                case AnimationEasing.Circular:
                    factor = MathF.Sqrt(1 - MathF.Pow(1 - timeFactor, 2));
                    break;
                case AnimationEasing.Cubic:
                    factor = 1 - MathF.Pow(1-timeFactor, 3);
                    break;
                case AnimationEasing.Bounce: //https://stackoverflow.com/questions/25249829/bouncing-ease-equation-in-c-sharp great match for byond behaviour
                    float bounce = timeFactor*2.75f;
                    if(bounce<1)
                        factor = MathF.Pow(bounce, 2);
                    else if(bounce<2) {
                        bounce -= 1.5f;
                        factor = MathF.Pow(bounce, 2)+ 0.75f;
                    } else if(bounce<2.5) {
                        bounce -= 2.25f;
                        factor = MathF.Pow(bounce, 2) + 0.9375f;
                    } else {
                        bounce -= 2.625f;
                        factor = MathF.Pow(bounce, 2) + 0.984375f;
                    }

                    break;
                case AnimationEasing.Elastic: //http://www.java2s.com/example/csharp/system/easing-equation-function-for-an-elastic-exponentially-decaying-sine-w.html with d=1, s=pi/2, c=2, b = -1
                    factor = MathF.Pow(2, -10 * timeFactor) * MathF.Sin((timeFactor - MathF.PI/2.0f) * (2.0f*MathF.PI/0.3f)) + 1.0f;
                    break;
                case AnimationEasing.Back: //https://learn.microsoft.com/en-us/dotnet/api/system.windows.media.animation.backease?view=windowsdesktop-8.0
                    factor = MathF.Pow(timeFactor, 3) - timeFactor * MathF.Sin(timeFactor * MathF.PI);
                    break;
                case AnimationEasing.Quad:
                    factor = 1 - MathF.Pow(1-timeFactor,2);
                    break;
                case AnimationEasing.Jump:
                    factor = (timeFactor < 1) ? 0 : 1;
                    break;
            }

            var endAppearance = animation.EndAppearance;

            //non-smooth animations
            /*
            dir
            icon
            icon_state
            invisibility
            maptext
            suffix
            */

            if (endAppearance.Direction != _appearance.Direction)
                _animatedAppearance.Direction = endAppearance.Direction;
            if (endAppearance.Icon != _appearance.Icon)
                _animatedAppearance.Icon = endAppearance.Icon;
            if (endAppearance.IconState != _appearance.IconState)
                _animatedAppearance.IconState = endAppearance.IconState;
            if (endAppearance.Invisibility != _appearance.Invisibility)
                _animatedAppearance.Invisibility = endAppearance.Invisibility;
            if (endAppearance.Maptext != _appearance.Maptext)
                _animatedAppearance.Maptext = endAppearance.Maptext;

            /* TODO suffix
            if (endAppearance.Suffix != _appearance.Suffix)
                appearance.Suffix = endAppearance.Suffix;
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
                _animatedAppearance.Alpha = (byte)Math.Clamp(((1-factor) * _appearance.Alpha) + (factor * endAppearance.Alpha), 0, 255);
            }

            if (endAppearance.Color != _appearance.Color) {
                _animatedAppearance.Color = Color.FromSrgb(new Color(
                    Math.Clamp(((1-factor) * _appearance.Color.R) + (factor * endAppearance.Color.R), 0, 1),
                    Math.Clamp(((1-factor) * _appearance.Color.G) + (factor * endAppearance.Color.G), 0, 1),
                    Math.Clamp(((1-factor) * _appearance.Color.B) + (factor * endAppearance.Color.B), 0, 1),
                    Math.Clamp(((1-factor) * _appearance.Color.A) + (factor * endAppearance.Color.A), 0, 1)
                ));
            }

            if (!endAppearance.ColorMatrix.Equals(_appearance.ColorMatrix)){
                ColorMatrix.Interpolate(in _appearance.ColorMatrix, in endAppearance.ColorMatrix, factor, out _animatedAppearance.ColorMatrix);
            }

            if (!endAppearance.GlideSize.Equals(_appearance.GlideSize)) {
                _animatedAppearance.GlideSize = ((1-factor) * _appearance.GlideSize) + (factor * endAppearance.GlideSize);
            }

            /* TODO infraluminosity
            if (endAppearance.InfraLuminosity != _appearance.InfraLuminosity) {
                appearance.InfraLuminosity = ((1-factor) * _appearance.InfraLuminosity) + (factor * endAppearance.InfraLuminosity);
            }
            */

            if (!endAppearance.Layer.Equals(_appearance.Layer)) {
                _animatedAppearance.Layer = ((1-factor) * _appearance.Layer) + (factor * endAppearance.Layer);
            }

            /* TODO luminosity
            if (endAppearance.Luminosity != _appearance.Luminosity) {
                appearance.Luminosity = ((1-factor) * _appearance.Luminosity) + (factor * endAppearance.Luminosity);
            }
            */

            if (endAppearance.MaptextSize != _appearance.MaptextSize) {
                Vector2 startingOffset = _appearance.MaptextSize;
                Vector2 newMaptextSize = Vector2.Lerp(startingOffset, endAppearance.MaptextSize, factor);

                _animatedAppearance.MaptextSize = (Vector2i)newMaptextSize;
            }

            if (endAppearance.MaptextOffset != _appearance.MaptextOffset) {
                Vector2 startingOffset = _appearance.MaptextOffset;
                Vector2 newMaptextOffset = Vector2.Lerp(startingOffset, endAppearance.MaptextOffset, factor);

                _animatedAppearance.MaptextOffset = (Vector2i)newMaptextOffset;
            }

            if (endAppearance.PixelOffset != _appearance.PixelOffset) {
                Vector2 startingOffset = _appearance.PixelOffset;
                Vector2 newPixelOffset = Vector2.Lerp(startingOffset, endAppearance.PixelOffset, factor);

                _animatedAppearance.PixelOffset = (Vector2i)newPixelOffset;
            }

            if (endAppearance.PixelOffset2 != _appearance.PixelOffset2) {
                Vector2 startingOffset = _appearance.PixelOffset2;
                Vector2 newPixelOffset = Vector2.Lerp(startingOffset, endAppearance.PixelOffset2, factor);

                _animatedAppearance.PixelOffset2 = (Vector2i)newPixelOffset;
            }

            if (!endAppearance.Transform.SequenceEqual(_appearance.Transform)) {
                _animatedAppearance.Transform[0] = (1.0f-factor)*_appearance.Transform[0] + (factor * endAppearance.Transform[0]);
                _animatedAppearance.Transform[1] = (1.0f-factor)*_appearance.Transform[1] + (factor * endAppearance.Transform[1]);
                _animatedAppearance.Transform[2] = (1.0f-factor)*_appearance.Transform[2] + (factor * endAppearance.Transform[2]);
                _animatedAppearance.Transform[3] = (1.0f-factor)*_appearance.Transform[3] + (factor * endAppearance.Transform[3]);
                _animatedAppearance.Transform[4] = (1.0f-factor)*_appearance.Transform[4] + (factor * endAppearance.Transform[4]);
                _animatedAppearance.Transform[5] = (1.0f-factor)*_appearance.Transform[5] + (factor * endAppearance.Transform[5]);
            }

            if (timeFactor >= 1f) {
                toRemove ??= new();
                toRemove.Add(animation);
                if (_appearanceAnimationsLoops != 0) { //add it back to the list with the times updated
                    if(_appearanceAnimationsLoops != -1 && animation.LastInSequence)
                        _appearanceAnimationsLoops -= 1;
                    toReAdd ??= new();
                    DateTime start;
                    if((animation.Flags & AnimationFlags.AnimationParallel) != 0)
                        start = _appearanceAnimations[^1].Start; //either that's also a parallel, or its one that this should be parallel with
                    else
                        start = _appearanceAnimations[^1].Start + _appearanceAnimations[^1].Duration; //if it's not parallel, it's chained
                    AppearanceAnimation repeatAnimation = new AppearanceAnimation(start, animation.Duration, animation.EndAppearance, animation.Easing, animation.Flags, animation.Delay, animation.LastInSequence);
                    toReAdd.Add(repeatAnimation);
                }
            }
        }

        if(toRemove != null)
            foreach (AppearanceAnimation animation in toRemove) {
                EndAppearanceAnimation(animation);
            }

        if(toReAdd != null)
            foreach (AppearanceAnimation animation in toReAdd) {
                _appearanceAnimations.Add(animation);
            }

        return new(_animatedAppearance, null); //one of the very few times it's okay to do this.
    }

    private void UpdateIcon() {
        DirtyTexture();

        if (Appearance == null) {
            DMI = null;
            return;
        }

        if (Appearance.Icon == null) {
            DMI = null;
        } else {
            IoCManager.Resolve<IDreamResourceManager>().LoadResourceAsync<DMIResource>(Appearance.Icon.Value, dmi => {
                if (dmi.Id != Appearance.Icon) return; //Icon changed while resource was loading
                dmi.OnUpdateCallbacks.Add(DirtyTexture);
                DMI = dmi;
                _animationFrame = 0;
                _animationComplete = false;
            });
        }

        Overlays.Clear();
        foreach (var overlayAppearance in Appearance.Overlays) {
            DreamIcon overlay = new DreamIcon(renderTargetPool, interfaceManager, gameTiming, clyde, appearanceSystem, overlayAppearance.MustGetId(), _direction, _iconState);
            overlay.SizeChanged += CheckSizeChange;

            Overlays.Add(overlay);
        }

        Underlays.Clear();
        foreach (var underlayAppearance in Appearance.Underlays) {
            DreamIcon underlay = new DreamIcon(renderTargetPool, interfaceManager, gameTiming, clyde, appearanceSystem, underlayAppearance.MustGetId(), _direction, _iconState);
            underlay.SizeChanged += CheckSizeChange;

            Underlays.Add(underlay);
        }
    }

    /// <summary>
    /// Perform a full (slower) render of this icon's texture, including filters and color
    /// </summary>
    /// <remarks>In a separate method to avoid closure allocations when not executed</remarks>
    /// <returns>The final texture</returns>
    [SuppressMessage("ReSharper", "AccessToModifiedClosure")] // RenderInRenderTarget executes immediately, shouldn't be an issue
    private IRenderTexture FullRenderTexture(DreamViewOverlay viewOverlay, DrawingHandleWorld handle, RendererMetaData iconMetaData, Texture frame) {
        Vector2 requiredRenderSpace = frame.Size;
        foreach (var filter in iconMetaData.MainIcon!.Appearance!.Filters) {
            var requiredSpace = filter.CalculateRequiredRenderSpace(frame.Size,
                renderSource => viewOverlay.RenderSourceLookup.GetValueOrDefault(renderSource)?.Size ?? new(0, 0));

            requiredRenderSpace = Vector2.Max(requiredRenderSpace, requiredSpace);
        }

        var ping = renderTargetPool.Rent((Vector2i)requiredRenderSpace);
        var pong = renderTargetPool.Rent(ping.Size);

        handle.RenderInRenderTarget(pong, () => {
            //we can use the color matrix shader here, since we don't need to blend
            //also because blend mode is none, we don't need to clear
            var colorMatrix = iconMetaData.ColorMatrixToApply;

            ShaderInstance colorShader = DreamViewOverlay.ColorInstance.Duplicate();
            colorShader.SetParameter("colorMatrix", colorMatrix.GetMatrix4());
            colorShader.SetParameter("offsetVector", colorMatrix.GetOffsetVector());
            colorShader.SetParameter("isPlaneMaster",iconMetaData.IsPlaneMaster);
            handle.UseShader(colorShader);

            handle.SetTransform(DreamViewOverlay.CreateRenderTargetFlipMatrix(pong.Size, (pong.Size/2 - frame.Size/2)));
            handle.DrawTextureRect(frame, new Box2(Vector2.Zero, frame.Size));
        }, Color.Black.WithAlpha(0));

        foreach (DreamFilter filterId in iconMetaData.MainIcon!.Appearance!.Filters) {
            ShaderInstance s = appearanceSystem.GetFilterShader(filterId, viewOverlay.RenderSourceLookup);

            handle.RenderInRenderTarget(ping, () => {
                handle.UseShader(s);

                // Technically this should be ping.Size, but they are the same size so avoid the extra closure alloc
                handle.SetTransform(DreamViewOverlay.CreateRenderTargetFlipMatrix(pong.Size, Vector2.Zero));
                handle.DrawTextureRect(pong.Texture, new Box2(Vector2.Zero, pong.Size));
            }, Color.Black.WithAlpha(0));

            // The blur filter runs a more performant two passes
            if (filterId.FilterType == "blur") {
                s = appearanceSystem.GetFilterShader(filterId with {FilterType = "blur_vertical"}, viewOverlay.RenderSourceLookup);
                (ping, pong) = (pong, ping);

                handle.RenderInRenderTarget(ping, () => {
                    handle.UseShader(s);

                    // Technically this should be ping.Size, but they are the same size so avoid the extra closure alloc
                    handle.SetTransform(DreamViewOverlay.CreateRenderTargetFlipMatrix(pong.Size, Vector2.Zero));
                    handle.DrawTextureRect(pong.Texture, new Box2(Vector2.Zero, pong.Size));
                }, Color.Black.WithAlpha(0));
            }

            (ping, pong) = (pong, ping);
        }

        renderTargetPool.Return(ping);
        TextureRenderOffset = -(pong.Texture.Size / 2 - frame.Size / 2);
        return pong;
    }

    private void CheckSizeChange() {
        Box2? aabb = null;
        GetWorldAABB(Vector2.Zero, ref aabb);

        if (aabb != _cachedAABB) {
            _cachedAABB = aabb;
            SizeChanged?.Invoke();
        }
    }

    private void DirtyTexture() {
        _textureDirty = true;
        CachedTexture = null;
    }

    private struct AppearanceAnimation(DateTime start, TimeSpan duration, ImmutableAppearance endAppearance, AnimationEasing easing, AnimationFlags flags, int delay, bool lastInSequence) {
        public readonly DateTime Start = start;
        public readonly TimeSpan Duration = duration;
        public readonly ImmutableAppearance EndAppearance = endAppearance;
        public readonly AnimationEasing Easing = easing;
        public readonly AnimationFlags Flags = flags;
        public readonly int Delay = delay;
        public bool LastInSequence = lastInSequence;
    }
}
