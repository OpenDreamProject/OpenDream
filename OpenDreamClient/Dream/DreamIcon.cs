using System;
using System.Collections.Generic;
using System.Drawing;
using OpenDreamClient.Resources.ResourceTypes;
using OpenDreamShared.Dream;
using OpenDreamShared.Resources;

namespace OpenDreamClient.Dream {
    class DreamIcon {
        public ResourceDMI DMI { get; private set; } = null;
        public List<DreamIcon> Overlays { get; } = new();

        public int AnimationFrame {
            get {
                UpdateAnimation();
                return _animationFrame;
            }
        }

        public IconAppearance Appearance {
            get => _appearance;
            set {
                _appearance = value;
                UpdateIcon();
            }
        }

        private IconAppearance _appearance;
        private int _animationFrame;
        private DateTime _animationFrameTime = DateTime.Now;

        public DreamIcon() { }

        public DreamIcon(IconAppearance appearance) {
            Appearance = appearance;
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

            Rectangle textureRect = DMI.GetTextureRect(Appearance.IconState, Appearance.Direction, AnimationFrame);
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
            }
        }

        public Rectangle GetTextureRect() {
            return DMI.GetTextureRect(Appearance.IconState, Appearance.Direction, AnimationFrame);
        }

        public bool IsValidIcon() {
            return DMI != null && DMI.Description.States.ContainsKey(Appearance.IconState);
        }

        private void UpdateIcon() {
            if (Appearance.Icon == null) {
                DMI = null;
                return;
            }

            Program.OpenDream.ResourceManager.LoadResourceAsync<ResourceDMI>(Appearance.Icon, (ResourceDMI dmi) => {
                if (dmi.ResourcePath != Appearance.Icon) return; //Icon changed while resource was loading

                DMI = dmi;
                _animationFrame = 0;
                _animationFrameTime = DateTime.Now;
            });

            Overlays.Clear();
            foreach (int overlayId in Appearance.Overlays) {
                IconAppearance appearance = Program.OpenDream.IconAppearances[overlayId];

                if (appearance.Direction == AtomDirection.None) {
                    appearance = new IconAppearance(appearance) { Direction = Appearance.Direction };
                }

                Overlays.Add(new DreamIcon(appearance));
            }

            Overlays.Sort(
                new Comparison<DreamIcon>((DreamIcon first, DreamIcon second) => {
                    float diff = first.Appearance.Layer - second.Appearance.Layer;

                    if (diff < 0) return -1;
                    else if (diff > 0) return 1;
                    return 0;
                })
            );
        }
    }
}
