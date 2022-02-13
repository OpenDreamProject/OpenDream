using System;
using OpenDreamShared.Dream.Procs;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace OpenDreamClient.Interface.Prompts
{
    [Virtual]
    class TextPrompt : InputWindow {
        public TextPrompt(int promptId, String title, String message, String defaultValue, bool canCancel) : base(promptId, title, message, defaultValue, canCancel) { }

        protected override Control CreateInputControl(String defaultValue) {
            return new LineEdit {
                Text = defaultValue,
                VerticalAlignment = VAlignment.Top
            };
        }

        protected override void OkButtonClicked() {
            FinishPrompt(DMValueType.Text, ((LineEdit)_inputControl).Text);
        }
    }
}
