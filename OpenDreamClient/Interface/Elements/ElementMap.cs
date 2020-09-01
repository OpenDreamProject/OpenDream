using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.Windows.Input;
using OpenDreamClient.Dream;
using OpenDreamShared.Net.Packets;
using OpenDreamShared.Interface;

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
        private Image _image;

        public ElementMap() {
            _image = new Image();
            _image.HorizontalAlignment = HorizontalAlignment.Center;
            _image.VerticalAlignment = VerticalAlignment.Center;
            _image.Width = 480;
            _image.Height = 480;
            this.Children.Add(_image);
            this.Background = Brushes.Black;

            this.Loaded += OnLoaded;
            this.Unloaded += OnUnloaded;
            this.MouseDown += OnMouseDown;
        }

        private void UpdateVisuals() {
            
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            Program.OpenDream.DreamRenderer.UpdateViewportSize(480, 480);
            _image.Source = Program.OpenDream.DreamRenderer.GetImageSource();
            CompositionTarget.Rendering += this.OnCompositionTargetRendering;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) {
            CompositionTarget.Rendering -= this.OnCompositionTargetRendering;
        }

        private void OnMouseDown(object sender, MouseEventArgs e) {
            Point mousePosition = e.GetPosition(_image);
            System.Drawing.Point cameraPosition = Program.OpenDream.DreamRenderer.GetCameraPosition();
            mousePosition.X = Math.Floor(mousePosition.X);
            mousePosition.Y = _image.Height - Math.Floor(mousePosition.Y);

            if (mousePosition.X < 0 || mousePosition.X > _image.Width || mousePosition.Y < 0 || mousePosition.Y > _image.Height) return;

            int viewATOMX = (int)(mousePosition.X / 32);
            int viewATOMY = (int)(mousePosition.Y / 32);
            int atomX = (cameraPosition.X - 7) + viewATOMX;
            int atomY = (cameraPosition.Y - 7) + viewATOMY;
            int iconX = (int)mousePosition.X - (viewATOMX * 32);
            int iconY = (int)mousePosition.Y - (viewATOMY * 32);
            ATOM turf = Program.OpenDream.Map.Turfs[atomX, atomY];

            if (turf != null) {
                ATOM clickedATOM = null;

                foreach (ATOM atom in turf.Contents) {
                    if (atom.Icon.GetPixel(iconX, iconY).A != 0) {
                        clickedATOM = atom;
                        break;
                    }
                }

                if (clickedATOM == null) clickedATOM = turf;
                Program.OpenDream.Connection.SendPacket(new PacketClickATOM(clickedATOM.ID, iconX, iconY));
            }
        }

        private void OnCompositionTargetRendering(object sender, EventArgs e) {
            Program.OpenDream.DreamRenderer.RenderFrame();
        }
    }
}
