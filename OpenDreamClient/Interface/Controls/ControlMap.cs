﻿using OpenDreamClient.Input;
using OpenDreamClient.Interface.Controls.UI;
using OpenDreamClient.Interface.Descriptors;
using OpenDreamShared.Dream;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;

namespace OpenDreamClient.Interface.Controls;

public sealed class ControlMap(ControlDescriptor controlDescriptor, ControlWindow window) : InterfaceControl(controlDescriptor, window) {
    public ScalingViewport Viewport { get; private set; }

    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    private MouseInputSystem? _mouseInput;

    private ControlDescriptorMap MapDescriptor => (ControlDescriptorMap)ElementDescriptor;

    protected override void UpdateElementDescriptor() {
        base.UpdateElementDescriptor();

        Viewport.StretchMode = MapDescriptor.ZoomMode.Value switch {
            "blur" => ScalingViewportStretchMode.Bilinear,
            "distort" => ScalingViewportStretchMode.Nearest,

            // TODO: "tries to keep the look of individual pixels,
            //          but will adjust to non-integer zooms (like 1.1x) by blending neighboring pixels"
            "normal" or _ => ScalingViewportStretchMode.Nearest
        };

        UpdateViewRange(_interfaceManager.View);
    }

    public void UpdateViewRange(ViewRange view) {
        var viewWidth = Math.Max(view.Width, 1);
        var viewHeight = Math.Max(view.Height, 1);

        Viewport.ViewportSize = new Vector2i(viewWidth, viewHeight) * EyeManager.PixelsPerMeter;
        if (MapDescriptor.IconSize.Value != 0) {
            // BYOND supports a negative number here (flips the view), but we're gonna enforce a positive number instead
            var iconSize = Math.Max(MapDescriptor.IconSize.Value, 1);

            Viewport.SetWidth = iconSize * viewWidth;
            Viewport.SetHeight = iconSize * viewHeight;
        } else {
            // icon-size of 0 means stretch to fit the available space
            Viewport.SetWidth = float.NaN;
            Viewport.SetHeight = float.NaN;
        }
    }

    protected override Control CreateUIElement() {
        Viewport = new ScalingViewport { MouseFilter = Control.MouseFilterMode.Stop };
        Viewport.OnKeyBindDown += OnViewportKeyBindEvent;
        Viewport.OnKeyBindUp += OnViewportKeyBindEvent;
        Viewport.OnVisibilityChanged += (args) => {
            if (args.Visible) {
                OnShowEvent();
            } else {
                OnHideEvent();
            }
        };
        if(ControlDescriptor.IsVisible.Value)
            OnShowEvent();
        else
            OnHideEvent();

        UpdateViewRange(_interfaceManager.View);

        return new PanelContainer { StyleClasses = {"MapBackground"}, Children = { Viewport } };
    }

    private void OnViewportKeyBindEvent(GUIBoundKeyEventArgs e) {
        if (e.Function == EngineKeyFunctions.Use || e.Function == EngineKeyFunctions.TextCursorSelect ||
            e.Function == EngineKeyFunctions.UIRightClick || e.Function == OpenDreamKeyFunctions.MouseMiddle) {
            _entitySystemManager.Resolve(ref _mouseInput);

            if (_mouseInput.HandleViewportEvent(Viewport, e, ControlDescriptor)) {
                e.Handle();
            }
        }
    }

    public void OnShowEvent() {
        ControlDescriptorMap controlDescriptor = (ControlDescriptorMap)ControlDescriptor;
        if (!string.IsNullOrWhiteSpace(controlDescriptor.OnShowCommand.Value)) {
            _interfaceManager.RunCommand(controlDescriptor.OnShowCommand.AsRaw());
        }
    }

    public void OnHideEvent() {
        ControlDescriptorMap controlDescriptor = (ControlDescriptorMap)ControlDescriptor;
        if (!string.IsNullOrWhiteSpace(controlDescriptor.OnHideCommand.Value)) {
            _interfaceManager.RunCommand(controlDescriptor.OnHideCommand.AsRaw());
        }
    }
}
