using OpenDreamClient.Input;
using OpenDreamClient.Interface.Descriptors;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace OpenDreamClient.Interface.Controls;

internal sealed class ControlInput : InterfaceControl {
    private LineEdit _textBox;
    [Dependency] private readonly IDreamInterfaceManager _interfaceManager = default!;
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
}
