using OpenDreamShared.Dream;
using Robust.Client.UserInterface.Controls;

namespace OpenDreamClient.Interface.Prompts;

internal sealed class TextPrompt : InputWindow {
    private readonly LineEdit _textEdit;

    public TextPrompt(string title, string message, string defaultValue, bool canCancel,
        Action<DMValueType, object?>? onClose) : base(title, message, canCancel, onClose) {
        _textEdit = new LineEdit {
            Text = defaultValue,
            VerticalAlignment = VAlignment.Top
        };

        _textEdit.OnTextEntered += TextEdit_TextEntered;
        SetPromptControl(_textEdit);
    }

    protected override void OkButtonClicked() {
        FinishPrompt(DMValueType.Text, _textEdit.Text);
    }

    private void TextEdit_TextEntered(LineEdit.LineEditEventArgs e) {
        ButtonClicked(DefaultButton);
    }
}
