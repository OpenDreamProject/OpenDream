using OpenDreamClient.Interface.Elements;
using OpenDreamShared.Interface;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OpenDreamClient.Interface {
    static class InterfaceHelpers {
        public static ElementWindow CreateWindowFromDescriptor(WindowDescriptor windowDescriptor) {
            ElementDescriptorMain mainDescriptor = windowDescriptor.MainElementDescriptor;
            ElementWindow elementWindow = new ElementWindow();
            List<IElement> elements = new List<IElement>();

            elementWindow.ElementDescriptor = mainDescriptor;
            foreach (ElementDescriptor elementDescriptor in windowDescriptor.ElementDescriptors) {
                if (elementDescriptor != mainDescriptor) {
                    try {
                        IElement element = CreateElementFromDescriptor(elementDescriptor);

                        elements.Add(element);
                        elementWindow.Children.Add((UIElement)element);
                    } catch (Exception e) {
                        Console.WriteLine("Error while creating an interface element: " + e.Message);
                    }
                }
            }

            elementWindow.ChildElements = elements.ToArray();
            elementWindow.Margin = new Thickness(0, 0, 0, 0);
            elementWindow.HorizontalAlignment = HorizontalAlignment.Stretch;
            elementWindow.VerticalAlignment = VerticalAlignment.Stretch;
            elementWindow.SizeChanged += (object sender, SizeChangedEventArgs e) => UpdateAnchors(elementWindow, elements);

            return elementWindow;
        }

        public static IElement CreateElementFromDescriptor(ElementDescriptor elementDescriptor) {
            IElement element = elementDescriptor switch {
                ElementDescriptorChild => new ElementChild(),
                ElementDescriptorInput => new ElementInput(),
                ElementDescriptorButton => new ElementButton(),
                ElementDescriptorOutput => new ElementOutput(),
                ElementDescriptorInfo => new ElementInfo(),
                ElementDescriptorMap => new ElementMap(),
                ElementDescriptorBrowser => new ElementBrowser(),
                _ => throw new Exception("Invalid descriptor")
            };

            SetSharedAttributes(element, elementDescriptor);
            element.ElementDescriptor = elementDescriptor;

            return element;
        }

        private static void SetSharedAttributes(IElement element, ElementDescriptor elementDescriptor) {
            UIElement uiElement = (UIElement)element;
            FrameworkElement frameworkElement = uiElement as FrameworkElement;
            System.Drawing.Point pos = elementDescriptor.Pos.GetValueOrDefault();

            Canvas.SetLeft(uiElement, pos.X);
            Canvas.SetTop(uiElement, pos.Y);
            if (frameworkElement != null) {
                System.Drawing.Size size = elementDescriptor.Size.GetValueOrDefault();

                frameworkElement.Width = size.Width;
                frameworkElement.Height = size.Height;
            }

            if (elementDescriptor.BackgroundColor.HasValue) {
                System.Drawing.Color color = elementDescriptor.BackgroundColor.Value;
                Brush brush = new SolidColorBrush(Color.FromRgb(color.R, color.G, color.B));

                switch (uiElement) {
                    case Panel panel:
                        panel.Background = brush;
                        break;
                    case Control control:
                        control.Background = brush;
                        break;
                }
            }

            if (elementDescriptor.IsVisible.HasValue) {
                uiElement.Visibility = elementDescriptor.IsVisible.Value ? Visibility.Visible : Visibility.Hidden;
            }
        }

        private static void UpdateAnchors(ElementWindow elementWindow, List<IElement> elements) {
            ElementDescriptor mainDescriptor = elementWindow.ElementDescriptor;

            foreach (IElement element in elements) {
                ElementDescriptor elementDescriptor = element.ElementDescriptor;
                FrameworkElement control = (FrameworkElement)element;

                if (elementDescriptor.Anchor1.HasValue) {
                    System.Drawing.Point elementPos = elementDescriptor.Pos.GetValueOrDefault();
                    System.Drawing.Size windowSize = mainDescriptor.Size.GetValueOrDefault();

                    double offset1X = elementPos.X - (windowSize.Width * elementDescriptor.Anchor1.Value.X / 100);
                    double offset1Y = elementPos.Y - (windowSize.Height * elementDescriptor.Anchor1.Value.Y / 100);
                    double left = (elementWindow.ActualWidth * elementDescriptor.Anchor1.Value.X / 100) + offset1X;
                    double top = (elementWindow.ActualHeight * elementDescriptor.Anchor1.Value.Y / 100) + offset1Y;
                    Canvas.SetLeft(control, left);
                    Canvas.SetTop(control, top);

                    if (elementDescriptor.Anchor2.HasValue) {
                        System.Drawing.Size elementSize = elementDescriptor.Size.GetValueOrDefault();

                        double offset2X = (elementPos.X + elementSize.Width) - (windowSize.Width * elementDescriptor.Anchor2.Value.X / 100);
                        double offset2Y = (elementPos.Y + elementSize.Height) - (windowSize.Height * elementDescriptor.Anchor2.Value.Y / 100);
                        control.Width = (elementWindow.ActualWidth * elementDescriptor.Anchor2.Value.X / 100) + offset2X - left;
                        control.Height = (elementWindow.ActualHeight * elementDescriptor.Anchor2.Value.Y / 100) + offset2Y - top;
                    }
                }
            }
        }

        public static int KeyToKeyCode(Key key) {

            int keyCode = key switch {
                Key.W => 87,
                Key.A => 65,
                Key.S => 83,
                Key.D => 68,
                Key.Up => 38,
                Key.Down => 40,
                Key.Left => 37,
                Key.Right => 39,
                Key.T => 84,
                _ => -1
            };

            return keyCode;
        }
    }
}
