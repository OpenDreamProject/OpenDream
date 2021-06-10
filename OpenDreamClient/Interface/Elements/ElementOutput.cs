using OpenDreamShared.Interface;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OpenDreamClient.Interface.Elements {
    class ElementOutput : InterfaceElement {
        private TextBox _textBox;
        private Border _border;

        public ElementOutput(WindowElementDescriptor elementDescriptor, ElementWindow window) : base(elementDescriptor, window) { }

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
