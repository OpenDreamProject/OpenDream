using OpenDreamClient.Interface.Descriptors;
using OpenDreamClient.Interface.Html;
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
            msg.PushTag(new MarkupNode("font", null, null)); // Use the default font and font size
            // TODO: Look into using RobustToolbox's markup parser once it's customizable enough
            HtmlParser.Parse(value.Replace("\t", "    "), msg);
            msg.Pop();
            msg.Pop();

            _textBox.AddMessage(msg);
        }
    }
}
