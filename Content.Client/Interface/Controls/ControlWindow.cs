using System;
using System.Collections.Generic;
using Content.Client.Input;
using Content.Shared.Interface;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;


namespace Content.Client.Interface.Controls
{
    sealed class ControlWindow : InterfaceControl
    {
        [Dependency] private readonly IUserInterfaceManager _uiMgr = default!;
        [Dependency] private readonly IDreamInterfaceManager _dreamInterface = default!;

        // NOTE: a "window" in BYOND does not necessarily map 1:1 to OS windows.
        // Just like in win32 (which is definitely what this is inspired by let's be real),
        // windows can be embedded into other windows as a way to do nesting.

        public readonly List<(OSWindow osWindow, IClydeWindow clydeWindow)> _openWindows = new();

        public List<InterfaceControl> ChildControls = new();

        private readonly WindowDescriptor _windowDescriptor;
        private MenuBar _menu = default!;
        private LayoutContainer _canvas = default!;

        public ControlWindow(WindowDescriptor windowDescriptor) : base(windowDescriptor.MainControlDescriptor, null)
        {
            IoCManager.InjectDependencies(this);

            _windowDescriptor = windowDescriptor;
        }

        public override void UpdateElementDescriptor()
        {
            // Don't call base.UpdateElementDescriptor();

            var controlDescriptor = (ControlDescriptorMain)_elementDescriptor;

            if (controlDescriptor.Menu != null)
            {
                _menu.Visible = true;

                InterfaceDescriptor interfaceDescriptor = _dreamInterface.InterfaceDescriptor;

                interfaceDescriptor.MenuDescriptors.TryGetValue(controlDescriptor.Menu,
                    out MenuDescriptor menuDescriptor);
                CreateMenu(menuDescriptor);
            }
            else
            {
                _menu.Visible = false;
            }

            foreach (var window in _openWindows)
            {
                UpdateWindowAttributes(window, controlDescriptor);
            }
        }

        public OSWindow CreateWindow()
        {
            OSWindow window = new();

            window.Children.Add(UIElement);
            window.SetWidth = _controlDescriptor.Size?.Width ?? 640;
            window.SetHeight = _controlDescriptor.Size?.Height ?? 440;
            window.Closing += _ => { _openWindows.Remove((window, null)); };

            _openWindows.Add((window, null));
            UpdateWindowAttributes((window, null), (ControlDescriptorMain)_elementDescriptor);
            return window;
        }

        public void RegisterOnClydeWindow(IClydeWindow window)
        {
            // todo: listen for closed.
            _openWindows.Add((null, window));
            UpdateWindowAttributes((null, window), (ControlDescriptorMain)_elementDescriptor);
        }

        public void UpdateAnchors()
        {
            foreach (InterfaceControl control in ChildControls)
            {
                var element = control.UIElement;

                if (control.Anchor1.HasValue)
                {
                    var elementPos = control.Pos.GetValueOrDefault();
                    var windowSize = Size.GetValueOrDefault();

                    var offset1X = elementPos.X - (windowSize.Width * control.Anchor1.Value.X / 100f);
                    var offset1Y = elementPos.Y - (windowSize.Height * control.Anchor1.Value.Y / 100f);
                    var left = (_canvas.Width * control.Anchor1.Value.X / 100) + offset1X;
                    var top = (_canvas.Height * control.Anchor1.Value.Y / 100) + offset1Y;
                    LayoutContainer.SetMarginLeft(element, Math.Max(left, 0));
                    LayoutContainer.SetMarginTop(element, Math.Max(top, 0));

                    if (control.Anchor2.HasValue)
                    {
                        var elementSize = control.Size.GetValueOrDefault();

                        var offset2X = (elementPos.X + elementSize.Width) -
                                       (windowSize.Width * control.Anchor2.Value.X / 100);
                        var offset2Y = (elementPos.Y + elementSize.Height) -
                                       (windowSize.Height * control.Anchor2.Value.Y / 100);
                        var width = (_canvas.Width * control.Anchor2.Value.X / 100) + offset2X - left;
                        var height = (_canvas.Height * control.Anchor2.Value.Y / 100) + offset2Y - top;
                        element.SetWidth = Math.Max(width, 0);
                        element.SetHeight = Math.Max(height, 0);
                    }
                }
            }
        }

