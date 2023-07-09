using OpenDreamClient.Resources;
using OpenDreamClient.Resources.ResourceTypes;
using OpenDreamShared.Dream;
using OpenDreamShared.Resources;
using Robust.Client.Graphics;

namespace OpenDreamClient.Rendering {
    internal sealed class DreamIcon {
        public delegate void SizeChangedEventHandler();

        public List<DreamIcon> Overlays { get; } = new();
        public List<DreamIcon> Underlays { get; } = new();
        public event SizeChangedEventHandler? SizeChanged;
        private ClientAppearanceSystem? appearanceSystem = null;
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

        public AtlasTexture? CurrentFrame {
            get => (Appearance == null || DMI == null) ? null : DMI.GetState(Appearance.IconState)?.GetFrames(Appearance.Direction)[AnimationFrame];
        }

        private int _animationFrame;
        private DateTime _animationFrameTime = DateTime.Now;
        private AppearanceAnimation? _appearanceAnimation = null;
        private Box2? _cachedAABB = null;

        public DreamIcon() { }

        public DreamIcon(uint appearanceId, AtomDirection? parentDir = null) {
            SetAppearance(appearanceId, parentDir);
        }

        public void SetAppearance(uint? appearanceId, AtomDirection? parentDir = null) {
            // End any animations that are currently happening
            // Note that this isn't faithful to the original behavior
            EndAppearanceAnimation();

            if (appearanceId == null) {
                Appearance = null;
                return;
            }

            //for some reason, doing this as a dependency doesn't initialise it in time so a null ref happens
            appearanceSystem ??= EntitySystem.Get<ClientAppearanceSystem>();

            appearanceSystem.LoadAppearance(appearanceId.Value, appearance => {
                if (parentDir != null && appearance.InheritsDirection) {
                    appearance = new IconAppearance(appearance) {
                        Direction = parentDir.Value
                    };
                }

                Appearance = appearance;
            });
        }

        public void StartAppearanceAnimation(IconAppearance endingAppearance, TimeSpan duration) {
            _appearance = CalculateAnimatedAppearance(); //Animation starts from the current animated appearance
            _appearanceAnimation = new AppearanceAnimation(DateTime.Now, duration, endingAppearance);
        }

        public void EndAppearanceAnimation() {
            if (_appearanceAnimation != null)
                _appearance = _appearanceAnimation.Value.EndAppearance;

            _appearanceAnimation = null;
        }

        public Box2 GetWorldAABB(Vector2 worldPos) {
            Box2? aabb = null;

            if (DMI != null && Appearance != null) {
                Vector2 size = DMI.IconSize / (float)EyeManager.PixelsPerMeter;
                Vector2 pixelOffset = Appearance.PixelOffset / (float)EyeManager.PixelsPerMeter;

                worldPos += pixelOffset;
                aabb = Box2.CenteredAround(worldPos, size);
            }

            foreach (DreamIcon underlay in Underlays) {
                Box2 underlayAABB = underlay.GetWorldAABB(worldPos);

                if (aabb == null) aabb = underlayAABB;
                else aabb = aabb.Value.Union(underlayAABB);
            }

            foreach (DreamIcon overlay in Overlays) {
                Box2 overlayAABB = overlay.GetWorldAABB(worldPos);

                if (aabb == null) aabb = overlayAABB;
                else aabb = aabb.Value.Union(overlayAABB);
            }

            return aabb ?? Box2.FromDimensions(Vector2.Zero, Vector2.Zero);
        }

        private void UpdateAnimation() {
            if(DMI == null || Appearance == null)
                return;
            DMIParser.ParsedDMIState? dmiState = DMI.Description.GetStateOrDefault(Appearance.IconState);
            if(dmiState == null)
                return;
            DMIParser.ParsedDMIFrame[] frames = dmiState.GetFrames(Appearance.Direction);

            if (_animationFrame == frames.Length - 1 && !dmiState.Loop) return;

            double elapsedTime = DateTime.Now.Subtract(_animationFrameTime).TotalMilliseconds;
            while (elapsedTime >= frames[_animationFrame].Delay) {
                elapsedTime -= frames[_animationFrame].Delay;
                _animationFrameTime = _animationFrameTime.AddMilliseconds(frames[_animationFrame].Delay);
                _animationFrame++;

                if (_animationFrame >= frames.Length) _animationFrame -= frames.Length;
            }
        }

        private IconAppearance? CalculateAnimatedAppearance() {
            if (_appearanceAnimation == null || _appearance == null)
                return _appearance;

            AppearanceAnimation animation = _appearanceAnimation.Value;
            IconAppearance appearance = new IconAppearance(_appearance);
            float factor = (float)(DateTime.Now - animation.Start).Ticks / animation.Duration.Ticks;
            IconAppearance endAppearance = animation.EndAppearance;

            if (endAppearance.PixelOffset != _appearance.PixelOffset) {
                Vector2 startingOffset = appearance.PixelOffset;
                Vector2 newPixelOffset = Vector2.LerpClamped(in startingOffset, endAppearance.PixelOffset, factor);

                appearance.PixelOffset = (Vector2i)newPixelOffset;
            }

            if (endAppearance.Direction != _appearance.Direction) {
                appearance.Direction = endAppearance.Direction;
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
                    _animationFrameTime = DateTime.Now;
                });
            }

            Overlays.Clear();
            foreach (uint overlayId in Appearance.Overlays) {
                DreamIcon overlay = new DreamIcon(overlayId, Appearance.Direction);
                overlay.SizeChanged += CheckSizeChange;

                Overlays.Add(overlay);
            }

            Underlays.Clear();
            foreach (uint underlayId in Appearance.Underlays) {
                DreamIcon underlay = new DreamIcon(underlayId, Appearance.Direction);
                underlay.SizeChanged += CheckSizeChange;

                Underlays.Add(underlay);
            }
        }

        private void CheckSizeChange() {
            Box2 aabb = GetWorldAABB(Vector2.Zero);

            if (aabb != _cachedAABB) {
                _cachedAABB = aabb;
                SizeChanged?.Invoke();
            }
        }

        private struct AppearanceAnimation {
            public readonly DateTime Start;
            public readonly TimeSpan Duration;
            public readonly IconAppearance EndAppearance;

            public AppearanceAnimation(DateTime start, TimeSpan duration, IconAppearance endAppearance) {
                Start = start;
                Duration = duration;
                EndAppearance = endAppearance;
            }
        }
    }
}
