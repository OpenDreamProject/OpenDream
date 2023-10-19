using OpenDreamClient.Input;
using OpenDreamClient.Interface.Descriptors;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace OpenDreamClient.Interface.Controls;

public sealed class ControlWindow : InterfaceControl {
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IUserInterfaceManager _uiMgr = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    private readonly ISawmill _sawmill = Logger.GetSawmill("opendream.window");

    // NOTE: a "window" in BYOND does not necessarily map 1:1 to OS windows.
    // Just like in win32 (which is definitely what this is inspired by let's be real),
    // windows can be embedded into other windows as a way to do nesting.

    public readonly List<InterfaceControl> ChildControls = new();

    public InterfaceMacroSet Macro => _interfaceManager.MacroSets[WindowDescriptor.Macro];

    private WindowDescriptor WindowDescriptor => (WindowDescriptor)ElementDescriptor;

    private Control _menuContainer = default!;
    private LayoutContainer _canvas = default!;

    private (OSWindow? osWindow, IClydeWindow? clydeWindow) _myWindow;


    public ControlWindow(WindowDescriptor windowDescriptor) : base(windowDescriptor, null) {
        IoCManager.InjectDependencies(this);
    }

    protected override void UpdateElementDescriptor() {
        // Don't call base.UpdateElementDescriptor();

        _menuContainer.RemoveAllChildren();
        if (WindowDescriptor.Menu != null && _interfaceManager.Menus.TryGetValue(WindowDescriptor.Menu, out var menu)) {
            _menuContainer.AddChild(menu.MenuBar);
            _menuContainer.Visible = true;
        } else {
            _menuContainer.Visible = false;
        }

        if(!WindowDescriptor.IsPane)
            UpdateWindowAttributes(_myWindow);

        if (WindowDescriptor.IsDefault) {
            Macro.SetActive();
        }
    }

    public OSWindow CreateWindow() {
        OSWindow window = new();
        if(UIElement.Parent is not null)
            UIElement.Orphan();
        window.Children.Add(UIElement);
        window.SetWidth = ControlDescriptor.Size?.X ?? 640;
        window.SetHeight = ControlDescriptor.Size?.Y ?? 440;
        if (ControlDescriptor.Size?.X == 0)
            window.SetWidth = window.MaxWidth;
        if (ControlDescriptor.Size?.Y == 0)
            window.SetHeight = window.MaxHeight;
        window.Closing += _ => {
            // A window can have a command set to be run when it's closed
            if (WindowDescriptor.OnClose != null) {
                _interfaceManager.RunCommand(WindowDescriptor.OnClose);
            }
            _myWindow = (null, _myWindow.clydeWindow);
        };
        window.StartupLocation = WindowStartupLocation.CenterOwner;
        window.Owner = _clyde.MainWindow;

        _myWindow = (window, _myWindow.clydeWindow);
        UpdateWindowAttributes(_myWindow);
        return window;
    }

    public void RegisterOnClydeWindow(IClydeWindow window) {
        // todo: listen for closed.
        if(_myWindow.osWindow is not null){
            _myWindow.osWindow.Close();
            UIElement.Orphan();
        }
        _myWindow = (null, window);
        UpdateWindowAttributes(_myWindow);
    }

    public void UpdateAnchors() {
        var windowSize = Size.GetValueOrDefault();
        if (windowSize.X == 0)
            windowSize.X = 640;
        if (windowSize.Y == 0)
            windowSize.Y = 440;

        for (int i = 0; i < ChildControls.Count; i++) {
            InterfaceControl control = ChildControls[i];
            var element = control.UIElement;
            var elementPos = control.Pos.GetValueOrDefault();
            var elementSize = control.Size.GetValueOrDefault();

            if (control.Size?.Y == 0) {
                elementSize.Y = (windowSize.Y - elementPos.Y);
                if (ChildControls.Count - 1 > i) {
                    if (ChildControls[i + 1].Pos != null && ChildControls[i + 1].UIElement.Visible) {
                        var nextElementPos = ChildControls[i + 1].Pos.GetValueOrDefault();
                        elementSize.Y = nextElementPos.Y - elementPos.Y;
                    }
                }

                element.SetHeight = ((float)elementSize.Y / windowSize.Y) * _canvas.Height;
            }

            if (control.Size?.X == 0) {
                elementSize.X = (windowSize.X - elementPos.X);
                if (ChildControls.Count - 1 > i) {
                    if (ChildControls[i + 1].Pos != null && ChildControls[i + 1].UIElement.Visible) {
                        var nextElementPos = ChildControls[i + 1].Pos.GetValueOrDefault();
                        if (nextElementPos.X < (elementSize.X + elementPos.X) &&
                            nextElementPos.Y < (elementSize.Y + elementPos.Y))
                            elementSize.X = nextElementPos.X - elementPos.X;
                    }
                }

                element.SetWidth = ((float)elementSize.X / windowSize.X) * _canvas.Width;
            }

            if (control.Anchor1.HasValue) {
                var offset1X = elementPos.X - (windowSize.X * control.Anchor1.Value.X / 100f);
                var offset1Y = elementPos.Y - (windowSize.Y * control.Anchor1.Value.Y / 100f);
                var left = (_canvas.Width * control.Anchor1.Value.X / 100) + offset1X;
                var top = (_canvas.Height * control.Anchor1.Value.Y / 100) + offset1Y;
                LayoutContainer.SetMarginLeft(element, Math.Max(left, 0));
                LayoutContainer.SetMarginTop(element, Math.Max(top, 0));

                if (control.Anchor2.HasValue) {
                    if (control.Anchor2.Value.X < control.Anchor1.Value.X ||
                        control.Anchor2.Value.Y < control.Anchor1.Value.Y)
                        _sawmill.Warning($"Invalid anchor2 value in DMF for element {control.Id}. Ignoring.");
                    else {
                        var offset2X = (elementPos.X + elementSize.X) -
                                       (windowSize.X * control.Anchor2.Value.X / 100);
                        var offset2Y = (elementPos.Y + elementSize.Y) -
                                       (windowSize.Y * control.Anchor2.Value.Y / 100);
                        var width = (_canvas.Width * control.Anchor2.Value.X / 100) + offset2X - left;
                        var height = (_canvas.Height * control.Anchor2.Value.Y / 100) + offset2Y - top;
                        element.SetWidth = Math.Max(width, 0);
                        element.SetHeight = Math.Max(height, 0);
                    }

                }
            }
        }
    }

