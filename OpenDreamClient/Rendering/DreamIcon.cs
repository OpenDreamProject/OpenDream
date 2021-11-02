using System;
using System.Collections.Generic;
using OpenDreamClient.Input;
using OpenDreamClient.Resources;
using OpenDreamClient.Resources.ResourceTypes;
using OpenDreamShared.Dream;
using OpenDreamShared.Resources;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace OpenDreamClient.Rendering {
    class DreamIcon {
        public delegate void SizeChangedEventHandler();

        public List<DreamIcon> Overlays { get; } = new();
        public List<DreamIcon> Underlays { get; } = new();
        public event SizeChangedEventHandler SizeChanged;

        public DMIResource DMI {
            get => _dmi;
            private set {
                _dmi = value;
                CheckSizeChange();
            }
        }
        private DMIResource _dmi;

        public int AnimationFrame {
            get {
                UpdateAnimation();
                return _animationFrame;
            }
        }

        public IconAppearance Appearance {
            get => _appearance;
            private set {
                _appearance = value;
                UpdateIcon();
            }
        }
        private IconAppearance _appearance;

        public AtlasTexture CurrentFrame {
            get => DMI?.GetState(Appearance.IconState)?.GetFrames(Appearance.Direction)[AnimationFrame];
        }

        private int _animationFrame;
        private DateTime _animationFrameTime = DateTime.Now;
        private Box2? _cachedAABB = null;

        public DreamIcon() { }

        public DreamIcon(uint appearanceId, AtomDirection? parentDir = null) {
            SetAppearance(appearanceId, parentDir);
        }

        public void SetAppearance(uint? appearanceId, AtomDirection? parentDir = null) {
            if (appearanceId == null) {
                Appearance = null;
                return;
            }

            ClientAppearanceSystem appearanceSystem = EntitySystem.Get<ClientAppearanceSystem>();

            appearanceSystem.LoadAppearance(appearanceId.Value, appearance => {
                if (appearance.Direction == AtomDirection.None && parentDir != null) {
                    appearance = new IconAppearance(appearance) {
                        Direction = parentDir.Value
                    };
                }

                Appearance = appearance;
            });
        }

        public Box2 GetWorldAABB(Vector2? worldPos) {
            Box2? aabb = null;

            if (DMI != null) {
                //TODO: Unit size is likely stored somewhere, use that instead of hardcoding 32
                Vector2 size = DMI.IconSize / (32, 32);
                Vector2 pixelOffset = Appearance.PixelOffset / (32, 32);

                worldPos += pixelOffset;
                Vector2 position = (worldPos ?? Vector2.Zero) + (size / 2);

                aabb = Box2.CenteredAround(position, size);
            }

            foreach (DreamIcon underlay in Underlays) {
                Box2 underlayAABB = underlay.GetWorldAABB(worldPos);

                aabb = aabb?.Union(underlayAABB) ?? underlayAABB;
            }

            foreach (DreamIcon overlay in Overlays) {
                Box2 overlayAABB = overlay.GetWorldAABB(worldPos);

                aabb = aabb?.Union(overlayAABB) ?? overlayAABB;
            }

            return aabb ?? Box2.FromDimensions(Vector2.Zero, Vector2.Zero);
        }

        public bool CheckClick(Vector2 iconPos, Vector2 worldPos) {
            IClickMapManager _clickMap = IoCManager.Resolve<IClickMapManager>();
            iconPos += Appearance.PixelOffset;

            if (CurrentFrame != null) {
                Vector2 pos = (worldPos - iconPos) * DMI.IconSize;

                if (_clickMap.IsOccluding(CurrentFrame, ((int)pos.X, DMI.IconSize.Y - (int)pos.Y))) {
                    return true;
                }
            }

            foreach (DreamIcon underlay in Underlays) {
                if (underlay.CheckClick(iconPos, worldPos)) {
                    return true;
                }
            }

            foreach (DreamIcon overlay in Overlays) {
                if (overlay.CheckClick(iconPos, worldPos)) {
                    return true;
                }
            }

            return false;
        }

        public void UpdateAnimation() {
            DMIParser.ParsedDMIState dmiState = DMI.Description.GetState(Appearance.IconState);
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

        private static int LayerSort(DreamIcon first, DreamIcon second) {
            float diff = first.Appearance.Layer - second.Appearance.Layer;

            if (diff < 0) return -1;
            else if (diff > 0) return 1;
            return 0;
        }

        private void UpdateIcon() {
            if (Appearance?.Icon == null) {
                DMI = null;
                return;
            }

            IoCManager.Resolve<IDreamResourceManager>().LoadResourceAsync<DMIResource>(Appearance.Icon, dmi => {
                if (dmi.ResourcePath != Appearance.Icon) return; //Icon changed while resource was loading

                DMI = dmi;
                _animationFrame = 0;
                _animationFrameTime = DateTime.Now;
            });

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

            Overlays.Sort(new Comparison<DreamIcon>(LayerSort));
            Underlays.Sort(new Comparison<DreamIcon>(LayerSort));
        }

        private void CheckSizeChange() {
            Box2 aabb = GetWorldAABB(null);

            if (aabb != _cachedAABB) {
                _cachedAABB = aabb;
                SizeChanged?.Invoke();
            }
        }
    }
}
