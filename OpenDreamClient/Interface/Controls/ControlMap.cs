using OpenDreamClient.Input;
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

        Viewport.StretchMode = MapDescriptor.ZoomMode switch {
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
        if (MapDescriptor.IconSize != 0) {
            Viewport.SetWidth = MapDescriptor.IconSize * viewWidth;
            Viewport.SetHeight = MapDescriptor.IconSize * viewHeight;
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
        if(ControlDescriptor.IsVisible)
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

            if (_mouseInput.HandleViewportEvent(Viewport, e)) {
                e.Handle();
            }
        }
    }

    public void OnShowEvent() {
        ControlDescriptorMap controlDescriptor = (ControlDescriptorMap)ControlDescriptor;
        if (!string.IsNullOrWhiteSpace(controlDescriptor.OnShowCommand)) {
            _interfaceManager.RunCommand(controlDescriptor.OnShowCommand);
        }
    }

    public void OnHideEvent() {
        ControlDescriptorMap controlDescriptor = (ControlDescriptorMap)ControlDescriptor;
        if (!string.IsNullOrWhiteSpace(controlDescriptor.OnHideCommand)) {
            _interfaceManager.RunCommand(controlDescriptor.OnHideCommand);
        }
    }
}
