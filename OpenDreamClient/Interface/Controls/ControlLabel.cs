using OpenDreamClient.Interface.Descriptors;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace OpenDreamClient.Interface.Controls;

internal sealed class ControlLabel : InterfaceControl {
    private Label _label;

    public ControlLabel(ControlDescriptor controlDescriptor, ControlWindow window) : base(controlDescriptor, window) { }

    protected override Control CreateUIElement() {
        _label = new Label() {
            HorizontalAlignment = Control.HAlignment.Center,
            VerticalAlignment = Control.VAlignment.Center,
        };

        return _label;
    }

    protected override void UpdateElementDescriptor() {
        base.UpdateElementDescriptor();

        ControlDescriptorLabel controlDescriptor = (ControlDescriptorLabel)ElementDescriptor;
        _label.Text = controlDescriptor.Text.AsRaw();
    }
}
