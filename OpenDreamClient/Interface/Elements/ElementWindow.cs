using OpenDreamShared.Interface;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace OpenDreamClient.Interface.Elements {
    class ElementWindow : InterfaceElement {
        public List<InterfaceElement> ChildElements = new();

        private WindowDescriptor _windowDescriptor;
        private Canvas _canvas;

        public ElementWindow(WindowDescriptor windowDescriptor) : base(windowDescriptor.MainElementDescriptor, null) {
            _windowDescriptor = windowDescriptor;
        }

        protected override FrameworkElement CreateUIElement() {
            _canvas = new Canvas() {
                Margin = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            _canvas.SizeChanged += (object sender, SizeChangedEventArgs e) => UpdateAnchors();

            return _canvas;
        }

        public override void UpdateElementDescriptor() {
            // Don't call base.UpdateElementDescriptor();
        }

        public override void Shutdown() {
            foreach (InterfaceElement element in ChildElements) {
                element.Shutdown();
            }
        }

        public void CreateChildElements() {
            foreach (ElementDescriptor elementDescriptor in _windowDescriptor.ElementDescriptors) {
                if (elementDescriptor == _windowDescriptor.MainElementDescriptor) continue;

                InterfaceElement element = elementDescriptor switch {
                    ElementDescriptorChild => new ElementChild(elementDescriptor, this),
                    ElementDescriptorInput => new ElementInput(elementDescriptor, this),
                    ElementDescriptorButton => new ElementButton(elementDescriptor, this),
                    ElementDescriptorOutput => new ElementOutput(elementDescriptor, this),
                    ElementDescriptorInfo => new ElementInfo(elementDescriptor, this),
                    ElementDescriptorMap => new ElementMap(elementDescriptor, this),
                    ElementDescriptorBrowser => new ElementBrowser(elementDescriptor, this),
                    _ => throw new Exception("Invalid descriptor")
                };

                ChildElements.Add(element);
                _canvas.Children.Add(element.UIElement);
            }
        }

        public Window CreateWindow() {
            Window window = new Window();

            window.Content = UIElement;
            window.Width = _elementDescriptor.Size?.Width ?? 640;
            window.Height = _elementDescriptor.Size?.Height ?? 440;
            window.Closing += (object sender, CancelEventArgs e) => {
                window.Owner = null; //Without this, the owning window ends up minimized
            };

            return window;
        }

        public void UpdateAnchors() {
            foreach (InterfaceElement element in ChildElements) {
                FrameworkElement control = element.UIElement;

                if (element.Anchor1.HasValue) {
                    System.Drawing.Point elementPos = element.Pos.GetValueOrDefault();
                    System.Drawing.Size windowSize = Size.GetValueOrDefault();

                    double offset1X = elementPos.X - (windowSize.Width * element.Anchor1.Value.X / 100);
                    double offset1Y = elementPos.Y - (windowSize.Height * element.Anchor1.Value.Y / 100);
                    double left = (_canvas.ActualWidth * element.Anchor1.Value.X / 100) + offset1X;
                    double top = (_canvas.ActualHeight * element.Anchor1.Value.Y / 100) + offset1Y;
                    Canvas.SetLeft(control, Math.Max(left, 0));
                    Canvas.SetTop(control, Math.Max(top, 0));

                    if (element.Anchor2.HasValue) {
                        System.Drawing.Size elementSize = element.Size.GetValueOrDefault();

                        double offset2X = (elementPos.X + elementSize.Width) - (windowSize.Width * element.Anchor2.Value.X / 100);
                        double offset2Y = (elementPos.Y + elementSize.Height) - (windowSize.Height * element.Anchor2.Value.Y / 100);
                        double width = (_canvas.ActualWidth * element.Anchor2.Value.X / 100) + offset2X - left;
                        double height = (_canvas.ActualHeight * element.Anchor2.Value.Y / 100) + offset2Y - top;
                        control.Width = Math.Max(width, 0);
                        control.Height = Math.Max(height, 0);
                    }
                }
            }
        }
    }
}
