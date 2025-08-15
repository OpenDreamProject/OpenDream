using System.Diagnostics.CodeAnalysis;
using OpenDreamClient.Interface.Descriptors;
using OpenDreamClient.Interface.DMF;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace OpenDreamClient.Interface.Controls;

internal sealed class ControlInput(ControlDescriptor controlDescriptor, ControlWindow window) : InterfaceControl(controlDescriptor, window) {
    private LineEdit _textBox = default!;

    private ControlDescriptorInput InputDescriptor => (ControlDescriptorInput)ControlDescriptor;

    protected override Control CreateUIElement() {
        _textBox = new LineEdit();
        _textBox.OnTextEntered += TextBox_OnSubmit;

        return _textBox;
    }

    private void TextBox_OnSubmit(LineEdit.LineEditEventArgs lineEditEventArgs) {
        if (InputDescriptor.NoCommand.Value)
            return;

        var command = InputDescriptor.Command.Value;
        if (command.StartsWith('!')) {
            _interfaceManager.RunCommand(lineEditEventArgs.Text);
        } else {
            _interfaceManager.RunCommand(command + lineEditEventArgs.Text);
        }

        ResetText();
    }

    protected override void UpdateElementDescriptor() {
        base.UpdateElementDescriptor();

        ResetText();
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

    public override void SetProperty(string property, string value, bool manualWinset = false) {
        switch (property) {
            case "focus":
                var focusValue = new DMFPropertyBool(value);
                if (focusValue.Value)
                    _textBox.GrabKeyboardFocus();
                break;
            default:
                base.SetProperty(property, value, manualWinset);
                break;
        }
    }

    private void ResetText() {
        var command = InputDescriptor.Command.Value;

        if (command.StartsWith('!')) {
            _textBox.Text = command[1..];
        } else {
            _textBox.Clear();
        }
    }
}
