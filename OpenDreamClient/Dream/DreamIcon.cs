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
        public IconVisualProperties VisualProperties {
            get => _visualProperties;
            set {
                if (!_visualProperties.Equals(value)) {
                    _visualProperties = value;
                    UpdateIcon();
                }
            }
        }

        private IconVisualProperties _visualProperties;
        private DateTime _animationStartTime = DateTime.Now;

        public DreamIcon() {

        }

        public DreamIcon(IconVisualProperties visualProperties) {
            VisualProperties = visualProperties;
        }

        public int GetCurrentAnimationFrame() {
            DMIParser.ParsedDMIState dmiState = DMI.Description.GetState(VisualProperties.IconState);
            DMIParser.ParsedDMIFrame[] frames = dmiState.GetFrames(VisualProperties.Direction);
            double elapsedTime = DateTime.Now.Subtract(_animationStartTime).TotalMilliseconds / 100;
            int animationFrame = (int)(elapsedTime / frames[0].Delay); //TODO: Don't just use the first frame's delay

            if (dmiState.Loop) {
                animationFrame %= frames.Length;
            } else {
                animationFrame = Math.Min(animationFrame, frames.Length - 1);
            }

            return animationFrame;
        }

        public Color GetPixel(int x, int y) {
            Rectangle textureRect = DMI.GetTextureRect(VisualProperties.IconState, VisualProperties.Direction, GetCurrentAnimationFrame());

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
            return DMI != null && DMI.Description.States.ContainsKey(VisualProperties.IconState);
        }

        public void AddOverlay(UInt16 id, IconVisualProperties visualProperties) {
            Overlays.Add(id, new DreamIcon(visualProperties));
        }

        public void RemoveOverlay(UInt16 id) {
            Overlays.Remove(id);
        }

        private void UpdateIcon() {
            UpdateTexture();
            UpdateOverlays();
        }

        private void UpdateTexture() {
            if (VisualProperties.Icon == null) {
                DMI = null;
                return;
            }

            Program.OpenDream.ResourceManager.LoadResourceAsync<ResourceDMI>(VisualProperties.Icon, (ResourceDMI dmi) => {
                if (dmi.ResourcePath != VisualProperties.Icon) return; //Icon changed while resource was loading

                DMI = dmi;
                _animationStartTime = DateTime.Now;
            });
        }

        private void UpdateOverlays() {
            foreach (DreamIcon overlay in Overlays.Values) {
                IconVisualProperties visualProperties = overlay.VisualProperties;

                if (visualProperties.Direction != VisualProperties.Direction) {
                    visualProperties.Direction = VisualProperties.Direction;
                    overlay.VisualProperties = visualProperties;
                }
            }
        }
    }
}
