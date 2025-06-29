using OpenDreamClient.Interface.Controls.UI;
using OpenDreamClient.Interface.Descriptors;
using OpenDreamClient.Interface.Html;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;

namespace OpenDreamClient.Interface.Controls;

public sealed class ControlOutput(ControlDescriptor controlDescriptor, ControlWindow window) : InterfaceControl(controlDescriptor, window) {
    private OutputControl _textBox = default!;

    protected override Control CreateUIElement() {
        _textBox = new OutputControl();

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

        msg.PushColor(ControlDescriptor.TextColor.Value);
        msg.PushTag(new MarkupNode("font", null, null)); // Use the default font and font size
        HtmlParser.Parse(value.Replace("\t", "    "), msg);
        msg.Pop();
        msg.Pop();

        _textBox.AddMessage(msg);
    }
}
