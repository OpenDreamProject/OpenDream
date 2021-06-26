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
using System.Windows.Media.Imaging;
using Rectangle = System.Drawing.Rectangle;

namespace OpenDreamClient.Interface.Controls {
    class ControlMap : InterfaceControl {
        private DreamRenderer _dreamRenderer;
        private Grid _grid;

        public ControlMap(ControlDescriptor controlDescriptor, ControlWindow window) : base(controlDescriptor, window) { }

        protected override FrameworkElement CreateUIElement() {
            _grid = new Grid() {
                Focusable = true,
                IsEnabled = true,
                UseLayoutRounding = true,
                Background = Brushes.Black
            };
            _grid.MouseLeftButtonDown += OnLeftMouseDown;
            _grid.SizeChanged += OnSizeChanged;

            _dreamRenderer = new DreamRenderer();
            _grid.Children.Add(_dreamRenderer.OpenGLViewControl);

            return _grid;
        }

        public override void Shutdown() {
            _dreamRenderer.StopRendering();
        }

        public BitmapSource CreateScreenshot() {
            SharpGLControl control = _dreamRenderer.OpenGLViewControl;
            DpiScale dpi = VisualTreeHelper.GetDpi(control);
            double dpiX = dpi.PixelsPerInchX;
            double dpiY = dpi.PixelsPerInchY;

            RenderTargetBitmap renderTarget = new RenderTargetBitmap((int)control.Width, (int)control.Height, dpiX, dpiY, PixelFormats.Pbgra32);
            renderTarget.Render(control);

            return renderTarget;
        }

        private (int X, int Y) ControlToScreenCoordinates(double x, double y) {
            x /= _dreamRenderer.OpenGLViewControl.Scale;
            y /= _dreamRenderer.OpenGLViewControl.Scale;

            return ((int)Math.Floor(x), (int)_dreamRenderer.OpenGLViewControl.ViewportHeight - (int)Math.Floor(y));
        }

        private bool IsOverAtom(ATOM atom, (int X, int Y) screenCoordinates, bool isScreenAtom) {
            Rectangle iconRect = _dreamRenderer.GetIconRect(atom, isScreenAtom);

            if (_dreamRenderer.IsAtomVisible(atom, isScreenAtom) && iconRect.Contains(new System.Drawing.Point(screenCoordinates.X, screenCoordinates.Y))) {
                int atomIconX = (screenCoordinates.X - iconRect.X) % 32;
                int atomIconY = 32 - ((screenCoordinates.Y - iconRect.Y) % 32);

                return atom.Icon.IsMouseOver(atomIconX, atomIconY);
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
                    iconX = (screenCoordinates.X - iconRect.X) % 32;
                    iconY = (screenCoordinates.Y - iconRect.Y) % 32;
                }
            }

            if (clickedATOM == null) {
                foreach (ATOM turf in Program.OpenDream.Map.Levels[_dreamRenderer.Camera.Z].Turfs) {
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
            _grid.Focus();

            PacketClickAtom pClickAtom = new PacketClickAtom(clickedATOM.ID, iconX, iconY, screenLocation);
            pClickAtom.ModifierShift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
            pClickAtom.ModifierCtrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
            pClickAtom.ModifierAlt = Keyboard.Modifiers.HasFlag(ModifierKeys.Alt);
            Program.OpenDream.Connection.SendPacket(pClickAtom);
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e) {
            double widthScaling = Math.Max(1, Math.Floor(_grid.Width / _dreamRenderer.OpenGLViewControl.ViewportWidth));
            double heightScaling = Math.Max(1, Math.Floor(_grid.Height / _dreamRenderer.OpenGLViewControl.ViewportHeight));

            _dreamRenderer.SetScale(Math.Min(widthScaling, heightScaling));
        }
    }
}
