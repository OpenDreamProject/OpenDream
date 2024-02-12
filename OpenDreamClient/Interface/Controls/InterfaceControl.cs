using System.Diagnostics.CodeAnalysis;
using OpenDreamClient.Interface.Descriptors;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace OpenDreamClient.Interface.Controls;

public abstract class InterfaceControl : InterfaceElement {
    public readonly Control UIElement;
    public bool IsDefault => ControlDescriptor.IsDefault;
    public Vector2i? Size => ControlDescriptor.Size;
    public Vector2i? Pos => ControlDescriptor.Pos;
    public Vector2i? Anchor1 => ControlDescriptor.Anchor1;
    public Vector2i? Anchor2 => ControlDescriptor.Anchor2;

    protected ControlDescriptor ControlDescriptor => (ControlDescriptor) ElementDescriptor;

    private readonly ControlWindow _window;

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    protected InterfaceControl(ControlDescriptor controlDescriptor, ControlWindow window) : base(controlDescriptor) {
        IoCManager.InjectDependencies(this);

        _window = window;
        UIElement = CreateUIElement();

        UpdateElementDescriptor();
    }

    protected abstract Control CreateUIElement();

    protected override void UpdateElementDescriptor() {
        UIElement.Name = ControlDescriptor.Name;

        var pos = ControlDescriptor.Pos.GetValueOrDefault();
        LayoutContainer.SetMarginLeft(UIElement, pos.X);
        LayoutContainer.SetMarginTop(UIElement, pos.Y);

        if (ControlDescriptor.Size is { } size)
            UIElement.SetSize = size;

        _window?.UpdateAnchors();

        if (ControlDescriptor.BackgroundColor is { } bgColor) {
            var styleBox = new StyleBoxFlat {BackgroundColor = bgColor};

            switch (UIElement) {
                case PanelContainer panel:
                    panel.PanelOverride = styleBox;
                    break;
                case LineEdit lineEdit:
                    lineEdit.StyleBoxOverride = styleBox;
                    break;
            }
        }

        UIElement.Visible = ControlDescriptor.IsVisible;
        // TODO: enablement
        //UIControl.IsEnabled = !_controlDescriptor.IsDisabled;
    }

    public override bool TryGetProperty(string property, out string value) {
        switch (property) {
            case "size":
                value = $"{UIElement.Size.X}x{UIElement.Size.Y}";
                return true;
            case "is-disabled":
                value = ControlDescriptor.IsDisabled.ToString();
                return true;
            case "pos":
                value = $"{UIElement.Position.X},{UIElement.Position.Y}";
                return true;
            default:
                return base.TryGetProperty(property, out value);
        }
    }

    public virtual void Output(string value, string? data) {

    }
}
