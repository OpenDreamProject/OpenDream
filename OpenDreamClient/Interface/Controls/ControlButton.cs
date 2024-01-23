using OpenDreamClient.Interface.Descriptors;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace OpenDreamClient.Interface.Controls;

internal sealed class ControlButton : InterfaceControl {
    public const string StyleClassDMFButton = "DMFbutton";
    private Button _button;

    public ControlButton(ControlDescriptor controlDescriptor, ControlWindow window) : base(controlDescriptor, window) { }

    protected override Control CreateUIElement() {
        _button = new Button() {
            ClipText = true
        };

        _button.OnPressed += OnButtonClick;
        _button.Label.Margin = new Thickness(0, -3, 0, 0);
        _button.Label.AddStyleClass(StyleClassDMFButton);

        return _button;
    }

    protected override void UpdateElementDescriptor() {
        base.UpdateElementDescriptor();

        ControlDescriptorButton controlDescriptor = (ControlDescriptorButton)ElementDescriptor;

        _button.Text = controlDescriptor.Text;
    }

    private void OnButtonClick(BaseButton.ButtonEventArgs args) {
        ControlDescriptorButton controlDescriptor = (ControlDescriptorButton)ElementDescriptor;

        if (controlDescriptor.Command != null) {
            _interfaceManager.RunCommand(controlDescriptor.Command);
        }
    }
}
