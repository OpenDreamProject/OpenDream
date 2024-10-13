﻿using System.Diagnostics.CodeAnalysis;
using OpenDreamClient.Interface.Descriptors;
using OpenDreamClient.Interface.DMF;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace OpenDreamClient.Interface.Controls;

public sealed class ControlWindow : InterfaceControl {
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IUserInterfaceManager _uiMgr = default!;

    private readonly ISawmill _sawmill = Logger.GetSawmill("opendream.window");

    // NOTE: a "window" in BYOND does not necessarily map 1:1 to OS windows.
    // Just like in win32 (which is definitely what this is inspired by let's be real),
    // windows can be embedded into other windows as a way to do nesting.

    public readonly List<InterfaceControl> ChildControls = new();

    public string Title => WindowDescriptor.Title.Value ?? (WindowDescriptor.IsDefault.Value ? "OpenDream World" : WindowDescriptor.Id.AsRaw());
    public InterfaceMacroSet Macro => _interfaceManager.MacroSets[WindowDescriptor.Macro.AsRaw()];

    private WindowDescriptor WindowDescriptor => (WindowDescriptor)ElementDescriptor;

    private PanelContainer _menuContainer = default!;
    private LayoutContainer _canvas = default!;

    private (OSWindow? osWindow, IClydeWindow? clydeWindow) _myWindow;

    public ControlWindow(WindowDescriptor windowDescriptor) : base(windowDescriptor, null) {
        IoCManager.InjectDependencies(this);
    }

    protected override void UpdateElementDescriptor() {
        // Don't call base.UpdateElementDescriptor();

        _menuContainer.RemoveAllChildren();
        if (WindowDescriptor.Menu.Value != null && _interfaceManager.Menus.TryGetValue(WindowDescriptor.Menu.Value, out var menu)) {
            _menuContainer.AddChild(menu.MenuBar);
            _menuContainer.Visible = true;
        } else {
            _menuContainer.Visible = false;
        }

        if(!WindowDescriptor.IsPane.Value)
            UpdateWindowAttributes(_myWindow);

        if (WindowDescriptor.IsDefault.Value) {
            Macro.SetActive();
        }
    }

    /// <summary>
    /// Closes the window if it is a child window. No effect if it is either a default window or a pane
    /// </summary>
    public void CloseChildWindow() {
        if(_myWindow.osWindow is not null)
            _myWindow.osWindow.Close();
    }

