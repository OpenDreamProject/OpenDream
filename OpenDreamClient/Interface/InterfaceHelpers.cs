using OpenDreamClient.Interface.Elements;
using OpenDreamShared.Interface;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace OpenDreamClient.Interface {
    static class InterfaceHelpers {
        public static ElementWindow CreateWindowFromDescriptor(InterfaceWindowDescriptor windowDescriptor) {
            InterfaceElementDescriptor mainDescriptor = windowDescriptor.MainElementDescriptor;
            ElementWindow elementWindow = new ElementWindow();
            List<IElement> elements = new List<IElement>();

            elementWindow.ElementDescriptor = mainDescriptor;
            foreach (InterfaceElementDescriptor elementDescriptor in windowDescriptor.ElementDescriptors) {
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

            elementWindow.Margin = new Thickness(0, 0, 0, 0);
            elementWindow.HorizontalAlignment = HorizontalAlignment.Stretch;
            elementWindow.VerticalAlignment = VerticalAlignment.Stretch;
            elementWindow.SizeChanged += (object sender, SizeChangedEventArgs e) => UpdateAnchors(elementWindow, elements);

            return elementWindow;
        }

        public static IElement CreateElementFromDescriptor(InterfaceElementDescriptor elementDescriptor) {
            IElement element;

            switch (elementDescriptor.Type) {
                case InterfaceElementDescriptor.InterfaceElementDescriptorType.Child: element = new ElementChild(); break;
                case InterfaceElementDescriptor.InterfaceElementDescriptorType.Input: element = new ElementInput(); break;
                case InterfaceElementDescriptor.InterfaceElementDescriptorType.Button: element = new ElementButton(); break;
                case InterfaceElementDescriptor.InterfaceElementDescriptorType.Output: element = new ElementOutput(); break;
                case InterfaceElementDescriptor.InterfaceElementDescriptorType.Info: element = new ElementInfo(); break;
                case InterfaceElementDescriptor.InterfaceElementDescriptorType.Map: element = new ElementMap(); break;
                default: throw new Exception("Element descriptor had an invalid type (" + elementDescriptor.Type + ")");
            }

            SetSharedAttributes(element, elementDescriptor);
            element.ElementDescriptor = elementDescriptor;

            return element;
        }

        private static void SetSharedAttributes(IElement element, InterfaceElementDescriptor elementDescriptor) {
            UIElement uiElement = (UIElement)element;
            FrameworkElement frameworkElement = uiElement as FrameworkElement;

            Canvas.SetLeft(uiElement, elementDescriptor.Pos.X);
            Canvas.SetTop(uiElement, elementDescriptor.Pos.Y);
            if (frameworkElement != null) {
                frameworkElement.Width = elementDescriptor.Size.Width;
                frameworkElement.Height = elementDescriptor.Size.Height;
            }
        }

        private static void UpdateAnchors(ElementWindow elementWindow, List<IElement> elements) {
            InterfaceElementDescriptor mainDescriptor = elementWindow.ElementDescriptor;

            foreach (IElement element in elements) {
                InterfaceElementDescriptor elementDescriptor = element.ElementDescriptor;
                FrameworkElement control = (FrameworkElement)element;

                if (elementDescriptor.CoordinateAttributes.ContainsKey("anchor1")) {
                    System.Drawing.Point anchor1 = elementDescriptor.CoordinateAttributes["anchor1"];

                    double offset1X = elementDescriptor.Pos.X - (mainDescriptor.Size.Width * anchor1.X / 100);
                    double offset1Y = elementDescriptor.Pos.Y - (mainDescriptor.Size.Height * anchor1.Y / 100);
                    double left = (elementWindow.ActualWidth * anchor1.X / 100) + offset1X;
                    double top = (elementWindow.ActualHeight * anchor1.Y / 100) + offset1Y;
                    Canvas.SetLeft(control, left);
                    Canvas.SetTop(control, top);

                    if (elementDescriptor.CoordinateAttributes.ContainsKey("anchor2")) {
                        System.Drawing.Point anchor2 = elementDescriptor.CoordinateAttributes["anchor2"];

                        double offset2X = (elementDescriptor.Pos.X + elementDescriptor.Size.Width) - (mainDescriptor.Size.Width * anchor2.X / 100);
                        double offset2Y = (elementDescriptor.Pos.Y + elementDescriptor.Size.Height) - (mainDescriptor.Size.Height * anchor2.Y / 100);
                        control.Width = (elementWindow.ActualWidth * anchor2.X / 100) + offset2X - left;
                        control.Height = (elementWindow.ActualHeight * anchor2.Y / 100) + offset2Y - top;
                    }
                }
            }
        }
    }
}
