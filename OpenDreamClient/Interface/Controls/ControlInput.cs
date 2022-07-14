using OpenDreamShared.Interface;
using OpenDreamClient.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace OpenDreamClient.Interface.Controls {
    sealed class ControlInput : InterfaceControl
    {
        private LineEdit _textBox;

        public ControlInput(ControlDescriptor controlDescriptor, ControlWindow window) : base(controlDescriptor, window) { }

        protected override Control CreateUIElement() {
            _textBox = new LineEdit();
            _textBox.OnTextEntered += TextBox_OnSubmit;

            return _textBox;
        }

        private void TextBox_OnSubmit(LineEdit.LineEditEventArgs lineEditEventArgs) {
            EntitySystem.Get<DreamCommandSystem>().RunCommand(_textBox.Text);
            _textBox.Clear();
        }
    }
}
