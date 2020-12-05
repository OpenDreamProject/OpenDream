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
        public ElementDescriptor ElementDescriptor {
            get => _elementDescriptor;
            set {
                _elementDescriptor = (ElementDescriptorMap)value;
            }
        }

        private ElementDescriptorMap _elementDescriptor;
        private DreamRenderer _dreamRenderer;

        public ElementMap() {
            _dreamRenderer = new DreamRenderer();
            this.Children.Add(_dreamRenderer.OpenGLViewControl);
            
            this.Focusable = true;
            this.IsEnabled = true;
            this.UseLayoutRounding = true;
            this.Background = Brushes.Black;

            this.Loaded += OnLoaded;
            this.Unloaded += OnUnloaded;
            this.MouseDown += OnMouseDown;
        }

        public void UpdateVisuals() {
            
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            _dreamRenderer.SetViewportSize(480, 480);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) {
            
        }

        private void OnMouseDown(object sender, MouseEventArgs e) {
            e.Handled = true;
            this.Focus();

            Point mousePosition = e.GetPosition(_dreamRenderer.OpenGLViewControl);
            (int, int) cameraPosition = _dreamRenderer.GetCameraPosition();
            mousePosition.X = Math.Floor(mousePosition.X);
            mousePosition.Y = _dreamRenderer.OpenGLViewControl.Height - Math.Floor(mousePosition.Y);

            if (mousePosition.X < 0 || mousePosition.X > _dreamRenderer.OpenGLViewControl.Width || mousePosition.Y < 0 || mousePosition.Y > _dreamRenderer.OpenGLViewControl.Height) return;

            int iconX = (int)mousePosition.X % 32;
            int iconY = (int)mousePosition.Y % 32;
            ATOM clickedATOM = null;

            foreach (ATOM screenObject in Program.OpenDream.ScreenObjects) {
                System.Drawing.Point screenCoordinates = screenObject.ScreenLocation.GetScreenCoordinates(32);
                System.Drawing.Rectangle iconRect = new(screenCoordinates, new System.Drawing.Size(32, 32));

                if (iconRect.Contains(new System.Drawing.Point((int)mousePosition.X, (int)mousePosition.Y))) {
                    bool isAbove = (clickedATOM == null || clickedATOM.Icon.VisualProperties.Layer <= screenObject.Icon.VisualProperties.Layer);

                    if (isAbove && screenObject.Icon.GetPixel(iconX, 32 - iconY).A != 0) {
                        clickedATOM = screenObject;
                    }
                }
                
            }

            if (clickedATOM == null) {
                int viewATOMX = (int)(mousePosition.X / 32);
                int viewATOMY = (int)(mousePosition.Y / 32);
                int atomX = (cameraPosition.Item1 - 7) + viewATOMX;
                int atomY = (cameraPosition.Item2 - 7) + viewATOMY;
                if (atomX >= 0 && atomY >= 0 && atomX < Program.OpenDream.Map.Turfs.GetLength(0) && atomY < Program.OpenDream.Map.Turfs.GetLength(1)) {
                    ATOM turf = Program.OpenDream.Map.Turfs[atomX, atomY];

                    if (turf != null) {
                        foreach (ATOM atom in turf.Contents) {
                            bool isAbove = (clickedATOM == null || clickedATOM.Icon.VisualProperties.Layer <= atom.Icon.VisualProperties.Layer);

                            if (isAbove && atom.Icon.GetPixel(iconX, 32 - iconY).A != 0) {
                                clickedATOM = atom;
                            }
                        }

                        if (clickedATOM == null) clickedATOM = turf;
                    }
                }
            }

            Program.OpenDream.Connection.SendPacket(new PacketClickAtom(clickedATOM.ID, iconX, iconY));
        }
    }
}
