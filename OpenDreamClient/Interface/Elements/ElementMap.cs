using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.Windows.Input;
using OpenDreamShared.Interface;
using OpenDreamClient.Renderer;
using OpenDreamClient.Dream;
using OpenDreamShared.Net.Packets;

namespace OpenDreamClient.Interface.Elements {
    class ElementMap : Grid, IElement {
        public InterfaceElementDescriptor ElementDescriptor {
            get => _elementDescriptor;
            set {
                _elementDescriptor = value;
                UpdateVisuals();
            }
        }

        private InterfaceElementDescriptor _elementDescriptor;
        private DreamRenderer _dreamRenderer;

        public ElementMap() {
            _dreamRenderer = new DreamRenderer();
            this.Children.Add(_dreamRenderer.OpenGLViewControl);
            
            this.UseLayoutRounding = true;
            this.Background = Brushes.Black;

            this.Loaded += OnLoaded;
            this.Unloaded += OnUnloaded;
            this.MouseDown += OnMouseDown;
        }

        private void UpdateVisuals() {
            
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            _dreamRenderer.SetViewportSize(480, 480);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) {
            
        }

        private void OnMouseDown(object sender, MouseEventArgs e) {
            Point mousePosition = e.GetPosition(_dreamRenderer.OpenGLViewControl);
            (int, int) cameraPosition = _dreamRenderer.GetCameraPosition();
            mousePosition.X = Math.Floor(mousePosition.X);
            mousePosition.Y = _dreamRenderer.OpenGLViewControl.Height - Math.Floor(mousePosition.Y);

            if (mousePosition.X < 0 || mousePosition.X > _dreamRenderer.OpenGLViewControl.Width || mousePosition.Y < 0 || mousePosition.Y > _dreamRenderer.OpenGLViewControl.Height) return;

            int viewATOMX = (int)(mousePosition.X / 32);
            int viewATOMY = (int)(mousePosition.Y / 32);
            int atomX = (cameraPosition.Item1 - 7) + viewATOMX;
            int atomY = (cameraPosition.Item2 - 7) + viewATOMY;
            int iconX = (int)mousePosition.X - (viewATOMX * 32);
            int iconY = (int)mousePosition.Y - (viewATOMY * 32);
            ATOM turf = Program.OpenDream.Map.Turfs[atomX, atomY];

            if (turf != null) {
                ATOM clickedATOM = null;

                foreach (ATOM atom in turf.Contents) {
                    bool isAbove = (clickedATOM == null || clickedATOM.Icon.VisualProperties.Layer <= atom.Icon.VisualProperties.Layer);

                    if (atom.Icon.GetPixel(iconX, iconY).A != 0 && isAbove) {
                        clickedATOM = atom;
                    }
                }

                if (clickedATOM == null) clickedATOM = turf;
                Program.OpenDream.Connection.SendPacket(new PacketClickAtom(clickedATOM.ID, iconX, iconY));
            }
        }
    }
}
