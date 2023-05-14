using OpenDreamClient.Interface.Descriptors;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace OpenDreamClient.Interface.Controls {
    public sealed class ControlOutput : InterfaceControl {
        private OutputPanel _textBox;
        //private Border _border;

        public ControlOutput(ControlDescriptor controlDescriptor, ControlWindow window) : base(controlDescriptor, window) { }

        protected override Control CreateUIElement() {
            _textBox = new OutputPanel();

            /*
            _border = new Border() {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                Child = _textBox
            };
            */

            return _textBox;
        }

        public override void Output(string value, string? data) {
            var msg = new FormattedMessage(2);
            msg.PushColor(Color.Black);
            msg.AddText(value.Replace("\t", "        "));
            _textBox.AddMessage(msg);
        }
    }
}
