using OpenDreamShared.Interface;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OpenDreamClient.Interface.Controls {
    class ControlInput : InterfaceControl {
        private TextBox _textBox;

        public ControlInput(ControlDescriptor controlDescriptor, ControlWindow window) : base(controlDescriptor, window) { }

        protected override FrameworkElement CreateUIElement() {
            _textBox = new TextBox();
            _textBox.KeyDown += TextBox_KeyDown;

            return _textBox;
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Return) {
                Program.OpenDream.RunCommand(_textBox.Text);
                _textBox.Clear();
            }
        }
    }
}
