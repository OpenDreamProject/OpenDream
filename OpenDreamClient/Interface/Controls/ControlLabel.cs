using System.Globalization;
using OpenDreamClient.Interface.Descriptors;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace OpenDreamClient.Interface.Controls;

internal sealed class ControlLabel(ControlDescriptor controlDescriptor, ControlWindow window) : InterfaceControl(controlDescriptor, window) {
    private Label _label = default!;

    protected override Control CreateUIElement() {
        _label = new Label {
            HorizontalExpand = true,
            VerticalExpand = true
        };

        var container = new PanelContainer();
        container.AddChild(_label);

        return container;
    }

    protected override void UpdateElementDescriptor() {
        base.UpdateElementDescriptor();

        ControlDescriptorLabel controlDescriptor = (ControlDescriptorLabel)ElementDescriptor;
        _label.Text = controlDescriptor.Text.AsRaw();
        _label.FontColorOverride = (ControlDescriptor.TextColor.Value != Color.Transparent)
            ? ControlDescriptor.TextColor.Value
            : null;

        (_label.Align, _label.VAlign) = controlDescriptor.Align.AsRaw().ToLower(CultureInfo.InvariantCulture) switch {
            "center" => (Label.AlignMode.Center, Label.VAlignMode.Center),
            "left" => (Label.AlignMode.Left, Label.VAlignMode.Center),
            "right" => (Label.AlignMode.Right, Label.VAlignMode.Center),
            "top" => (Label.AlignMode.Center, Label.VAlignMode.Top),
            "bottom" => (Label.AlignMode.Center, Label.VAlignMode.Bottom),
            "top-left" => (Label.AlignMode.Left, Label.VAlignMode.Top),
            "top-right" => (Label.AlignMode.Right, Label.VAlignMode.Top),
            "bottom-left" => (Label.AlignMode.Left, Label.VAlignMode.Bottom),
            "bottom-right" => (Label.AlignMode.Right, Label.VAlignMode.Bottom),
            _ => (Label.AlignMode.Center, Label.VAlignMode.Center)
            // TODO: This can also take DM direction flags like WEST, or 0 for center
        };
    }
}
