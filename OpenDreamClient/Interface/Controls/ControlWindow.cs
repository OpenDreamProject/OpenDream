using OpenDreamClient.Resources.ResourceTypes;
using OpenDreamShared.Interface;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OpenDreamClient.Interface.Controls {
    class ControlWindow : InterfaceControl {
        public List<InterfaceControl> ChildControls = new();

        private WindowDescriptor _windowDescriptor;
        private DockPanel _dockPanel;
        private Menu _menu;
        private Canvas _canvas;
        private List<Window> _openWindows = new();

        public ControlWindow(WindowDescriptor windowDescriptor) : base(windowDescriptor.MainControlDescriptor, null) {
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
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            _canvas.SizeChanged += (object sender, SizeChangedEventArgs e) => UpdateAnchors();
            DockPanel.SetDock(_canvas, Dock.Bottom);
            _dockPanel.Children.Add(_canvas);

            return _dockPanel;
        }

        public override void UpdateElementDescriptor() {
            // Don't call base.UpdateElementDescriptor();

            ControlDescriptorMain controlDescriptor = (ControlDescriptorMain)_elementDescriptor;

            if (controlDescriptor.Menu != null) {
                _menu.Visibility = Visibility.Visible;

                InterfaceDescriptor interfaceDescriptor = Program.OpenDream.Interface.InterfaceDescriptor;

                interfaceDescriptor.MenuDescriptors.TryGetValue(controlDescriptor.Menu, out MenuDescriptor menuDescriptor);
                CreateMenu(menuDescriptor);
            } else {
                _menu.Visibility = Visibility.Collapsed;
            }

            foreach (Window window in _openWindows) {
                UpdateWindowAttributes(window, controlDescriptor);
            }
        }

        public override void Shutdown() {
            foreach (InterfaceControl control in ChildControls) {
                control.Shutdown();
            }
        }

        public void CreateChildControls() {
            foreach (ControlDescriptor controlDescriptor in _windowDescriptor.ControlDescriptors) {
                if (controlDescriptor == _windowDescriptor.MainControlDescriptor) continue;

                InterfaceControl control = controlDescriptor switch {
                    ControlDescriptorChild => new ControlChild(controlDescriptor, this),
                    ControlDescriptorInput => new ControlInput(controlDescriptor, this),
                    ControlDescriptorButton => new ControlButton(controlDescriptor, this),
                    ControlDescriptorOutput => new ControlOutput(controlDescriptor, this),
                    ControlDescriptorInfo => new ControlInfo(controlDescriptor, this),
                    ControlDescriptorMap => new ControlMap(controlDescriptor, this),
                    ControlDescriptorBrowser => new ControlBrowser(controlDescriptor, this),
                    ControlDescriptorLabel => new ControlLabel(controlDescriptor, this),
                    _ => throw new Exception("Invalid descriptor")
                };

                ChildControls.Add(control);
                _canvas.Children.Add(control.UIElement);
            }
        }

        public Window CreateWindow() {
            Window window = new Window();

            window.Content = UIElement;
            window.Width = _controlDescriptor.Size?.Width ?? 640;
            window.Height = _controlDescriptor.Size?.Height ?? 440;
            window.Closing += (object sender, CancelEventArgs e) => {
                window.Owner = null; //Without this, the owning window ends up minimized
                _openWindows.Remove(window);
            };

            _openWindows.Add(window);
            UpdateWindowAttributes(window, (ControlDescriptorMain)_elementDescriptor);
            return window;
        }

        public void UpdateAnchors() {
            foreach (InterfaceControl control in ChildControls) {
                FrameworkElement element = control.UIElement;

                if (control.Anchor1.HasValue) {
                    System.Drawing.Point elementPos = control.Pos.GetValueOrDefault();
                    System.Drawing.Size windowSize = Size.GetValueOrDefault();

                    double offset1X = elementPos.X - (windowSize.Width * control.Anchor1.Value.X / 100);
                    double offset1Y = elementPos.Y - (windowSize.Height * control.Anchor1.Value.Y / 100);
                    double left = (_canvas.ActualWidth * control.Anchor1.Value.X / 100) + offset1X;
                    double top = (_canvas.ActualHeight * control.Anchor1.Value.Y / 100) + offset1Y;
                    Canvas.SetLeft(element, Math.Max(left, 0));
                    Canvas.SetTop(element, Math.Max(top, 0));

                    if (control.Anchor2.HasValue) {
                        System.Drawing.Size elementSize = control.Size.GetValueOrDefault();

                        double offset2X = (elementPos.X + elementSize.Width) - (windowSize.Width * control.Anchor2.Value.X / 100);
                        double offset2Y = (elementPos.Y + elementSize.Height) - (windowSize.Height * control.Anchor2.Value.Y / 100);
                        double width = (_canvas.ActualWidth * control.Anchor2.Value.X / 100) + offset2X - left;
                        double height = (_canvas.ActualHeight * control.Anchor2.Value.Y / 100) + offset2Y - top;
                        element.Width = Math.Max(width, 0);
                        element.Height = Math.Max(height, 0);
                    }
                }
            }
        }

        private void UpdateWindowAttributes(Window window, ControlDescriptorMain descriptor) {
            window.Title = descriptor.Title ?? "OpenDream World";

            if (descriptor.Icon != null) {
                Program.OpenDream.ResourceManager.LoadResourceAsync<ResourceDMI>(descriptor.Icon, iconResource => {
                    window.Icon = iconResource.CreateWPFImageSource();
                });
            } else {
                window.Icon = null;
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
