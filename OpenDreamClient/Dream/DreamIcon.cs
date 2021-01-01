using System;
using System.Collections.Generic;
using System.Drawing;
using OpenDreamClient.Resources.ResourceTypes;
using OpenDreamShared.Dream;
using OpenDreamShared.Resources;

namespace OpenDreamClient.Dream {
    class DreamIcon {
        public ResourceDMI DMI { get; private set; } = null;
        public Dictionary<UInt16, DreamIcon> Overlays { get; } = new Dictionary<UInt16, DreamIcon>();
        public IconAppearance Appearance {
            get => _appearance;
            set {
                _appearance = value;
                UpdateIcon();
            }
        }

        private IconAppearance _appearance;
        private DateTime _animationStartTime = DateTime.Now;

        public DreamIcon() {

        }

        public DreamIcon(IconAppearance appearance) {
            Appearance = appearance;
        }

        public int GetCurrentAnimationFrame() {
            DMIParser.ParsedDMIState dmiState = DMI.Description.GetState(Appearance.IconState);
            DMIParser.ParsedDMIFrame[] frames = dmiState.GetFrames(Appearance.Direction);
            double elapsedTime = DateTime.Now.Subtract(_animationStartTime).TotalMilliseconds / 100;

            int animationFrame = -1;
            float animationTime = 0;
            do {
                animationFrame = (animationFrame + 1) % frames.Length;
                animationTime += frames[animationFrame].Delay;

                if (!dmiState.Loop && animationFrame == frames.Length - 1) break;
            } while (animationTime < elapsedTime);

            return animationFrame;
        }

        public Color GetPixel(int x, int y) {
            Rectangle textureRect = DMI.GetTextureRect(Appearance.IconState, Appearance.Direction, GetCurrentAnimationFrame());

            if (x > 0 && x < textureRect.Width && y > 0 && y < textureRect.Height) {
                Color pixel = DMI.ImageBitmap.GetPixel(textureRect.X + x, textureRect.Y + y);

                if (pixel.A == Color.Transparent.A || !IsValidIcon()) {
                    foreach (DreamIcon overlay in Overlays.Values) {
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

        public bool IsValidIcon() {
            return DMI != null && DMI.Description.States.ContainsKey(Appearance.IconState);
        }

        public void AddOverlay(UInt16 id, IconAppearance appearance) {
            Overlays.Add(id, new DreamIcon(appearance));
        }

        public void RemoveOverlay(UInt16 id) {
            Overlays.Remove(id);
        }

        private void UpdateIcon() {
            UpdateTexture();
            UpdateOverlays();
        }

        private void UpdateTexture() {
            if (Appearance.Icon == null) {
                DMI = null;
                return;
            }

            Program.OpenDream.ResourceManager.LoadResourceAsync<ResourceDMI>(Appearance.Icon, (ResourceDMI dmi) => {
                if (dmi.ResourcePath != Appearance.Icon) return; //Icon changed while resource was loading

                DMI = dmi;
                _animationStartTime = DateTime.Now;
            });
        }

        private void UpdateOverlays() {
            foreach (DreamIcon overlay in Overlays.Values) {
                overlay.Appearance = new IconAppearance(overlay.Appearance) { Direction = Appearance.Direction };
            }
        }
    }
}
