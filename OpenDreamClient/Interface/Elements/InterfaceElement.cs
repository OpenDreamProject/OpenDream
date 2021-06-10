using OpenDreamShared.Interface;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OpenDreamClient.Interface.Elements {
    class InterfaceElement {
        public FrameworkElement UIElement { get; private set; }
        public string Name { get => _elementDescriptor.Name; }
        public bool IsDefault { get => _elementDescriptor.IsDefault; }
        public System.Drawing.Size? Size { get => _elementDescriptor.Size; }
        public System.Drawing.Point? Pos { get => _elementDescriptor.Pos; }
        public System.Drawing.Point? Anchor1 { get => _elementDescriptor.Anchor1; }
        public System.Drawing.Point? Anchor2 { get => _elementDescriptor.Anchor2; }

        protected WindowElementDescriptor _elementDescriptor;
        protected ElementWindow _window;

        public InterfaceElement(WindowElementDescriptor elementDescriptor, ElementWindow window) {
            _elementDescriptor = elementDescriptor;
            _window = window;
            UIElement = CreateUIElement();

            UpdateElementDescriptor();
        }

        protected virtual FrameworkElement CreateUIElement() {
            throw new NotImplementedException("Invalid InterfaceElement");
        }

        public virtual void UpdateElementDescriptor() {
            System.Drawing.Point pos = _elementDescriptor.Pos.GetValueOrDefault();

            Canvas.SetLeft(UIElement, pos.X);
            Canvas.SetTop(UIElement, pos.Y);
            if (UIElement is FrameworkElement frameworkElement) {
                System.Drawing.Size size = _elementDescriptor.Size.GetValueOrDefault();

                frameworkElement.Width = size.Width;
                frameworkElement.Height = size.Height;
            }

            _window?.UpdateAnchors();

            if (_elementDescriptor.BackgroundColor.HasValue) {
                System.Drawing.Color color = _elementDescriptor.BackgroundColor.Value;
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

            UIElement.Visibility = _elementDescriptor.IsVisible ? Visibility.Visible : Visibility.Hidden;
            UIElement.IsEnabled = !_elementDescriptor.IsDisabled;
        }

        public virtual void Output(string value, string data) {

        }

        public virtual void Shutdown() {

        }

        public void SetAttribute(string name, object value) {
            _elementDescriptor.SetAttribute(name, value);
            UpdateElementDescriptor();
        }
    }
}
