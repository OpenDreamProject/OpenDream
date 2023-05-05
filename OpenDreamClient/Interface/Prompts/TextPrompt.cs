using OpenDreamShared.Dream.Procs;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace OpenDreamClient.Interface.Prompts;

sealed class TextPrompt : InputWindow {
    private readonly LineEdit _textEdit;

    public TextPrompt(int promptId, String title, String message, String defaultValue, bool canCancel) : base(promptId,
        title, message, defaultValue, canCancel) {
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
