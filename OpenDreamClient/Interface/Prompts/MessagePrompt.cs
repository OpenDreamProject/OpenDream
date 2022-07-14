using OpenDreamShared.Dream.Procs;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace OpenDreamClient.Interface.Prompts
{
    sealed class MessagePrompt : InputWindow {
        public MessagePrompt(int promptId, String title, String message, String defaultValue, bool canCancel) : base(promptId, title, message, defaultValue, canCancel) { }

        protected override Control CreateInputControl(String defaultValue) {
            // TODO: Switch this to a proper multi-line edit.
            return new LineEdit {
                MinHeight = 100,
                MaxWidth = 500,
                MaxHeight = 400,
                //AcceptsReturn = true,
                //VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
                //HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Text = defaultValue,
            };
        }

        protected override void OkButtonClicked() {
            FinishPrompt(DMValueType.Message, ((LineEdit)_inputControl).Text);
        }
    }
}