        private void UpdateWindowAttributes(
            (OSWindow osWindow, IClydeWindow clydeWindow) windowRoot,
            ControlDescriptorMain descriptor)
        {
            // TODO: this would probably be cleaner if an OSWindow for MainWindow was available.
            var (osWindow, clydeWindow) = windowRoot;

            var title = descriptor.Title ?? "OpenDream World";
            if (osWindow != null) osWindow.Title = title;
            else if (clydeWindow != null) clydeWindow.Title = title;

            WindowRoot root = null;
            if (osWindow?.Window != null)
                root = _uiMgr.GetWindowRoot(osWindow.Window);
            else if (clydeWindow != null)
                root = _uiMgr.GetWindowRoot(clydeWindow);

            if (root != null)
            {
                root.BackgroundColor = descriptor.BackgroundColor;
            }
        }

        public void CreateChildControls(DreamInterfaceManager manager)
        {
            foreach (ControlDescriptor controlDescriptor in _windowDescriptor.ControlDescriptors)
            {
                if (controlDescriptor == _windowDescriptor.MainControlDescriptor) continue;

                InterfaceControl control = controlDescriptor switch
                {
                    ControlDescriptorChild => new ControlChild(controlDescriptor, this),
                    ControlDescriptorInput => new ControlInput(controlDescriptor, this),
                    ControlDescriptorButton => new ControlButton(controlDescriptor, this),
                    ControlDescriptorOutput => new ControlOutput(controlDescriptor, this),
                    ControlDescriptorInfo => new ControlInfo(controlDescriptor, this),
                    ControlDescriptorMap => new ControlMap(controlDescriptor, this),
                    ControlDescriptorBrowser => new ControlBrowser(controlDescriptor, this),
                    ControlDescriptorLabel => new ControlLabel(controlDescriptor, this),
                    _ => throw new Exception($"Invalid descriptor {controlDescriptor.GetType()}")
                };

                ChildControls.Add(control);
                _canvas.Children.Add(control.UIElement);
            }
        }


        // Because of how windows are not always real windows,
        // UIControl contains the *contents* of the window, not the actual OS window itself.
        protected override Control CreateUIElement()
        {
            var container = new BoxContainer
            {
                RectClipContent = true,
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                Children =
                {
                    (_menu = new MenuBar { Margin = new Thickness(4, 0)}),
                    (_canvas = new LayoutContainer
                    {
                        InheritChildMeasure = false,
                        VerticalExpand = true
                    })
                }
            };

            _canvas.OnResized += CanvasOnResized;

            return container;
        }

        private void CanvasOnResized()
        {
            UpdateAnchors();
        }

        private void CreateMenu(MenuDescriptor menuDescriptor)
        {
            _menu.Menus.Clear();
            if (menuDescriptor == null) return;

            Dictionary<string, List<MenuElementDescriptor>> categories = new();

            foreach (MenuElementDescriptor elementDescriptor in menuDescriptor.Elements)
            {
                if (elementDescriptor.Category == null)
                {
                    categories.Add(elementDescriptor.Name, new());
                }
                else
                {
                    if (!categories.ContainsKey(elementDescriptor.Category))
                        categories.Add(elementDescriptor.Category, new());

                    categories[elementDescriptor.Category].Add(elementDescriptor);
                }
            }

            foreach (KeyValuePair<string, List<MenuElementDescriptor>> categoryPair in categories)
            {
                var menu = new MenuBar.Menu();
                menu.Title = categoryPair.Key;

                _menu.Menus.Add(menu);
                foreach (MenuElementDescriptor elementDescriptor in categoryPair.Value)
                {
                    if (String.IsNullOrEmpty(elementDescriptor.Name))
                    {
                        menu.Entries.Add(new MenuBar.MenuSeparator());
                    }
                    else
                    {
                        var item = CreateMenuItem(elementDescriptor.Name, elementDescriptor.Command,
                            elementDescriptor.CanCheck);

                        menu.Entries.Add(item);
                    }
                }
            }
        }

        private MenuBar.MenuEntry CreateMenuItem(string name, string command, bool isCheckable)
        {
            if (name.StartsWith("&"))
                name = name.Substring(1); //TODO: First character in name becomes a selection shortcut

            MenuBar.MenuButton item = new MenuBar.MenuButton()
            {
                Text = name,
                //IsCheckable = isCheckable
            };

            if (!String.IsNullOrEmpty(command))
            {
                item.OnPressed += () => { EntitySystem.Get<DreamCommandSystem>().RunCommand(command); };
            }

            return item;
        }
    }
}
