using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.Windows.Input;
using OpenDreamShared.Interface;
using OpenDreamClient.Renderer;
using OpenDreamClient.Dream;
using OpenDreamShared.Net.Packets;
using OpenDreamShared.Dream;
using Rectangle = System.Drawing.Rectangle;

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

            this.MouseLeftButtonDown += OnLeftMouseDown;
        }

        public void UpdateVisuals() {
            
        }

        private (int X, int Y) ControlToScreenCoordinates(double x, double y) {
            return ((int)Math.Floor(x), (int)_dreamRenderer.OpenGLViewControl.Height - (int)Math.Floor(y));
        }

        private bool IsOverAtom(ATOM atom, (int X, int Y) screenCoordinates, bool isScreenAtom) {
            Rectangle iconRect = _dreamRenderer.GetIconRect(atom, isScreenAtom);
            
            if (_dreamRenderer.IsAtomVisible(atom, isScreenAtom) && iconRect.Contains(new System.Drawing.Point(screenCoordinates.X, screenCoordinates.Y))) {
                int atomIconX = screenCoordinates.X - iconRect.X;
                int atomIconY = screenCoordinates.Y - iconRect.Y;

                return atom.Icon.GetPixel(atomIconX, 32 - atomIconY).A != 0;
            }

            return false;
        }

        private void OnLeftMouseDown(object sender, MouseEventArgs e) {
            Point mousePosition = e.GetPosition(_dreamRenderer.OpenGLViewControl);
            if (mousePosition.X < 0 || mousePosition.X > _dreamRenderer.OpenGLViewControl.Width ||
                mousePosition.Y < 0 || mousePosition.Y > _dreamRenderer.OpenGLViewControl.Height) return;
            (int X, int Y) screenCoordinates = ControlToScreenCoordinates(mousePosition.X, mousePosition.Y);

            ATOM clickedATOM = null;
            int iconX = 0, iconY = 0;
            ScreenLocation screenLocation = new ScreenLocation(screenCoordinates.X, screenCoordinates.Y, 32);

            foreach (ATOM screenObject in Program.OpenDream.ScreenObjects) {
                Rectangle iconRect = _dreamRenderer.GetIconRect(screenObject, true);

                if (IsOverAtom(screenObject, screenCoordinates, true)) {
                    clickedATOM = screenObject;
                    iconX = screenCoordinates.X - iconRect.X;
                    iconY = screenCoordinates.Y - iconRect.Y;
                }
            }

            if (clickedATOM == null) {
                foreach (ATOM turf in Program.OpenDream.Map.Turfs) {
                    foreach (ATOM atom in turf.Contents) {
                        bool isAbove = (clickedATOM == null || clickedATOM.Icon.Appearance.Layer <= atom.Icon.Appearance.Layer);

                        if (isAbove && IsOverAtom(atom, screenCoordinates, false)) {
                            Rectangle iconRect = _dreamRenderer.GetIconRect(atom, false);

                            clickedATOM = atom;
                            iconX = screenCoordinates.X - iconRect.X;
                            iconY = screenCoordinates.Y - iconRect.Y;
                        }
                    }

                    if (clickedATOM == null && IsOverAtom(turf, screenCoordinates, false)) {
                        clickedATOM = turf;
                        iconX = screenCoordinates.X % 32;
                        iconY = screenCoordinates.Y % 32;
                    }
                }
            }

            if (clickedATOM == null) return;
            e.Handled = true;
            this.Focus();

            PacketClickAtom pClickAtom = new PacketClickAtom(clickedATOM.ID, iconX, iconY, screenLocation);
            pClickAtom.ModifierShift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
            pClickAtom.ModifierCtrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
            pClickAtom.ModifierAlt = Keyboard.Modifiers.HasFlag(ModifierKeys.Alt);
            Program.OpenDream.Connection.SendPacket(pClickAtom);
        }
    }
}
