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
            this.MouseLeftButtonDown += OnLeftMouseDown;
        }

        public void UpdateVisuals() {
            
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            _dreamRenderer.SetViewportSize(480, 480);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) {
            
        }

        private void OnLeftMouseDown(object sender, MouseEventArgs e) {
            e.Handled = true;
            this.Focus();

            Point mousePosition = e.GetPosition(_dreamRenderer.OpenGLViewControl);
            (int, int) cameraPosition = _dreamRenderer.GetCameraPosition();
            mousePosition.X = Math.Floor(mousePosition.X);
            mousePosition.Y = _dreamRenderer.OpenGLViewControl.Height - Math.Floor(mousePosition.Y);

            if (mousePosition.X < 0 || mousePosition.X > _dreamRenderer.OpenGLViewControl.Width || mousePosition.Y < 0 || mousePosition.Y > _dreamRenderer.OpenGLViewControl.Height) return;

            ATOM clickedATOM = null;
            int iconX = 0, iconY = 0;

            foreach (ATOM screenObject in Program.OpenDream.ScreenObjects) {
                System.Drawing.Point screenCoordinates = screenObject.ScreenLocation.GetScreenCoordinates(32);
                System.Drawing.Rectangle iconRect = new(screenCoordinates, new System.Drawing.Size(32, 32));

                if (iconRect.Contains(new System.Drawing.Point((int)mousePosition.X, (int)mousePosition.Y))) {
                    int screenObjectIconX = (int)mousePosition.X - iconRect.X;
                    int screenObjectIconY = 32 - ((int)mousePosition.Y - iconRect.Y);

                    if (screenObject.Icon.GetPixel(screenObjectIconX, screenObjectIconY).A != 0) {
                        clickedATOM = screenObject;
                        iconX = screenObjectIconX;
                        iconY = screenObjectIconY;
                    }
                }
                
            }

            if (clickedATOM == null) {
                int viewATOMX = (int)(mousePosition.X / 32);
                int viewATOMY = (int)(mousePosition.Y / 32);
                int atomX = (cameraPosition.Item1 - 7) + viewATOMX;
                int atomY = (cameraPosition.Item2 - 7) + viewATOMY;

                iconX = (int)mousePosition.X % 32;
                iconY = (int)mousePosition.Y % 32;
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

            PacketClickAtom pClickAtom = new PacketClickAtom(clickedATOM.ID, iconX, iconY);
            pClickAtom.ModifierShift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
            pClickAtom.ModifierCtrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
            pClickAtom.ModifierAlt = Keyboard.Modifiers.HasFlag(ModifierKeys.Alt);
            Program.OpenDream.Connection.SendPacket(pClickAtom);
        }
    }
}
