using System.Diagnostics.CodeAnalysis;
using OpenDreamClient.Input;
using OpenDreamClient.Interface.Controls.UI;
using OpenDreamClient.Interface.Descriptors;
using OpenDreamClient.Interface.DMF;
using OpenDreamClient.Rendering;
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
    private ClientAppearanceSystem? _appearanceSystem;

    private ControlDescriptorMap MapDescriptor => (ControlDescriptorMap)ElementDescriptor;

    private ClientObjectReference? _atomUnderMouse;

    protected override void UpdateElementDescriptor() {
        base.UpdateElementDescriptor();

        // Don't attempt to render any non-main viewports
        Viewport.Visible = MapDescriptor.IsDefault.Value;

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
        Viewport.OnMouseMove += OnViewportMouseMoveEvent;
        Viewport.OnMouseExited += OnViewportMouseExitedEvent;
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

    private void OnViewportMouseMoveEvent(GUIMouseMoveEventArgs e) {
        if (_mouseInput == null)
            return;

        var underMouse = _mouseInput.GetAtomUnderMouse(Viewport, e.RelativePixelPosition, e.GlobalPixelPosition);
        UpdateAtomUnderMouse(underMouse?.Atom, e.RelativePixelPosition, underMouse?.IconPosition ?? Vector2i.Zero);
    }

    private void OnViewportMouseExitedEvent(GUIMouseHoverEventArgs e) {
        UpdateAtomUnderMouse(null, Vector2.Zero, Vector2i.Zero);
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

    public override bool TryGetProperty(string property, [NotNullWhen(true)] out IDMFProperty? value) {
        switch (property) {
            case "view-size": // Size of the final viewport (resized and all) rather than the whole container
                value = new DMFPropertyVec2(Viewport.GetDrawBox().Size);
                return true;
            default:
                return base.TryGetProperty(property, out value);
        }
    }

    private void UpdateAtomUnderMouse(ClientObjectReference? atom, Vector2 relativePos, Vector2i iconPos) {
        if (!_atomUnderMouse.Equals(atom)) {
            _entitySystemManager.Resolve(ref _appearanceSystem);

            var name = (atom != null) ? _appearanceSystem.GetName(atom.Value) : string.Empty;
            Window?.SetStatus(name);

            if (_atomUnderMouse != null)
                _mouseInput?.HandleAtomMouseExited(Viewport, _atomUnderMouse.Value);
            if (atom != null)
                _mouseInput?.HandleAtomMouseEntered(Viewport, relativePos, atom.Value, iconPos);
        }

        _atomUnderMouse = atom;
    }
}