    private void UpdateWindowAttributes((OSWindow? osWindow, IClydeWindow? clydeWindow) windowRoot) {
        // TODO: this would probably be cleaner if an OSWindow for MainWindow was available.
        var (osWindow, clydeWindow) = windowRoot;
        //if our window is null or closed, and we are visible, we need to create a new one. Otherwise we need to update the existing one.
        if(osWindow == null && clydeWindow == null) {
            if (WindowDescriptor.IsVisible) {
                CreateWindow();
                return; //we return because CreateWindow() calls UpdateWindowAttributes() again.
            }
        }
        if(osWindow != null && !osWindow.IsOpen) {
            if (WindowDescriptor.IsVisible) {
                osWindow.Show();
            }
        }

        var title = WindowDescriptor.Title ?? "OpenDream World";
        if (osWindow != null) osWindow.Title = title;
        else if (clydeWindow != null) clydeWindow.Title = title;

        WindowRoot? root = null;
        if (osWindow?.Window != null)
            root = _uiMgr.GetWindowRoot(osWindow.Window);
        else if (clydeWindow != null)
            root = _uiMgr.GetWindowRoot(clydeWindow);

        if (root != null) {
            root.BackgroundColor = WindowDescriptor.BackgroundColor;
        }

        if (osWindow != null && osWindow.ClydeWindow != null) {
            osWindow.ClydeWindow.IsVisible = WindowDescriptor.IsVisible;
            //
            //else if (!WindowDescriptor.IsVisible && osWindow.IsOpen)
            //   osWindow?.Close();
        } else if (clydeWindow != null) {
            clydeWindow.IsVisible = WindowDescriptor.IsVisible;
        }

    }

    public void CreateChildControls() {
        foreach (ControlDescriptor controlDescriptor in WindowDescriptor.ControlDescriptors) {
            AddChild(controlDescriptor);
        }
    }

    public override void AddChild(ElementDescriptor descriptor) {
        if (descriptor is not ControlDescriptor controlDescriptor)
            throw new Exception($"Attempted to add {descriptor} to a window, but it was not a control");
        if (controlDescriptor is WindowDescriptor)
            throw new Exception("Cannot add a window to a window");

        InterfaceControl control = controlDescriptor switch {
            ControlDescriptorChild => new ControlChild(controlDescriptor, this),
            ControlDescriptorInput => new ControlInput(controlDescriptor, this),
            ControlDescriptorButton => new ControlButton(controlDescriptor, this),
            ControlDescriptorOutput => new ControlOutput(controlDescriptor, this),
            ControlDescriptorInfo => new ControlInfo(controlDescriptor, this),
            ControlDescriptorMap => new ControlMap(controlDescriptor, this),
            ControlDescriptorBrowser => new ControlBrowser(controlDescriptor, this),
            ControlDescriptorLabel => new ControlLabel(controlDescriptor, this),
            ControlDescriptorGrid => new ControlGrid(controlDescriptor, this),
            ControlDescriptorTab => new ControlTab(controlDescriptor, this),
            _ => throw new Exception($"Invalid descriptor {controlDescriptor.GetType()}")
        };

        // Can't have out-of-order components, so make sure they're ordered properly
        if (ChildControls.Count > 0) {
            var prevPos = ChildControls[^1].Pos.GetValueOrDefault();
            var curPos = control.Pos.GetValueOrDefault();
            if (prevPos.X <= curPos.X && prevPos.Y <= curPos.Y)
                ChildControls.Add(control);
            else {
                _sawmill.Warning(
                    $"Out of order component {control.Id}. Elements should be defined in order of position. Attempting to fix automatically.");

                int i = 0;
                while (i < ChildControls.Count) {
                    prevPos = ChildControls[i].Pos.GetValueOrDefault();
                    if (prevPos.X <= curPos.X && prevPos.Y <= curPos.Y)
                        i++;
                    else
                        break;
                }

                ChildControls.Insert(i, control);
            }
        } else
            ChildControls.Add(control);

        _canvas.Children.Add(control.UIElement);
    }

    // Because of how windows are not always real windows,
    // UIControl contains the *contents* of the window, not the actual OS window itself.
    protected override Control CreateUIElement() {
        var container = new BoxContainer {
            RectClipContent = true,
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Children = {
                (_menuContainer = new Control { Margin = new Thickness(4, 0)}),
                (_canvas = new LayoutContainer {
                    InheritChildMeasure = false,
                    VerticalExpand = true
                })
            }
        };

        _canvas.OnResized += CanvasOnResized;

        return container;
    }

    private void CanvasOnResized() {
        UpdateAnchors();
    }
}
