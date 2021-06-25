using OpenDreamShared.Interface;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OpenDreamClient.Interface.Controls {
    class InterfaceControl : InterfaceElement {
        public FrameworkElement UIElement { get; private set; }
        public bool IsDefault { get => _controlDescriptor.IsDefault; }
        public System.Drawing.Size? Size { get => _controlDescriptor.Size; }
        public System.Drawing.Point? Pos { get => _controlDescriptor.Pos; }
        public System.Drawing.Point? Anchor1 { get => _controlDescriptor.Anchor1; }
        public System.Drawing.Point? Anchor2 { get => _controlDescriptor.Anchor2; }

        protected ControlDescriptor _controlDescriptor { get => _elementDescriptor as ControlDescriptor; }
        protected ControlWindow _window;

        public InterfaceControl(ControlDescriptor controlDescriptor, ControlWindow window) : base(controlDescriptor) {
            _window = window;
            UIElement = CreateUIElement();

            UpdateElementDescriptor();
        }

        protected virtual FrameworkElement CreateUIElement() {
            throw new NotImplementedException("Invalid InterfaceControl");
        }

        public override void UpdateElementDescriptor() {
            System.Drawing.Point pos = _controlDescriptor.Pos.GetValueOrDefault();

            Canvas.SetLeft(UIElement, pos.X);
            Canvas.SetTop(UIElement, pos.Y);
            if (UIElement is FrameworkElement frameworkElement) {
                System.Drawing.Size size = _controlDescriptor.Size.GetValueOrDefault();

                frameworkElement.Width = size.Width;
                frameworkElement.Height = size.Height;
            }

            _window?.UpdateAnchors();

            if (_controlDescriptor.BackgroundColor.HasValue) {
                System.Drawing.Color color = _controlDescriptor.BackgroundColor.Value;
                Brush brush = new SolidColorBrush(Color.FromRgb(color.R, color.G, color.B));

                switch (UIElement) {
                    case Panel panel:
                        panel.Background = brush;
                        break;
                    case Control control:
                        control.Background = brush;
                        break;
                }
            }

            UIElement.Visibility = _controlDescriptor.IsVisible ? Visibility.Visible : Visibility.Hidden;
            UIElement.IsEnabled = !_controlDescriptor.IsDisabled;
        }

        public virtual void Output(string value, string data) {

        }
    }
}
