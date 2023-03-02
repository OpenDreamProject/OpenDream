using OpenDreamShared.Dream.Procs;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace OpenDreamClient.Interface.Prompts {
    sealed class MessagePrompt : InputWindow {
        private readonly TextEdit _textEdit;

        public MessagePrompt(int promptId, String title, String message, String defaultValue, bool canCancel) : base(
            promptId, title, message, defaultValue, canCancel) {
            _textEdit = new TextEdit {
                TextRope = new Rope.Leaf(defaultValue),

                // Select all the text by default
                CursorPosition = new TextEdit.CursorPos(defaultValue.Length, TextEdit.LineBreakBias.Bottom),
                SelectionStart = new TextEdit.CursorPos(0, TextEdit.LineBreakBias.Bottom)
            };

            SetPromptControl(_textEdit);
        }

        protected override void OkButtonClicked() {
            FinishPrompt(DMValueType.Message, Rope.Collapse(_textEdit.TextRope));
        }
    }
}
