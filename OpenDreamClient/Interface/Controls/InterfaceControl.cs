using System.Diagnostics.CodeAnalysis;
using OpenDreamClient.Interface.Descriptors;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace OpenDreamClient.Interface.Controls;

public abstract class InterfaceControl : InterfaceElement {
    public readonly Control UIElement;
    public bool IsDefault => ControlDescriptor.IsDefault.Value;
    public DMFPropertyVec2 Size => ControlDescriptor.Size;
    public DMFPropertyVec2 Pos => ControlDescriptor.Pos;
    public DMFPropertyVec2? Anchor1 => ControlDescriptor.Anchor1;
    public DMFPropertyVec2? Anchor2 => ControlDescriptor.Anchor2;

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
        UIElement.Name = ControlDescriptor.Name.Value;

        var pos = ControlDescriptor.Pos;
        LayoutContainer.SetMarginLeft(UIElement, pos.X);
        LayoutContainer.SetMarginTop(UIElement, pos.Y);

        if (ControlDescriptor.Size is { } size)
            UIElement.SetSize = new Vector2(size.X, size.Y);

        _window?.UpdateAnchors();

        if (ControlDescriptor.BackgroundColor is { } bgColor) {
            var styleBox = new StyleBoxFlat {BackgroundColor = bgColor.Value};

            switch (UIElement) {
                case PanelContainer panel:
                    panel.PanelOverride = styleBox;
                    break;
                case LineEdit lineEdit:
                    lineEdit.StyleBoxOverride = styleBox;
                    break;
            }
        }

        UIElement.Visible = ControlDescriptor.IsVisible.Value;
        // TODO: enablement
        //UIControl.IsEnabled = !_controlDescriptor.IsDisabled;
    }

    public override bool TryGetProperty(string property, [NotNullWhen(true)] out DMFProperty? value) {
        switch (property) {
            case "size":
                value = new DMFPropertyVec2(UIElement.Size);
                return true;
            case "is-disabled":
                value = ControlDescriptor.IsDisabled;
                return true;
            case "pos":
                value = new DMFPropertyVec2(UIElement.Position);
                return true;
            default:
                return base.TryGetProperty(property, out value);
        }
    }

    public virtual void Output(string value, string? data) {

    }
}
