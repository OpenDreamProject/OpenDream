using OpenDreamClient.Interface.Descriptors;
using OpenDreamClient.Interface.Html;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace OpenDreamClient.Interface.Controls;

public sealed class ControlOutput(ControlDescriptor controlDescriptor, ControlWindow window) : InterfaceControl(controlDescriptor, window) {
    private OutputPanel _textBox;

    protected override Control CreateUIElement() {
        _textBox = new OutputPanel();

        return _textBox;
    }

    protected override void UpdateElementDescriptor() {
        base.UpdateElementDescriptor();

        _textBox.StyleBoxOverride = new StyleBoxFlat((ControlDescriptor.BackgroundColor.Value != Color.Transparent)
            ? ControlDescriptor.BackgroundColor.Value
            : Color.White);
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
