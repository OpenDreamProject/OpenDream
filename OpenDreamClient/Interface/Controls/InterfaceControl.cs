using System.Diagnostics.CodeAnalysis;
using OpenDreamClient.Interface.Descriptors;
using OpenDreamClient.Interface.DMF;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace OpenDreamClient.Interface.Controls;

public abstract class InterfaceControl : InterfaceElement {
    public readonly Control UIElement;
    public bool IsDefault => ControlDescriptor.IsDefault.Value;
    public DMFPropertySize Size => ControlDescriptor.Size;
    public DMFPropertyPos Pos => ControlDescriptor.Pos;
    public DMFPropertyPos? Anchor1 => ControlDescriptor.Anchor1;
    public DMFPropertyPos? Anchor2 => ControlDescriptor.Anchor2;

    /// <summary>
    /// The size that anchor1 and anchor2 anchor themselves to.
    /// Updates when this control's size is winset, to keep them relative to the window's size.
    /// </summary>
    public Vector2i? AnchoredToSize;

    protected ControlDescriptor ControlDescriptor => (ControlDescriptor) ElementDescriptor;

    private readonly ControlWindow? _window;

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    protected InterfaceControl(ControlDescriptor controlDescriptor, ControlWindow? window) : base(controlDescriptor) {
        IoCManager.InjectDependencies(this);

        _window = window;
        UIElement = CreateUIElement();

        UpdateAnchoredToSize();
        UpdateElementDescriptor();
    }

    protected abstract Control CreateUIElement();

    protected override void UpdateElementDescriptor() {
        UIElement.Name = ControlDescriptor.Name.Value;

        var pos = ControlDescriptor.Pos;
        LayoutContainer.SetMarginLeft(UIElement, pos.X);
        LayoutContainer.SetMarginTop(UIElement, pos.Y);

        UIElement.SetSize = new Vector2(ControlDescriptor.Size.X, ControlDescriptor.Size.Y);

        _window?.UpdateAnchors();

        //transparent is default because it's white with 0 alpha, and DMF color can't have none-255 alpha
        StyleBox? styleBox = (ControlDescriptor.BackgroundColor.Value != Color.Transparent)
            ? new StyleBoxFlat {BackgroundColor = ControlDescriptor.BackgroundColor.Value}
            : null;

        switch (UIElement) {
            case PanelContainer panel:
                panel.PanelOverride = styleBox;
                break;
            case LineEdit lineEdit:
                lineEdit.StyleBoxOverride = styleBox;
                break;
            case Button button:
                button.StyleBoxOverride = styleBox;
                break;
        }

        Color? textColor = (ControlDescriptor.TextColor.Value != Color.Transparent)
            ? ControlDescriptor.TextColor.Value
            : null;

        switch (UIElement) {
            case Button button:
                button.Label.FontColorOverride = textColor;
                break;
        }

        UIElement.Visible = ControlDescriptor.IsVisible.Value;
        // TODO: enablement
        //UIControl.IsEnabled = !_controlDescriptor.IsDisabled;
    }

    public override bool TryGetProperty(string property, [NotNullWhen(true)] out IDMFProperty? value) {
        switch (property) {
            case "size":
                // SetSize because Size won't update if the element isn't visible
                value = new DMFPropertySize(UIElement.SetSize);
                return true;
            case "pos":
                value = new DMFPropertyPos(UIElement.Position);
                return true;
            default:
                return base.TryGetProperty(property, out value);
        }
    }

    public virtual void Output(string value, string? data) {
    }

    public void UpdateAnchoredToSize() {
        AnchoredToSize = (_window != null) ? (Vector2i)_window.UIElement.Size : null;
    }
}
