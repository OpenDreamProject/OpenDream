using OpenDreamShared.Dream.Procs;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace OpenDreamClient.Interface.Prompts
{
    sealed class MessagePrompt : InputWindow {
        private readonly LineEdit _lineEdit;

        public MessagePrompt(int promptId, String title, String message, String defaultValue, bool canCancel) : base(
            promptId, title, message, defaultValue, canCancel) {
            // TODO: Switch this to a proper multi-line edit.
            _lineEdit = new LineEdit {
                MinHeight = 100,
                MaxWidth = 500,
                MaxHeight = 400,
                //AcceptsReturn = true,
                //VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
                //HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Text = defaultValue,
            };

            SetPromptControl(_lineEdit);
        }

        protected override void OkButtonClicked() {
            FinishPrompt(DMValueType.Message, _lineEdit.Text);
        }
    }
}
