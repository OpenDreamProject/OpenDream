/*using System;
using OpenDreamShared.Dream.Procs;
using System.Windows.Controls;

namespace OpenDreamClient.Interface.Prompts {
    class MessagePrompt : InputWindow {
        public MessagePrompt(int promptId, String title, String message, String defaultValue, bool canCancel) : base(promptId, title, message, defaultValue, canCancel) { }

        protected override Control CreateInputControl(String defaultValue) {
            return new TextBox {
                MinHeight = 100,
                MaxWidth = 500,
                MaxHeight = 400,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Text = defaultValue,
            };
        }

        protected override void OkButtonClicked() {
            FinishPrompt(DMValueType.Message, ((TextBox)_inputControl).Text);
        }
    }
}
*/
