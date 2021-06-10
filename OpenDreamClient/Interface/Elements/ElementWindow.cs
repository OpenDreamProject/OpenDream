using OpenDreamClient.Resources.ResourceTypes;
using OpenDreamShared.Interface;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OpenDreamClient.Interface.Elements {
    class ElementWindow : InterfaceElement {
        public List<InterfaceElement> ChildElements = new();

        private WindowDescriptor _windowDescriptor;
        private DockPanel _dockPanel;
        private Menu _menu;
        private Canvas _canvas;
        private List<Window> _openWindows = new();

        public ElementWindow(WindowDescriptor windowDescriptor) : base(windowDescriptor.MainElementDescriptor, null) {
            _windowDescriptor = windowDescriptor;
        }

        protected override FrameworkElement CreateUIElement() {
            _dockPanel = new DockPanel();

            _menu = new Menu();
            DockPanel.SetDock(_menu, Dock.Top);
            _dockPanel.Children.Add(_menu);

            _canvas = new Canvas() {
                Margin = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            _canvas.SizeChanged += (object sender, SizeChangedEventArgs e) => UpdateAnchors();
            DockPanel.SetDock(_canvas, Dock.Bottom);
            _dockPanel.Children.Add(_canvas);

            return _dockPanel;
        }

        public override void UpdateElementDescriptor() {
            // Don't call base.UpdateElementDescriptor();

            ElementDescriptorMain elementDescriptor = (ElementDescriptorMain)_elementDescriptor;

            if (elementDescriptor.Menu != null) {
                _menu.Visibility = Visibility.Visible;

                InterfaceDescriptor interfaceDescriptor = Program.OpenDream.Interface.InterfaceDescriptor;

                interfaceDescriptor.MenuDescriptors.TryGetValue(elementDescriptor.Menu, out MenuDescriptor menuDescriptor);
                CreateMenu(menuDescriptor);
            } else {
                _menu.Visibility = Visibility.Collapsed;
            }

            if (elementDescriptor.Icon != null) {
                Program.OpenDream.ResourceManager.LoadResourceAsync<ResourceDMI>(elementDescriptor.Icon, iconResource => {
                    SetIcon(iconResource.CreateWPFImageSource());
                });
            } else {
                SetIcon(null);
            }
        }

        public override void Shutdown() {
            foreach (InterfaceElement element in ChildElements) {
                element.Shutdown();
            }
        }

        public void CreateChildElements() {
            foreach (WindowElementDescriptor elementDescriptor in _windowDescriptor.ElementDescriptors) {
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
                _openWindows.Remove(window);
            };

            _openWindows.Add(window);
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

        private void SetIcon(ImageSource icon) {
            foreach (Window window in _openWindows) {
                window.Icon = icon;
            }
        }

        private void CreateMenu(MenuDescriptor menuDescriptor) {
            _menu.Items.Clear();
            if (menuDescriptor == null) return;

            Dictionary<string, List<MenuElementDescriptor>> categories = new();

            foreach (MenuElementDescriptor elementDescriptor in menuDescriptor.Elements) {
                if (elementDescriptor.Category == null) {
                    categories.Add(elementDescriptor.Name, new());
                } else {
                    if (!categories.ContainsKey(elementDescriptor.Category)) categories.Add(elementDescriptor.Category, new());

                    categories[elementDescriptor.Category].Add(elementDescriptor);
                }
            }

            foreach (KeyValuePair<string, List<MenuElementDescriptor>> categoryPair in categories) {
                MenuItem category = CreateMenuItem(categoryPair.Key, null, false);

                _menu.Items.Add(category);
                foreach (MenuElementDescriptor elementDescriptor in categoryPair.Value) {
                    if (String.IsNullOrEmpty(elementDescriptor.Name)) {
                        category.Items.Add(new Separator());
                    } else {
                        MenuItem item = CreateMenuItem(elementDescriptor.Name, elementDescriptor.Command, elementDescriptor.CanCheck);
                        
                        category.Items.Add(item);
                    }
                }
            }
        }

        private MenuItem CreateMenuItem(string name, string command, bool isCheckable) {
            if (name.StartsWith("&")) name = name.Substring(1); //TODO: First character in name becomes a selection shortcut

            MenuItem item = new MenuItem() {
                Header = name,
                IsCheckable = isCheckable
            };

            if (!String.IsNullOrEmpty(command)) {
                item.Click += (object sender, RoutedEventArgs e) => {
                    Program.OpenDream.RunCommand(command);
                };
            }

            return item;
        }
    }
}