    public OSWindow CreateWindow() {
        if(_myWindow.osWindow is not null)
            return _myWindow.osWindow;

        OSWindow window = new();
        if(UIElement.Parent is not null)
            UIElement.Orphan();
        window.Children.Add(UIElement);

        if (ControlDescriptor.Size.X == 0)
            window.SetWidth = window.MaxWidth;
        else
            window.SetWidth = ControlDescriptor.Size.X;
        if (ControlDescriptor.Size.Y == 0)
            window.SetHeight = window.MaxHeight;
        else
            window.SetHeight = ControlDescriptor.Size.Y;

        window.Closing += _ => {
            // A window can have a command set to be run when it's closed
            if (!string.IsNullOrWhiteSpace(WindowDescriptor.OnClose.Value)) {
                _interfaceManager.RunCommand(WindowDescriptor.OnClose.Value);
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
        var windowSize = Size;
        if (windowSize.X == 0)
            windowSize.X = _canvas.PixelWidth;
        if (windowSize.Y == 0)
            windowSize.Y = _canvas.PixelHeight;

        for (int i = 0; i < ChildControls.Count; i++) {
            InterfaceControl control = ChildControls[i];
            var element = control.UIElement;
            var elementPos = control.Pos;
            var elementSize = control.Size;

            if (control.Anchor1.HasValue) {
                var anchorTo = control.AnchorPosition;

                // Defaults to anchoring relative to the DMF-defined size
                if (anchorTo.X == 0)
                    anchorTo.X = Size.X;
                if (anchorTo.Y == 0)
                    anchorTo.Y = Size.Y;

                var offset1X = elementPos.X - (anchorTo.X * control.Anchor1.Value.X / 100f);
                var offset1Y = elementPos.Y - (anchorTo.Y * control.Anchor1.Value.Y / 100f);
                var left = (_canvas.Width * control.Anchor1.Value.X / 100) + offset1X;
                var top = (_canvas.Height * control.Anchor1.Value.Y / 100) + offset1Y;
                LayoutContainer.SetMarginLeft(element, Math.Max(left, 0));
                LayoutContainer.SetMarginTop(element, Math.Max(top, 0));

                if (control.Anchor2.HasValue) {
                    if (control.Anchor2.Value.X < control.Anchor1.Value.X ||
                        control.Anchor2.Value.Y < control.Anchor1.Value.Y) {
                        _sawmill.Warning($"Invalid anchor2 value in DMF for element {control.Id}. Ignoring.");
                    } else {
                        var offset2X = (elementPos.X + elementSize.X) -
                                       (anchorTo.X * control.Anchor2.Value.X / 100);
                        var offset2Y = (elementPos.Y + elementSize.Y) -
                                       (anchorTo.Y * control.Anchor2.Value.Y / 100);
                        var width = (_canvas.Width * control.Anchor2.Value.X / 100) + offset2X - left;
                        var height = (_canvas.Height * control.Anchor2.Value.Y / 100) + offset2Y - top;
                        element.SetWidth = Math.Max(width, 0);
                        element.SetHeight = Math.Max(height, 0);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Updates the control's anchoring position to the window's current size.
    /// Also updates other controls' anchoring position if they have a size of 0.
    /// </summary>
    /// <param name="control">The control triggering the anchor update</param>
    public void UpdateAnchorPosition(InterfaceControl control) {
        control.AnchorPosition = _canvas.PixelSize;

        // Also update the anchor position for anything with a size of 0
        foreach (var child in ChildControls) {
            if (child.UIElement.SetWidth == 0)
                child.AnchorPosition = child.AnchorPosition with {X = _canvas.PixelWidth + child.Size.X};
            if (child.UIElement.SetHeight == 0)
                child.AnchorPosition = child.AnchorPosition with {Y = _canvas.PixelHeight + child.Size.Y};
        }

        UpdateAnchors();
    }

    private void UpdateWindowAttributes((OSWindow? osWindow, IClydeWindow? clydeWindow) windowRoot) {
        // TODO: this would probably be cleaner if an OSWindow for MainWindow was available.
        var (osWindow, clydeWindow) = windowRoot;

        //if our window is null or closed, and we are visible, we need to create a new one. Otherwise we need to update the existing one.
        if(osWindow == null && clydeWindow == null) {
            if (WindowDescriptor.IsVisible.Value) {
                CreateWindow();
                return; //we return because CreateWindow() calls UpdateWindowAttributes() again.
            }
        }

        if(osWindow != null && !osWindow.IsOpen) {
            if (WindowDescriptor.IsVisible.Value) {
                osWindow.Show();
            }
        }

        if (osWindow != null) osWindow.Title = Title;
        else if (clydeWindow != null) clydeWindow.Title = Title;

        WindowRoot? root = null;
        if (osWindow?.Window != null)
            root = _uiMgr.GetWindowRoot(osWindow.Window);
        else if (clydeWindow != null)
            root = _uiMgr.GetWindowRoot(clydeWindow);

        if (root != null) {
            root.BackgroundColor = (WindowDescriptor.BackgroundColor.Value != Color.Transparent)
                ? WindowDescriptor.BackgroundColor.Value
                : DreamStylesheet.DefaultBackgroundColor;
        }

        if (osWindow != null && osWindow.ClydeWindow != null) {
            osWindow.ClydeWindow.IsVisible = WindowDescriptor.IsVisible.Value;
        } else if (clydeWindow != null) {
            clydeWindow.IsVisible = WindowDescriptor.IsVisible.Value;
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
            ControlDescriptorBar => new ControlBar(controlDescriptor, this),
            _ => throw new Exception($"Invalid descriptor {controlDescriptor.GetType()}")
        };

        // Can't have out-of-order components, so make sure they're ordered properly
        if (ChildControls.Count > 0) {
            var prevPos = ChildControls[^1].Pos;
            var curPos = control.Pos;
            if (prevPos.X <= curPos.X && prevPos.Y <= curPos.Y)
                ChildControls.Add(control);
            else {
                _sawmill.Warning(
                    $"Out of order component {control.Id}. Elements should be defined in order of position. Attempting to fix automatically.");

                int i = 0;
                while (i < ChildControls.Count) {
                    prevPos = ChildControls[i].Pos;
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
                (_menuContainer = new PanelContainer {
                    PanelOverride = new StyleBoxFlat(Color.White),
                    HorizontalExpand = true
                }),
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

    public override bool TryGetProperty(string property, [NotNullWhen(true)] out IDMFProperty? value) {
        switch (property) {
            case "size": // ControlWindow has its own getter for this because it doesn't use SetSize
                value = new DMFPropertySize(UIElement.Size);
                return true;
            case "inner-size":
                value = new DMFPropertySize((int)_canvas.Width, (int)_canvas.Height);
                return true;
            case "outer-size":
                if (_myWindow.osWindow is not null) {
                    value = new DMFPropertySize((int)_myWindow.osWindow.Width, (int)_myWindow.osWindow.Height);
                    return true;
                } else if (_myWindow.clydeWindow is not null) {
                    value = new DMFPropertySize(_myWindow.clydeWindow.Size);
                    return true;
                } else {
                    value = new DMFPropertySize(UIElement.Size);
                    return true;
                }
            case "is-minimized":
                if (_myWindow.osWindow?.ClydeWindow != null) {
                    value = new DMFPropertyBool(_myWindow.osWindow.ClydeWindow.IsMinimized);
                    return true;
                } else if (_myWindow.clydeWindow is not null) {
                    value = new DMFPropertyBool(_myWindow.clydeWindow.IsMinimized);
                    return true;
                } else {
                    value = new DMFPropertyBool(false);
                    return true;
                }
            case "is-maximized": //TODO this is currently "not isMinimised" because RT doesn't expose a maximised check
                if (_myWindow.osWindow?.ClydeWindow != null) {
                    value = new DMFPropertyBool(!_myWindow.osWindow.ClydeWindow.IsMinimized);
                    return true;
                } else if (_myWindow.clydeWindow is not null) {
                    value = new DMFPropertyBool(!_myWindow.clydeWindow.IsMinimized);
                    return true;
                } else {
                    value = new DMFPropertyBool(false);
                    return true;
                }
            default:
                return base.TryGetProperty(property, out value);
        }
    }

    public override void SetProperty(string property, string value, bool manualWinset = false) {
        if (property is "size" or "pos")
            return; // TODO: RT offers no ability to resize or position windows

        base.SetProperty(property, value, manualWinset);
    }
}
