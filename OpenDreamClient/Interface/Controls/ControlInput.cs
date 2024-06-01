using System.Diagnostics.CodeAnalysis;
using OpenDreamClient.Interface.Descriptors;
using OpenDreamClient.Interface.DMF;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace OpenDreamClient.Interface.Controls;

internal sealed class ControlInput : InterfaceControl {
    private LineEdit _textBox;

    public ControlInput(ControlDescriptor controlDescriptor, ControlWindow window) : base(controlDescriptor, window) { }

    protected override Control CreateUIElement() {
        _textBox = new LineEdit();
        _textBox.OnTextEntered += TextBox_OnSubmit;

        return _textBox;
    }

    private void TextBox_OnSubmit(LineEdit.LineEditEventArgs lineEditEventArgs) {
        _interfaceManager.RunCommand(_textBox.Text);
        _textBox.Clear();
    }

    protected override void UpdateElementDescriptor() {
        base.UpdateElementDescriptor();
        ControlDescriptorInput inputDescriptor = (ControlDescriptorInput)ElementDescriptor;
        _textBox.Text = inputDescriptor.Text.AsRaw();
    }

    public override bool TryGetProperty(string property, [NotNullWhen(true)] out IDMFProperty? value) {
        switch (property) {
            case "text":
                value = new DMFPropertyString(_textBox.Text);
                return true;
            default:
                return base.TryGetProperty(property, out value);
        }
    }
}
