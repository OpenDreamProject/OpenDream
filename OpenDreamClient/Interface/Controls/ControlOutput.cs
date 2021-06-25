using OpenDreamShared.Interface;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OpenDreamClient.Interface.Controls {
    class ControlOutput : InterfaceControl {
        private TextBox _textBox;
        private Border _border;

        public ControlOutput(ControlDescriptor controlDescriptor, ControlWindow window) : base(controlDescriptor, window) { }

        protected override FrameworkElement CreateUIElement() {
            _textBox = new TextBox() {
                IsReadOnly = true
            };

            _border = new Border() {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                Child = _textBox
            };

            return _border;
        }

        public override void Output(string value, string data) {
            _textBox.AppendText(value);
            _textBox.AppendText(Environment.NewLine);
        }
    }
}
