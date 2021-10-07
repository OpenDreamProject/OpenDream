using System;
using System.Collections.Generic;
using System.Drawing;
using OpenDreamClient.Resources;
using OpenDreamClient.Resources.ResourceTypes;
using OpenDreamShared.Dream;
using OpenDreamShared.Resources;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace OpenDreamClient.Rendering {
    class DreamIcon {
        public DMIResource DMI { get; private set; } = null;
        public List<DreamIcon> Overlays { get; } = new();
        public List<DreamIcon> Underlays { get; } = new();

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

        public AtlasTexture CurrentFrame {
            get => DMI?.States[Appearance.IconState].GetFrames(Appearance.Direction)[AnimationFrame];
        }

        private IconAppearance _appearance;
        private int _animationFrame;
        private DateTime _animationFrameTime = DateTime.Now;

        public DreamIcon() { }

        public DreamIcon(uint appearanceId) {
            SetAppearance(appearanceId);
        }

        public void SetAppearance(uint appearanceId) {
            ClientAppearanceSystem appearanceSystem = EntitySystem.Get<ClientAppearanceSystem>();

            appearanceSystem.LoadAppearance(appearanceId, appearance => {
                Appearance = appearance;
            });
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

        public Color GetPixel(int x, int y) {
            UpdateAnimation();

            //TODO
            return Color.White;

            /*Rectangle textureRect = DMI.GetTextureRect(Appearance.IconState, Appearance.Direction, AnimationFrame);
            if (x > 0 && x < textureRect.Width && y > 0 && y < textureRect.Height) {
                Color pixel = DMI.ImageBitmap.GetPixel(textureRect.X + x, textureRect.Y + y);

                if (pixel.A == Color.Transparent.A || !IsValidIcon()) {
                    foreach (DreamIcon overlay in Overlays) {
                        pixel = overlay.GetPixel(x, y);

                        if (pixel.A != Color.Transparent.A) return pixel;
                    }

                    return Color.Transparent;
                } else {
                    return pixel;
                }
            } else {
                return Color.Transparent;
            }*/
        }

        public bool IsMouseOver(int x, int y) {
            switch (Appearance.MouseOpacity) {
                case MouseOpacity.Transparent: return false;
                case MouseOpacity.Opaque: return true;
                case MouseOpacity.PixelOpaque: return GetPixel(x, y).A != 0;
                default: throw new InvalidOperationException($"Icon has an invalid mouse opacity ({Appearance.MouseOpacity})");
            }
        }

        public bool IsValidIcon() {
            return DMI != null && DMI.Description.States.ContainsKey(Appearance.IconState);
        }

        public static int LayerSort(DreamIcon first, DreamIcon second) {
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

            IoCManager.Resolve<IDreamResourceManager>().LoadResourceAsync<DMIResource>(Appearance.Icon.ToString(), dmi => {
                if (dmi.ResourcePath != Appearance.Icon.ToString()) return; //Icon changed while resource was loading

                DMI = dmi;
                _animationFrame = 0;
                _animationFrameTime = DateTime.Now;
            });

            Overlays.Clear();
            foreach (uint overlayId in Appearance.Overlays) {
                //TODO: Some overlays assume the direction of their owning icon
                Overlays.Add(new DreamIcon(overlayId));
            }

            Underlays.Clear();
            foreach (uint underlayId in Appearance.Underlays) {
                //TODO: Some underlays assume the direction of their owning icon
                Underlays.Add(new DreamIcon(underlayId));
            }

            Overlays.Sort(new Comparison<DreamIcon>(LayerSort));
            Underlays.Sort(new Comparison<DreamIcon>(LayerSort));
        }
    }
}
