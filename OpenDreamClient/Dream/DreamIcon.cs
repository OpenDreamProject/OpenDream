using System;
using OpenDreamClient.Renderer;
using System.Collections.Generic;
using System.Drawing;
using OpenDreamClient.Resources.ResourceTypes;
using OpenDreamShared.Dream;
using OpenDreamShared.Resources;
using System.Linq;

namespace OpenDreamClient.Dream {
    class DreamIcon {
        public IDreamTexture DreamTexture { get; private set; } = null;
        public ResourceDMI DMI { get; private set; } = null;
        private Rectangle _textureRect = new Rectangle(0, 0, 0, 0);
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

        public DreamIcon() {

        }

        public DreamIcon(IconVisualProperties visualProperties) {
            VisualProperties = visualProperties;
        }

        public Color GetPixel(int x, int y) {
            if (x > 0 && x < _textureRect.Width && y > 0 && y < _textureRect.Height) {
                Color pixel = DMI.ImageBitmap.GetPixel(_textureRect.X + x, _textureRect.Y + y);

                if (pixel == Color.Transparent) {
                    foreach (DreamIcon overlay in Overlays.Values) {
                        pixel = overlay.GetPixel(x, y);

                        if (pixel != Color.Transparent) return pixel;
                    }

                    return Color.Transparent;
                } else {
                    return pixel;
                }
            } else {
                return Color.Transparent;
            }
        }

        public void AddOverlay(UInt16 id, IconVisualProperties visualProperties) {
            visualProperties.Direction = VisualProperties.Direction;
            
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
                DreamTexture = null;
                return;
            }

            Program.OpenDream.DreamResourceManager.LoadResourceAsync<ResourceDMI>(VisualProperties.Icon, (ResourceDMI dmi) => {
                DMI = dmi;

                string stateName = (VisualProperties.IconState == null) ? dmi.Description.DefaultStateName : VisualProperties.IconState;
                if (!dmi.Description.States.ContainsKey(stateName)) {
                    DreamTexture = null;
                    return;
                }

                DMIParser.ParsedDMIState state = dmi.Description.States[stateName];
                DMIParser.ParsedDMIFrame[] frames = state.Directions.ContainsKey(VisualProperties.Direction) ? state.Directions[VisualProperties.Direction] : state.Directions.Values.First();
                DMIParser.ParsedDMIFrame firstFrame = frames[0];

                _textureRect = new Rectangle(firstFrame.X, firstFrame.Y, dmi.Description.Width, dmi.Description.Height);
                DreamTexture = Program.OpenDream.DreamRenderer.CreateTexture(dmi, _textureRect);
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
